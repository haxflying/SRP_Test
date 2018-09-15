using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


namespace MZ.LWD
{
    using System;
    using UnityObject = UnityEngine.Object;

    [Flags]
    public enum ClearFlag
    {
        None = 0,
        Color = 1,
        Depth = 2,

        All = Depth | Color
    }

    public enum MaterialHandles
    {
        //Error,
        //DepthCopy,
        //Sampling,
        Blit,
        ScreenSpacceShadow,
    }

    public struct RenderingData
    {
        public CullResults cullResults;
        public CameraData cameraData;
        public LightData lightData;
        public ShadowData shadowData;
        public bool supportsDynamicBatching;
    }

    public struct LightData
    {
        public int pixelAdditionalLightsCount;
        public int mainLightIndex;
        public List<VisibleLight> visibleLights;
        public List<int> visibleLocalLightIndices;
    }

    public struct CameraData
    {
        public Camera camera;
        public bool isSceneViewCamera;
        public bool isDefaultViewport;
        public bool isOffscreenRender;
        public bool isHdrEnabled;
        public float maxShadowDistance;
    }

    public struct ShadowData
    {
        public int directionalShadowAltasRes;
        public int directionalLightCascadeCount;
        public int bufferBitCount;
        public bool supportSoftShadows;
    }

    public struct RenderTargetHandle
    {
        public int id { private set; get; }

        public static readonly RenderTargetHandle CameraTarget = new RenderTargetHandle { id = -1 };

        public void Init(string shaderProperty)
        {
            id = Shader.PropertyToID(shaderProperty);
        }

        public RenderTargetIdentifier Identifier()
        {
            if(id == -1)
            {
                return BuiltinRenderTextureType.CameraTarget;
            }
            return new RenderTargetIdentifier(id);
        }

        public bool Equals(RenderTargetHandle other)
        {
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderTargetHandle && Equals((RenderTargetHandle)obj);
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator ==(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return !c1.Equals(c2);
        }
    }

    public struct ShadowSliceData
    {
        public Matrix4x4 shadowTransform;
        public int offsetX;
        public int offsetY;
        public int resolution;

        public void Clear()
        {
            shadowTransform = Matrix4x4.identity;
            offsetX = offsetY = 0;
            resolution = 1024;
        }
    }

    public static class CoreShadowUtils
    {
        public static bool ExtractDirectionalLightMatrix(ref CullResults cullResults, ref ShadowData shadowData, 
            int shadowLightIndex, int cascadeIndex, int shadowResolution, float shadowNearPlane, float shadowFarPlane,
            out Vector4 cascadeSplitDistance, out ShadowSliceData shadowSliceData, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix)
        {
            ShadowSplitData splitData;
            Vector3 splitRotio = Vector3.one;
            if (shadowNearPlane > 1f)
            {
                float r = Mathf.Pow(shadowFarPlane / shadowNearPlane, 1f / shadowData.directionalLightCascadeCount);
                splitRotio = new Vector3(shadowNearPlane * r, shadowNearPlane * r * r, shadowNearPlane * r * r * r) / shadowFarPlane;
            }
            else
            {
                splitRotio = new Vector3(0.067f, 0.2f, 0.467f);
            }
            bool success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowLightIndex, cascadeIndex,
                shadowData.directionalLightCascadeCount, splitRotio, shadowResolution, shadowNearPlane, out viewMatrix, out projMatrix, out splitData);
            cascadeSplitDistance = splitData.cullingSphere;
            shadowSliceData.offsetX = (cascadeIndex % 2) * shadowResolution;
            shadowSliceData.offsetY = (cascadeIndex / 2) * shadowResolution;
            shadowSliceData.resolution = shadowResolution;
            shadowSliceData.shadowTransform = GetShadowTransform(projMatrix, viewMatrix);

            if (shadowData.directionalLightCascadeCount > 1)
                ApplySliceTransform(ref shadowSliceData, shadowData.directionalShadowAltasRes, shadowData.directionalShadowAltasRes);

