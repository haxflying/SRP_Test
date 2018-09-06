using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MZ.LWD;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

class DirectionalShadowPass : ScriptableRenderPass
{
    const string k_RenderDirectionalShadowmapTag = "Render Directional Shadowmap";

    private static class DirectionalShadowConstantBuffer
    {
        public static int _WorldToShadow;
        public static int _ShadowData;
        public static int _DirShadowSplitSpheres0;
        public static int _DirShadowSplitSpheres1;
        public static int _DirShadowSplitSpheres2;
        public static int _DirShadowSplitSpheres3;
        public static int _DirShadowSplitSphereRadii;
        public static int _ShadowOffset0;
        public static int _ShadowOffset1;
        public static int _ShadowOffset2;
        public static int _ShadowOffset3;
        public static int _ShadowmapSize;
    }

    const int k_MaxCascades = 4;
    const int k_ShadowmapBufferBits = 16;
    int m_ShadowCasterCascadesCount;

    RenderTexture m_DirectionalShadowmapTexture;
    RenderTextureFormat m_ShadowmapFormat;

    Matrix4x4[] m_DirectionalShadowMatrices;
    ShadowSliceData[] m_CascadeSlices;
    Vector4[] m_CascadeSplitDistances;

    private RenderTargetHandle destination { get; set; }

    public DirectionalShadowPass()
    {
        RegisterShaderPassName("ShaderCaster");

        m_DirectionalShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
        m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
        m_CascadeSplitDistances = new Vector4[k_MaxCascades];

        DirectionalShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_WorldToShadow");
        DirectionalShadowConstantBuffer._ShadowData = Shader.PropertyToID("_ShadowData");
        DirectionalShadowConstantBuffer._DirShadowSplitSpheres0 = Shader.PropertyToID("_DirShadowSplitSpheres0");
        DirectionalShadowConstantBuffer._DirShadowSplitSpheres1 = Shader.PropertyToID("_DirShadowSplitSpheres1");
        DirectionalShadowConstantBuffer._DirShadowSplitSpheres2 = Shader.PropertyToID("_DirShadowSplitSpheres2");
        DirectionalShadowConstantBuffer._DirShadowSplitSpheres3 = Shader.PropertyToID("_DirShadowSplitSpheres3");
        DirectionalShadowConstantBuffer._DirShadowSplitSphereRadii = Shader.PropertyToID("_DirShadowSplitSphereRadii");
        DirectionalShadowConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_ShadowOffset0");
        DirectionalShadowConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_ShadowOffset1");
        DirectionalShadowConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_ShadowOffset2");
        DirectionalShadowConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_ShadowOffset3");
        DirectionalShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_ShadowmapSize");

        m_ShadowmapFormat = RenderTextureFormat.Shadowmap;
    }

    public void Setup(RenderTargetHandle destination)
    {
        this.destination = destination;
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Clear();

    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        if(m_DirectionalShadowmapTexture)
        {
            RenderTexture.ReleaseTemporary(m_DirectionalShadowmapTexture);
            m_DirectionalShadowmapTexture = null;
        }
    }

    void Clear()
    {
        m_DirectionalShadowmapTexture = null;

        for (int i = 0; i < m_DirectionalShadowMatrices.Length; ++i)
            m_DirectionalShadowMatrices[i] = Matrix4x4.identity;

        for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
            m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

        for (int i = 0; i < m_CascadeSlices.Length; ++i)
            m_CascadeSlices[i].Clear();
    }

    void RenderDirectionalCascadeShadowmap(ref ScriptableRenderContext context, ref CullResults cullResults, 
        ref LightData lightData, ref ShadowData shadowData)
    {
        int shadowLightIndex = lightData.mainLightIndex;
        if (shadowLightIndex == -1)
            return;

        VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
        Light light = shadowLight.light;

        if (light.shadows == LightShadows.None)
            return;

        Bounds bounds;
        if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
            return;

        CommandBuffer cmd = commandBufferPool.Get(k_RenderDirectionalShadowmapTag);

        {
            m_ShadowCasterCascadesCount = shadowData.directionalLightCascadeCount;

            int shadowResolution = (m_ShadowCasterCascadesCount > 1) ?
                (int)(shadowData.directionalShadowAltasRes / 2f) : shadowData.directionalShadowAltasRes;
            float shadowNearPlane = light.shadowNearPlane;

            Matrix4x4 view, proj;
            DrawShadowsSettings settings = new DrawShadowsSettings(cullResults, shadowLightIndex);

            m_DirectionalShadowmapTexture = RenderTexture.GetTemporary(shadowData.directionalShadowAltasRes,
                shadowData.directionalShadowAltasRes, k_ShadowmapBufferBits, m_ShadowmapFormat);
            m_DirectionalShadowmapTexture.filterMode = FilterMode.Bilinear;
            m_DirectionalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

            SetRenderTarget(cmd, m_DirectionalShadowmapTexture, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store, ClearFlag.Depth, Color.black, TextureDimension.Tex2D);

            bool success = false;
            for (int i = 0; i < m_ShadowCasterCascadesCount; i++)
            {
                
            }
        }
    }
}

