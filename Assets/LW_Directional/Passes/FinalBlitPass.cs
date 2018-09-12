using UnityEngine;
using System.Collections;
using UnityEngine;
using MZ.LWD;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class FinalBlitPass : ScriptableRenderPass
{
    const string k_FinalBlitTag = "Final Blit Pass";

    private RenderTargetHandle colorAttachmentHandle { get; set; }
    private RenderTextureDescriptor desc { get; set; }

    public void Setup(RenderTextureDescriptor baseDesc, RenderTargetHandle colorAttachmentHandle)
    {
        this.colorAttachmentHandle = colorAttachmentHandle;
        this.desc = baseDesc;
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Material material = renderer.GetMaterial(MaterialHandles.Blit);
        RenderTargetIdentifier sourceRT = colorAttachmentHandle.Identifier();

        CommandBuffer cmd = commandBufferPool.Get(k_FinalBlitTag);
        cmd.SetGlobalTexture("_BlitTex", sourceRT);

        //if(!renderingData.cameraData.isDefaultViewport)

        cmd.Blit(colorAttachmentHandle.Identifier(), BuiltinRenderTextureType.CameraTarget, material);

        context.ExecuteCommandBuffer(cmd);
        commandBufferPool.Release(cmd);
    }

}
