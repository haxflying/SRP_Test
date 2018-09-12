using MZ.LWD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public sealed class ScriptableRenderer
{
    readonly Material[] m_Materials;

    public ScriptableRenderer(LWDAsset asset)
    {
        m_Materials = new[]
        {
            CoreUtils.CreateEngineMaterial(asset.opaqueForward),
            CoreUtils.CreateEngineMaterial(asset.directionalShadow),
            CoreUtils.CreateEngineMaterial(asset.screenSpaceShadow),
        };
    }

    public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        for (int i = 0; i < m_ActiveRenderPassQueue.Count; i++)
        {
            Debug.Log(m_ActiveRenderPassQueue[i].GetName());
            m_ActiveRenderPassQueue[i].Execute(this, context, ref renderingData);
        }

        DisposePasses(ref context);
    }

    List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>();
    List<ShaderPassName> m_ShaderPassNames = new List<ShaderPassName>()
    {
        new ShaderPassName("Always"), 
        new ShaderPassName("ForwardBase"),
        new ShaderPassName("PrepassBase"),
    };

    public void EnqueuePass(ScriptableRenderPass pass)
    {
        m_ActiveRenderPassQueue.Add(pass);
    }

    public void Clear()
    {
        m_ActiveRenderPassQueue.Clear();
    }

    void DisposePasses(ref ScriptableRenderContext context)
    {
        var cmd = commandBufferPool.Get("ReleasePasses");

        for (int i = 0; i < m_ActiveRenderPassQueue.Count; i++)
        {
            m_ActiveRenderPassQueue[i].FrameCleanup(cmd);
        }
        context.ExecuteCommandBuffer(cmd);
        commandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        for (int i = 0; i < m_Materials.Length; i++)
        {
            CoreUtils.Destroy(m_Materials[i]);
        }
    }

    public static ClearFlag GetClearFlag(Camera camera)
    {
        ClearFlag clearFlag = ClearFlag.None;
        CameraClearFlags cameraClearFlags = camera.clearFlags;
        if (cameraClearFlags != CameraClearFlags.Nothing)
        {
            clearFlag |= ClearFlag.Depth;
            if (cameraClearFlags == CameraClearFlags.Color || cameraClearFlags == CameraClearFlags.Skybox)
                clearFlag |= ClearFlag.Color;
        }

        return clearFlag;
    }
    
}

