using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


namespace MZ.LWD
{
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
    
    
        
}