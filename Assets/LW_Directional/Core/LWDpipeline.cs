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
        private bool SupportDynamicBatching = true;
        private ShadowResolution ShadowAltasResolution = ShadowResolution._2048;
        private float ShadowDistance = 50.0f;
        private ShadowCascades ShadowCascades = ShadowCascades.FOUR_CASCADES;
        private bool useSoftShadow = false;

        public LWDpipeline(LWDAsset asset)
        {
            Shader.globalRenderPipeline = "LWD";

            SupportDynamicBatching = asset.SupportDynamicBatching;
            ShadowAltasResolution = asset.ShadowAltasResolution;
            ShadowDistance = asset.ShadowDistance;
            ShadowCascades = asset.ShadowCascades;
            useSoftShadow = asset.useSoftShadow;
        }

        public override void Dispose()
        {
            base.Dispose();
            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);

            GraphicsSettings.lightsUseLinearIntensity = true;
            SortCameras(cameras);
            foreach(Camera camera in cameras)
            {
                BeginCameraRendering(camera);

                
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

            cullingParam.shadowDistance = Mathf.Min(200f, camera.farClipPlane);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

#if UNITY_EDITOR
            //SceneView Camera
#endif
            CullResults.Cull(ref cullingParam, context, ref cullResults);
            RenderingData renderingData;
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

            cameraData.maxShadowDistance = 200f;         
        }

        public void SortCameras(Camera[] cameras)
        {
            Array.Sort(cameras, (lhs, rhs) => (int)(lhs.depth - rhs.depth));
        }
    }
}
