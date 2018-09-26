using System;
using System.Collections;
using System.Collections.Generic;
using MZ.LWD;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class MipShadowPass : ScriptableRenderPass
{
    private string k_mipTag = "Mip";

    private RenderTargetHandle source { get; set; }
    private RenderTargetHandle destination { get; set; }

    public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
    {
        this.source = source;
        this.destination = destination;
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = commandBufferPool.Get(k_mipTag);
        int res = renderingData.shadowData.directionalShadowAltasRes;
        RenderTextureDescriptor desc = new RenderTextureDescriptor(res, res, RenderTextureFormat.RG32, 0);
        desc.autoGenerateMips = true;
        desc.useMipMap = true;

        RenderTexture buffer = RenderTexture.GetTemporary(desc);
        buffer.filterMode = FilterMode.Trilinear;
        cmd.SetRenderTarget(buffer);
        cmd.Blit(source.Identifier(), buffer);

        cmd.GetTemporaryRT(destination.id, res, res, 0, FilterMode.Bilinear, RenderTextureFormat.RG32);
        cmd.SetGlobalFloat("_MipLevel", VarInstance.instance.mipLevel);
        cmd.Blit(buffer, destination.Identifier(), renderer.GetMaterial(MaterialHandles.Blit));

        context.ExecuteCommandBuffer(cmd);
        RenderTexture.ReleaseTemporary(buffer);
        cmd.ReleaseTemporaryRT(destination.id);
        commandBufferPool.Release(cmd);
    }
}
