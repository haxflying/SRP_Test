using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MZ.LWD;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class RenderOpaqueForwardPass : ScriptableRenderPass
{
    const string k_RenderOpaqueTag = "Render Opaques";
    FilterRenderersSettings m_OpaqueFilterSettings;
    RenderTargetHandle colorAttachmentHandle { get; set; }
    RenderTargetHandle depthAttachmentHandle { get; set; }
    RenderTextureDescriptor desc { get; set; }
    ClearFlag clearFlag { get; set; }
    Color clearColor { get; set; }
    RendererConfiguration rendererConfig;

    public override string GetName()
    {
        return k_RenderOpaqueTag;
    }

    public RenderOpaqueForwardPass()
    {
        RegisterShaderPassName("ForwardBase");

        m_OpaqueFilterSettings = new FilterRenderersSettings(true)
        {
            renderQueueRange = RenderQueueRange.opaque,
        };
    }

    public void Setup(RenderTextureDescriptor baseDesc, RenderTargetHandle colorAttachmentHandle, 
        RenderTargetHandle depthAttachmentHandle, ClearFlag clearFlag, Color clearColor, RendererConfiguration config)
    {
        this.colorAttachmentHandle = colorAttachmentHandle;
        this.depthAttachmentHandle = depthAttachmentHandle;
        this.clearColor = clearColor;
        this.clearFlag = clearFlag;
        this.desc = baseDesc;
        this.rendererConfig = config;
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = commandBufferPool.Get(k_RenderOpaqueTag);

        RenderBufferLoadAction loadOp = RenderBufferLoadAction.DontCare;
        RenderBufferStoreAction storeOp = RenderBufferStoreAction.Store;

        SetRenderTarget(cmd, colorAttachmentHandle.Identifier(), loadOp, storeOp, depthAttachmentHandle.Identifier(),
            loadOp, storeOp, clearFlag, clearColor, desc.dimension);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        Camera camera = renderingData.cameraData.camera;
        var drawSettings = CreateDrawRendererSettings(camera, SortFlags.CommonOpaque, rendererConfig, renderingData.supportsDynamicBatching);
        context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, m_OpaqueFilterSettings);

        commandBufferPool.Release(cmd);
    }
}

