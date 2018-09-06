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

    public static class CoreUtils
    {
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