using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MZ.LWD;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class DepthOnlyPass : ScriptableRenderPass
{
    const string k_DepthPrepassTag = "Depth Prepass";

    int kDepthBufferBits = 32;

    private RenderTargetHandle depthAttachmentHandle { get; set; }
    internal RenderTextureDescriptor desc { get; set; }
    private FilterRenderersSettings opaqueFilterSettings { get; set; }

    public void Setup(RenderTextureDescriptor baseDesc, RenderTargetHandle depthAttachmentHandle, int sampleCount)
    {
        this.depthAttachmentHandle = depthAttachmentHandle;
        baseDesc.colorFormat = RenderTextureFormat.Depth;
        baseDesc.depthBufferBits = kDepthBufferBits;

        if(sampleCount > 1)
        {
            baseDesc.bindMS = true;
            baseDesc.msaaSamples = sampleCount;
        }

        desc = baseDesc;
    }

    public DepthOnlyPass()
    {
        RegisterShaderPassName("DepthOnly");
        opaqueFilterSettings = new FilterRenderersSettings
        {
            renderQueueRange = RenderQueueRange.opaque,
        };
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = commandBufferPool.Get(k_DepthPrepassTag);

        {
            cmd.GetTemporaryRT(depthAttachmentHandle.id, desc, FilterMode.Point);
            SetRenderTarget(cmd, depthAttachmentHandle.Identifier(), UnityEngine.Rendering.RenderBufferLoadAction.DontCare,
                 UnityEngine.Rendering.RenderBufferStoreAction.Store, ClearFlag.Depth, Color.black, desc.dimension);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var drawSetting = CreateDrawRendererSettings(renderingData.cameraData.camera, SortFlags.CommonOpaque, RendererConfiguration.None, renderingData.supportsDynamicBatching);
            context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSetting, opaqueFilterSettings);
        }
        
        commandBufferPool.Release(cmd);
    }
}

