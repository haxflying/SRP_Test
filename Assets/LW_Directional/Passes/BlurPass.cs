using UnityEngine;
using System.Collections;
using MZ.LWD;
using UnityEngine.Experimental.Rendering;

public class BlurPass : ScriptableRenderPass
{
    private string k_blurTag = "Blur";

    private int iteration = 3;
    private float blurSpread = 0.65f;
    private int downSample = 2;

    private RenderTargetHandle source { get; set; }
    private RenderTargetHandle destination { get; set; }
    private RenderTextureDescriptor blurDecs;
    private Material m_Material;

    public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
    {
        this.source = source;
        this.destination = destination;        
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = commandBufferPool.Get(k_blurTag);
        m_Material = renderer.GetMaterial(MaterialHandles.Blur);
        int res = renderingData.shadowData.directionalShadowAltasRes / downSample;
        cmd.GetTemporaryRT(destination.id, res, res, 0, FilterMode.Bilinear, RenderTextureFormat.RG32);

        RenderTexture buffer0 = RenderTexture.GetTemporary(res, res, 0, RenderTextureFormat.RG32, RenderTextureReadWrite.Linear);

        cmd.Blit(source.Identifier(), buffer0);

        

        for (int i = 0; i < iteration; i++)
        {
            m_Material.SetFloat("_BlurSize", 1.0f + i * blurSpread);
            RenderTexture buffer1 = RenderTexture.GetTemporary(res, res, 0, RenderTextureFormat.RG32, RenderTextureReadWrite.Linear);

            cmd.Blit(buffer0, buffer1, m_Material, 0);

            RenderTexture.ReleaseTemporary(buffer0);
            buffer0 = buffer1;
            buffer1 = RenderTexture.GetTemporary(res, res, 0, RenderTextureFormat.RG32, RenderTextureReadWrite.Linear);

            cmd.Blit(buffer0, buffer1, m_Material, 1);

            RenderTexture.ReleaseTemporary(buffer0);
            buffer0 = buffer1;
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        cmd.GetTemporaryRT(destination.id, res, res, 0, FilterMode.Bilinear, RenderTextureFormat.RG32);
        cmd.Blit(buffer0, destination.Identifier());
        RenderTexture.ReleaseTemporary(buffer0);

        context.ExecuteCommandBuffer(cmd);
        commandBufferPool.Release(cmd);
    }
}
