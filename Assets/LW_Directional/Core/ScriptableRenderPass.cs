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
}