            return success;
        }

        public static void ApplySliceTransform(ref ShadowSliceData shadowSliceData, int altaWidth, int altaHeight)
        {
            Matrix4x4 sliceTransform = Matrix4x4.identity;
            float oneOverAltaWidht = 1f / altaWidth;
            float oneOverAltaHeight = 1f / altaHeight;

            sliceTransform.m00 = shadowSliceData.resolution * oneOverAltaWidht;
            sliceTransform.m11 = shadowSliceData.resolution * oneOverAltaHeight;
            sliceTransform.m03 = shadowSliceData.offsetX * oneOverAltaWidht;
            sliceTransform.m13 = shadowSliceData.offsetY * oneOverAltaHeight;

            shadowSliceData.shadowTransform = sliceTransform * shadowSliceData.shadowTransform;
        }

        static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
        {
            if(SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;
            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;

            return textureScaleAndBias * worldToShadow;
        }

        public static void SetupShadowCasterConstants(CommandBuffer cmd, ref VisibleLight visibleLight, Matrix4x4 proj, float cascadeRes)
        {
            Light light = visibleLight.light;
            float bias = 0f;
            float normalBias = 0f;

            const float kernelRadius = 3.65f;

            if(visibleLight.lightType == LightType.Directional)
            {
                float sign = (SystemInfo.usesReversedZBuffer) ? 1f : -1f;
                bias = light.shadowBias * proj.m22 * sign;

                double frustumWidth = 2.0 / (double)proj.m00;
                double frustumHeight = 2.0 / (double)proj.m11;
                float texelSizeX = (float)(frustumWidth / (double)cascadeRes);
                float texelSizeY = (float)(frustumHeight / (double)cascadeRes);
                float texelSize = Mathf.Max(texelSizeX, texelSizeY);

                normalBias = -light.shadowNormalBias * texelSize * kernelRadius;
            }

            Vector3 lightDirection = -visibleLight.localToWorld.GetColumn(2);
            cmd.SetGlobalVector("_ShadowBias", new Vector4(bias, normalBias, 0f, 0f));
            cmd.SetGlobalVector("_LightDirection", lightDirection);
        }

        public static void RenderShadowSlice(CommandBuffer cmd, ref ScriptableRenderContext context, ref ShadowSliceData shadowSliceData,
            ref DrawShadowsSettings settings, Matrix4x4 proj, Matrix4x4 view)
        {
            cmd.SetViewport(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
            cmd.EnableScissorRect(new Rect(shadowSliceData.offsetX + 4, shadowSliceData.offsetY + 4, shadowSliceData.resolution - 8, shadowSliceData.resolution - 8));

            cmd.SetViewProjectionMatrices(view, proj);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawShadows(ref settings);
            cmd.DisableScissorRect();
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }

    public static class CoreUtils
    {
        public static void SetKeyword(CommandBuffer cmd, string keyword, bool state)
        {
            if (state)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        public static void Setkeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        public static Material CreateEngineMaterial(string shaderPath)
        {
            Shader shader = Shader.Find(shaderPath);
            if (shader == null)
            {
                Debug.LogError("Cannot create required material because shader " + shaderPath + " could not be found");
                return null;
            }

            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public static Material CreateEngineMaterial(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Cannot create required material because shader could not be found");
                return null;
            }

            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public static void Destroy(UnityObject obj)
        {
            if(obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
            }
        }

        public static RenderTextureDescriptor CreateRenderTextureDescriptor(ref CameraData cameraData, float scaler = 1.0f)
        {
            Camera camera = cameraData.camera;
            RenderTextureDescriptor desc = new RenderTextureDescriptor((int)(camera.pixelWidth * scaler), (int)(camera.pixelHeight * scaler));
            desc.colorFormat = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            desc.enableRandomWrite = false;
            desc.sRGB = true;

            return desc;
        }

    }
}