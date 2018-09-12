using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MZ.LWD;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class SetupLWDConstantsPass : ScriptableRenderPass
{
    public static class LightConstantBuffer
    {
        public static int _MainLightPostion;
        public static int _MainLightColor;
        public static int _WorldToLight;
    }

    const string k_SetupLightConstants = "Setup Light Constants";

    //TODO : Shadowmask support

    Vector4 k_DefaultLightPosition = new Vector4(0f, 0f, 1f, 0f);
    Vector4 k_DefaultLightColor = Color.black;

    public override string GetName()
    {
        return k_SetupLightConstants;
    }

    public SetupLWDConstantsPass()
    {
        LightConstantBuffer._MainLightPostion = Shader.PropertyToID("_MainLightPosition");
        LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
        LightConstantBuffer._WorldToLight = Shader.PropertyToID("_WorldToLight");
    }

    public void Setup()
    {

    }

    void InitializeLightConstants(List<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor)
    {
        lightPos = k_DefaultLightPosition;
        lightColor = k_DefaultLightColor;

        if (lightIndex < 0)
            return;

        VisibleLight lightData = lights[lightIndex];
        if(lightData.lightType == LightType.Directional)
        {
            Vector4 dir = -lightData.localToWorld.GetColumn(2);
            lightPos = dir;
        }

        lightColor = lightData.finalColor;
    }

    void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
    {
        SetupMainLightConstants(cmd, ref lightData);
    }

    void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
    {
        Vector4 lightpos, lightColor;
        List<VisibleLight> lights = lightData.visibleLights;
        InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightpos, out lightColor);

        cmd.SetGlobalVector(LightConstantBuffer._MainLightPostion, lightpos);
        cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = commandBufferPool.Get(k_SetupLightConstants);
        SetupShaderLightConstants(cmd, ref renderingData.lightData);
        //TODO : Set shader keyword
        context.ExecuteCommandBuffer(cmd);
        commandBufferPool.Release(cmd);
    }
}

