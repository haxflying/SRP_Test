﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class pipeline_basic_asset : RenderPipelineAsset {

    public Color clearColor = Color.green;

    public const int MAX_SHADOWCASTERS = 4;
    public int maxLights = 8;

    public int shadowMapSize = 2048;

#if UNITY_EDITOR
    [UnityEditor.MenuItem("SRP/basic")]
    static void CreateBasicPipeline()
    {
        var instance = ScriptableObject.CreateInstance<pipeline_basic_asset>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/pipeline_assets/basic.asset");
    }
#endif

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new pipeline_basic_instance(this);
    }
}

public class pipeline_basic_instance : RenderPipeline
{
    private Color m_ClearColor = Color.black;
    private pipeline_basic_asset pipelineAsset;

    //shadow
    private RenderTextureDescriptor shadowMapDesc;
    RenderTexture shadowRT;
    RenderTargetIdentifier shadowRTID;
    RenderTargetIdentifier tempRTID;

    readonly LightComparer lightComparer;

    public pipeline_basic_instance(pipeline_basic_asset asset) : base()
    {
        pipelineAsset = asset;
        m_ClearColor = asset.clearColor;

        shadowMapDesc = new RenderTextureDescriptor(asset.shadowMapSize, asset.shadowMapSize, RenderTextureFormat.RGHalf, 24)
        {
            dimension = TextureDimension.Tex2D,
            volumeDepth = 1,
            msaaSamples = 1
        };

        shadowRT = new RenderTexture(shadowMapDesc) { name = "Shadow Depth Tex" };
        shaderLib.Variables.Global.id_ShadowTex = Shader.PropertyToID(shaderLib.Variables.Global.SHADOW_TEX);
        shaderLib.Variables.Global.id_TempTex = Shader.PropertyToID(shaderLib.Variables.Global.TEMP_TEX);
    }

    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);

        foreach (var camera in cameras)
        {
            ScriptableCullingParameters cullingParams;
            if (!CullResults.GetCullingParameters(camera, out cullingParams))
                continue;

            cullingParams.shadowDistance = Mathf.Min(2000, camera.farClipPlane);

            CullResults cull = CullResults.Cull(ref cullingParams, renderContext);
            
            List<VisibleLight> visibleLights = cull.visibleLights;
            Light shadowLight = SetupLightBuffers(renderContext, visibleLights, camera.worldToCameraMatrix);

            shadowMapDesc.width = pipelineAsset.shadowMapSize;
            shadowMapDesc.height = pipelineAsset.shadowMapSize;

            if(shadowLight != null)
                //ShadowPass

            renderContext.SetupCameraProperties(camera);

            var cmd = new CommandBuffer();
            cmd.ClearRenderTarget(true, false, m_ClearColor);
            renderContext.ExecuteCommandBuffer(cmd);

            cmd.Release();

            var settings = new DrawRendererSettings(camera, new ShaderPassName("basic"));
            settings.sorting.flags = SortFlags.CommonOpaque;

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };
            renderContext.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

            var shadowSettings = new DrawShadowsSettings(cull, 0);
            //renderContext.DrawShadows(ref shadowSettings);

            renderContext.DrawSkybox(camera);

            settings.sorting.flags = SortFlags.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            renderContext.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);
        }
        renderContext.Submit();
    }

    private Light SetupLightBuffers(ScriptableRenderContext context, List<VisibleLight> lights, Matrix4x4 viewMatrix)
    {
        Light shadowLight = null;
        int shadowLightID = -1;

        int maxLights = pipelineAsset.maxLights;
        int lightCount = 0;

        Vector4[] lightColors = new Vector4[maxLights];
        Vector4[] lightPositions = new Vector4[maxLights];
        Vector4[] lightAtten = new Vector4[maxLights];
        Vector4[] lightSpotDirection = new Vector4[maxLights];

        lights.Sort(lightComparer);

        for (int i = 0; i < lights.Count; i++)
        {
            if (lightCount == maxLights)
                break;

            VisibleLight vl = lights[i];
            if (vl.light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
                continue;

            Color lightColor = vl.finalColor;
            // we will be able to multiply out any light data that isn't a mixed light
            // this will help better with blending on lightmapped objects
            lightColor.a = vl.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed ? 0f : 1f;
            lightColors[lightCount] = lightColor;

            float sqrRange = vl.range * vl.range;
            float quadAtten = 25f / sqrRange;

            if(vl.lightType == LightType.Directional)
            {
                Vector4 dir = viewMatrix * vl.localToWorld.GetColumn(2);
                lightPositions[lightCount] = new Vector4(-dir.x, -dir.y, -dir.z, 0);
                lightAtten[lightCount] = new Vector4(-1, 1, 0, 0);
            }
            else if(vl.lightType == LightType.Point)
            {
                //TODO
            }
            else if(vl.lightType == LightType.Spot)
            {
                //TODO
            }
            else if(vl.lightType == LightType.Area)
            {
                //TODO
            }

            if (vl.light.shadows != LightShadows.None && shadowLightID < 0)
                shadowLightID = i;

            lightCount++;
        }

        if (shadowLightID >= 0)
            shadowLight = lights[shadowLightID].light;

        //set shader global
        CommandBuffer cmd = commandBufferPool.Get();
        cmd.SetGlobalVectorArray(shaderLib.Variables.Global.LIGHTS_COLOR, lightColors);
        cmd.SetGlobalVectorArray(shaderLib.Variables.Global.LIGHTS_POSITION, lightPositions);
        cmd.SetGlobalVectorArray(shaderLib.Variables.Global.LIGHTS_ATTEN, lightAtten);
        cmd.SetGlobalVectorArray(shaderLib.Variables.Global.LIGHTS_SPOT_DIRS, lightSpotDirection);
        cmd.SetGlobalVector(shaderLib.Variables.Global.LIGHTS_COUNT, new Vector4(lightCount, 0, 0, 0));

        context.ExecuteCommandBuffer(cmd);
        commandBufferPool.Release(cmd);

        return shadowLight;
    }

    private void ShadowPass(ScriptableRenderContext context, Light shadowLight)
    {
        CommandBuffer cmd = commandBufferPool.Get("Collect Shadow");
        cmd.GetTemporaryRT(shaderLib.Variables.Global.id_ShadowTex, shadowMapDesc, FilterMode.Bilinear);

        bool isOrtho = shadowLight.type == LightType.Directional || shadowLight.type == LightType.Area;

    }
}
