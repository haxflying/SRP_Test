using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace MZ.LWD
{
    public class LWDpipeline : RenderPipeline
    {
        private static bool SupportDynamicBatching = true;
        private static ShadowResolution ShadowAltasResolution = ShadowResolution._2048;
        private static float ShadowDistance = 50.0f;
        private static ShadowCascades ShadowCascades = ShadowCascades.FOUR_CASCADES;
        private static bool useSoftShadow = false;

        private static IRendererSetup m_DefaultRenderSetup;
        private static IRendererSetup defaultRenderSetup
        {
            get
            {
                if (m_DefaultRenderSetup == null)
                    m_DefaultRenderSetup = new DufaultRendererSetup();

                return m_DefaultRenderSetup;
            }
        }

        ScriptableRenderer m_Renderer;
        CullResults m_Cullresults;        

        public LWDpipeline(LWDAsset asset)
        {
            Shader.globalRenderPipeline = "LWD";

            SupportDynamicBatching = asset.SupportDynamicBatching;
            ShadowAltasResolution = asset.ShadowAltasResolution;
            ShadowDistance = asset.ShadowDistance;
            ShadowCascades = asset.ShadowCascades;
            useSoftShadow = asset.useSoftShadow;

            m_Renderer = new ScriptableRenderer(asset);
        }

        public override void Dispose()
        {
            base.Dispose();
            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

            m_Renderer.Dispose();
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);

            GraphicsSettings.lightsUseLinearIntensity = true;
            SortCameras(cameras);
            foreach(Camera camera in cameras)
            {
                BeginCameraRendering(camera);

                RenderSingleCamera(context, camera, ref m_Cullresults, camera.GetComponent<IRendererSetup>(), m_Renderer);              
            }
        }

        public static void RenderSingleCamera(ScriptableRenderContext context, Camera camera, 
            ref CullResults cullResults, IRendererSetup setup, ScriptableRenderer renderer)
        {
            CommandBuffer cmd = commandBufferPool.Get("RenderSingleCam");

            CameraData cameraData;
            InitializeCameraData(camera,out cameraData);

            ScriptableCullingParameters cullingParam;
            if(!CullResults.GetCullingParameters(camera, false, out cullingParam))
            {
                commandBufferPool.Release(cmd);
                return;
            }

            cullingParam.shadowDistance = Mathf.Min(ShadowDistance, camera.farClipPlane);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

#if UNITY_EDITOR
            //SceneView Camera
#endif
            CullResults.Cull(ref cullingParam, context, ref cullResults);
            RenderingData renderingData;
            InitializeRenderingData(ref cameraData, ref cullResults, out renderingData);

            IRendererSetup setupToUse = setup;
            if (setupToUse == null)
                setupToUse = defaultRenderSetup;

            renderer.Clear();
            setupToUse.Setup(renderer, ref renderingData);
            renderer.Execute(context, ref renderingData);

            context.ExecuteCommandBuffer(cmd);
            commandBufferPool.Release(cmd);
            context.Submit();
        }

        static void InitializeCameraData(Camera camera, out CameraData cameraData)
        {
            cameraData.camera = camera;

            cameraData.isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            cameraData.isOffscreenRender = camera.targetTexture != null && !cameraData.isSceneViewCamera;
            cameraData.isHdrEnabled = camera.allowHDR;

            Rect cameraRect = camera.rect;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            cameraData.maxShadowDistance = ShadowDistance;         
        }

        static void InitializeRenderingData(ref CameraData cameraData, ref CullResults cullResults, out RenderingData renderingData)
        {
            List<VisibleLight> visibleLights = cullResults.visibleLights;
            List<int> localLightIndices = new List<int>();

            bool hasDirectionalShadowCastingLight = false;
            bool hasLocalShadowCastingLight = false;

            if (cameraData.maxShadowDistance > 0f)
            {
                for (int i = 0; i < visibleLights.Count; ++i)
                {
                    Light light = visibleLights[i].light;
                    bool castShadow = light != null && light.shadows != LightShadows.None;
                    if(visibleLights[i].lightType == LightType.Directional)
                    {
                        hasDirectionalShadowCastingLight |= castShadow;
                    }
                    else
                    {
                        hasLocalShadowCastingLight |= castShadow;
                        localLightIndices.Add(i);
                    }
                }
            }

            renderingData.cullResults = cullResults;
            renderingData.cameraData = cameraData;
            InitializeLightData(visibleLights, 4, localLightIndices, out renderingData.lightData);
            InitializeShadowData(hasDirectionalShadowCastingLight, hasLocalShadowCastingLight, out renderingData.shadowData);
            renderingData.supportsDynamicBatching = SupportDynamicBatching;
        }

        static void InitializeShadowData(bool hasDirShadow, bool hasLocalShadow, out ShadowData shadowData)
        {
            shadowData.directionalShadowAltasRes = 2048;
            switch (ShadowAltasResolution)
            {
                case ShadowResolution._1024:
                    shadowData.directionalShadowAltasRes = 1024;break;
                case ShadowResolution._2048:
                    shadowData.directionalShadowAltasRes = 2048; break;
                case ShadowResolution._4096:
                    shadowData.directionalShadowAltasRes = 4096; break;
            }

            shadowData.directionalLightCascadeCount = 2;
            switch (ShadowCascades)
            {
                case ShadowCascades.FOUR_CASCADES:
                    shadowData.directionalLightCascadeCount = 4; break;
                case ShadowCascades.TWO_CASCADES:
                    shadowData.directionalLightCascadeCount = 2; break;
                case ShadowCascades.NO_CASCADES:
                    shadowData.directionalLightCascadeCount = 1; break;
            }

            shadowData.supportSoftShadows = useSoftShadow;
            shadowData.bufferBitCount = 16;
        }

        static void InitializeLightData(List<VisibleLight> visibleLights, 
            int maxLocalLightPerPass, List<int> localLightIndices, out LightData lightData)
        {
            int visibleLightCount = Math.Min(visibleLights.Count, 4);
            lightData.mainLightIndex = GetMainLight(visibleLights);

            int mainLightPresent = (lightData.mainLightIndex >= 0) ? 1 : 0;
            int additionPixelLightCount = Math.Min(visibleLightCount - mainLightPresent, maxLocalLightPerPass);

            lightData.pixelAdditionalLightsCount = additionPixelLightCount;
            lightData.visibleLights = visibleLights;
            lightData.visibleLocalLightIndices = localLightIndices;
        }

        static int GetMainLight(List<VisibleLight> visibleLights)
        {
            for (int i = 0; i < visibleLights.Count; i++)
            {
                VisibleLight vl = visibleLights[i];
                if(vl.light.shadows != LightShadows.None && vl.lightType == LightType.Directional)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SortCameras(Camera[] cameras)
        {
            Array.Sort(cameras, (lhs, rhs) => (int)(lhs.depth - rhs.depth));
        }
    }
}
