using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MZ.LWD;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class ScreenSpaceShadowResolvePass : ScriptableRenderPass
{
    const string k_CollectShadowTag = "Collect Shadows";
    RenderTextureFormat m_ColorFormat;

    public ScreenSpaceShadowResolvePass()
    {
        m_ColorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ?
            RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
    }

    private RenderTargetHandle colorAttachmentHandle { get; set; }
    private RenderTextureDescriptor desc { get; set; }

    public void Setup(RenderTextureDescriptor baseDesc, RenderTargetHandle colorAttachmentHandle)
    {
        this.colorAttachmentHandle = colorAttachmentHandle;
        baseDesc.depthBufferBits = 0;
        baseDesc.colorFormat = m_ColorFormat;
        desc = baseDesc;
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.lightData.mainLightIndex == -1)
            return;

        var cmd = commandBufferPool.Get(k_CollectShadowTag);
        cmd.GetTemporaryRT(colorAttachmentHandle.id, desc, FilterMode.Bilinear);

        VisibleLight shadowLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex];
        SetShadowCollectPassKeywords(cmd, ref shadowLight, ref renderingData.shadowData);

        RenderTargetIdentifier screenSpaceOcclusionTexture = colorAttachmentHandle.Identifier();
        SetRenderTarget(cmd, screenSpaceOcclusionTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
             ClearFlag.All, Color.white, desc.dimension);

        context.ExecuteCommandBuffer(cmd);
        commandBufferPool.Release(cmd);
    }

    void SetShadowCollectPassKeywords(CommandBuffer cmd, ref VisibleLight shadowLight, ref ShadowData shadowData)
    {
        
    }
}

