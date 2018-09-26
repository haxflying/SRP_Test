using MZ.LWD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public abstract class ScriptableRenderPass
{
    private List<ShaderPassName> m_ShaderPassNames = new List<ShaderPassName>();

    public virtual string GetName()
    {
        return "default parent";
    }

    protected void RegisterShaderPassName(string passName)
    {
        m_ShaderPassNames.Add(new ShaderPassName(passName));
    }

    public virtual void FrameCleanup(CommandBuffer cmd)
    { }

    public abstract void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData);

    protected DrawRendererSettings CreateDrawRendererSettings(Camera camera, SortFlags sortFlags, RendererConfiguration rendererConfiguration, bool supportDynamicBatching)
    {
        
        DrawRendererSettings settings = new DrawRendererSettings(camera, m_ShaderPassNames[0]);
        for(int i = 1; i < m_ShaderPassNames.Count; i++)
        {
            settings.SetShaderPassName(i, m_ShaderPassNames[i]);
        }
        settings.sorting.flags = sortFlags;
        settings.rendererConfiguration = rendererConfiguration;
        settings.flags = DrawRendererFlags.EnableInstancing;
        if (supportDynamicBatching)
            settings.flags |= DrawRendererFlags.EnableDynamicBatching;
        return settings;
    }

    protected static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderBufferLoadAction colorLoadAction,
        RenderBufferStoreAction colorStoreAction, ClearFlag clearFlag, Color clearColor, TextureDimension dimension)
    {
        if (dimension == TextureDimension.Tex2DArray)
        {
            cmd.SetRenderTarget(colorAttachment, 0, CubemapFace.Unknown, -1);
            cmd.ClearRenderTarget(clearFlag == ClearFlag.Depth || clearFlag == ClearFlag.All,
                clearFlag == ClearFlag.Color || clearFlag == ClearFlag.All, clearColor);
        }
        else
        {
            cmd.SetRenderTarget(colorAttachment, colorLoadAction, colorStoreAction);
            cmd.ClearRenderTarget(clearFlag == ClearFlag.Depth || clearFlag == ClearFlag.All,
                clearFlag == ClearFlag.Color || clearFlag == ClearFlag.All, clearColor);
        }
    }

    protected static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderBufferLoadAction colorLoadAction,
        RenderBufferStoreAction colorStoreAction, RenderTargetIdentifier depthAttachment, RenderBufferLoadAction depthLoadAction,
        RenderBufferStoreAction depthStoreAction, ClearFlag clearFlag, Color clearColor, TextureDimension dimension)
    {
        if (dimension == TextureDimension.Tex2DArray)
        {
            cmd.SetRenderTarget(colorAttachment, depthAttachment, 0, CubemapFace.Unknown, -1);
            cmd.ClearRenderTarget(clearFlag == ClearFlag.Depth || clearFlag == ClearFlag.All,
                clearFlag == ClearFlag.Color || clearFlag == ClearFlag.All, clearColor);
        }
        else
        {
            cmd.SetRenderTarget(colorAttachment, colorLoadAction, colorStoreAction, depthAttachment, depthLoadAction, depthStoreAction);
            cmd.ClearRenderTarget(clearFlag == ClearFlag.Depth || clearFlag == ClearFlag.All,
                clearFlag == ClearFlag.Color || clearFlag == ClearFlag.All, clearColor);
        }
    }
}

