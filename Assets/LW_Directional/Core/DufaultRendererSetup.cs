using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using MZ.LWD;

public class DufaultRendererSetup : IRendererSetup
{
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;
    private DirectionalShadowPass m_DirectionalShadowPass;
    private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowPass;
    private DepthOnlyPass m_DepthOnlyPass;
    private SetupLWDConstantsPass m_SetupLWDConstants;

    private RenderTargetHandle ColorAttachment;
    private RenderTargetHandle DepthAttachment;
    private RenderTargetHandle DepthTexture;
    private RenderTargetHandle OpaqueColor;
    private RenderTargetHandle DirectionalShadowmap;
    private RenderTargetHandle ScreenSpaceShadowmap;

    private bool m_Inited = false;

    private void Init()
    {
        if (m_Inited)
            return;

        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
        m_DirectionalShadowPass = new DirectionalShadowPass();
        m_ScreenSpaceShadowPass = new ScreenSpaceShadowResolvePass();
        m_DepthOnlyPass = new DepthOnlyPass();

        ColorAttachment.Init("_CameraColorTexture");
        DepthAttachment.Init("_CameraDepthAttachment");
        DepthTexture.Init("_DepthTexture");
        OpaqueColor.Init("_CameraOpaqueTexture");
        DirectionalShadowmap.Init("_DirectionalShadowmapTexture");
        ScreenSpaceShadowmap.Init("_ScreenSpaceShadowmapTexture");

        m_Inited = true;
    }

    public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Init();

        Camera camera = renderingData.cameraData.camera;

        RenderTextureDescriptor baseDesc = CoreUtils.CreateRenderTextureDescriptor(ref renderingData.cameraData);
        RenderTextureDescriptor shadowDesc = baseDesc;
        shadowDesc.dimension = TextureDimension.Tex2D;

        m_DirectionalShadowPass.Setup(DirectionalShadowmap);
        renderer.EnqueuePass(m_DirectionalShadowPass);

        m_DepthOnlyPass.Setup(baseDesc, DepthTexture, 1);
        renderer.EnqueuePass(m_DepthOnlyPass);

        m_ScreenSpaceShadowPass.Setup(baseDesc, ScreenSpaceShadowmap);
        renderer.EnqueuePass(m_ScreenSpaceShadowPass);

        m_SetupLWDConstants.Setup();
        renderer.EnqueuePass(m_SetupLWDConstants);
    }
}

