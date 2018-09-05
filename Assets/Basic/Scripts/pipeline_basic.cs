using System.Collections;
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
    RenderTextureDescriptor shadowMapDesc;
    RenderTexture shadowRT;
    RenderTargetIdentifier shadowRTID;
    RenderTargetIdentifier tempRTID;
    Material shadowMaterial;

    readonly LightComparer lightComparer;

    public pipeline_basic_instance(pipeline_basic_asset asset) : base()
    {
        pipelineAsset = asset;
        m_ClearColor = asset.clearColor;
        lightComparer = new LightComparer();

        shadowMapDesc = new RenderTextureDescriptor(asset.shadowMapSize, asset.shadowMapSize, RenderTextureFormat.RGHalf, 24)
        {
            dimension = TextureDimension.Tex2D,
            volumeDepth = 1,
            msaaSamples = 1
        };

        shadowRT = new RenderTexture(shadowMapDesc) { name = "Shadow Depth Tex" };
        shaderLib.Variables.Global.id_ShadowTex = Shader.PropertyToID(shaderLib.Variables.Global.SHADOW_TEX);
        shaderLib.Variables.Global.id_TempTex = Shader.PropertyToID(shaderLib.Variables.Global.TEMP_TEX);

        shadowRTID = new RenderTargetIdentifier(shaderLib.Variables.Global.id_ShadowTex);
        tempRTID = new RenderTargetIdentifier(shaderLib.Variables.Global.id_TempTex);

        shadowMaterial = new Material(shaderLib.Shaders.DynamicShadow);
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

            #region light&shadow

            List<VisibleLight> visibleLights = cull.visibleLights;
            Light shadowLight = SetupLightBuffers(renderContext, visibleLights, camera.worldToCameraMatrix);

            shadowMapDesc.width = pipelineAsset.shadowMapSize;
            shadowMapDesc.height = pipelineAsset.shadowMapSize;

            if (shadowLight != null)
                ShadowPass(renderContext, shadowLight);

            renderContext.SetupCameraProperties(camera);
            #endregion

            var cmd = commandBufferPool.Get("Clear");
            cmd.ClearRenderTarget(true, false, m_ClearColor);
            renderContext.ExecuteCommandBuffer(cmd);

            commandBufferPool.Release(cmd);


            #region opaque
            var settings = new DrawRendererSettings(camera, new ShaderPassName(shaderLib.Passes.BASE));
            settings.sorting.flags = SortFlags.CommonOpaque;

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };
            renderContext.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

            var shadowSettings = new DrawShadowsSettings(cull, 0);
            //renderContext.DrawShadows(ref shadowSettings);

            #endregion

            renderContext.DrawSkybox(camera);

            #region transparent

            settings.sorting.flags = SortFlags.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            renderContext.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

            #endregion
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
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.GetTemporaryRT(shaderLib.Variables.Global.id_ShadowTex, shadowMapDesc, FilterMode.Bilinear);

        bool isOrtho = shadowLight.type == LightType.Directional || shadowLight.type == LightType.Area;
        cmd.SetRenderTarget(shadowRTID);
        cmd.SetViewport(new Rect(0, 0, shadowMapDesc.width, shadowMapDesc.height));
        cmd.ClearRenderTarget(true, true, Color.clear, 1);

        if (isOrtho)
            cmd.EnableShaderKeyword(shaderLib.Keywords.SHADOW_PROJ_ORTHO);
        else
            cmd.DisableShaderKeyword(shaderLib.Keywords.SHADOW_PROJ_ORTHO);

        float[] shadowDistances = new float[pipeline_basic_asset.MAX_SHADOWCASTERS];
        float[] shadowBiases = new float[pipeline_basic_asset.MAX_SHADOWCASTERS];

        Vector2 shadowQuadrant = new Vector2(shadowMapDesc.width / 2f, shadowMapDesc.height / 2f);

        Rect[] pixelRects = new Rect[pipeline_basic_asset.MAX_SHADOWCASTERS]
        {
            new Rect(Vector2.zero, shadowQuadrant),
            new Rect(new Vector2(0, shadowQuadrant.y), shadowQuadrant),
            new Rect(new Vector2(shadowQuadrant.x, 0), shadowQuadrant),
            new Rect(shadowQuadrant, shadowQuadrant)
        };

        Matrix4x4[] shadowMatrices = new Matrix4x4[pipeline_basic_asset.MAX_SHADOWCASTERS];
        //Debug.Log(shadowCaster.casters.Count);
        for (int i = 0; i < shadowCaster.casters.Count; i++)
        {
            Matrix4x4 view, proj;
            float distance;
            shadowCaster.casters[i].SetupShadowMatrices(i, shadowLight, out view, out proj, out distance);
            cmd.SetViewProjectionMatrices(view, proj);
            cmd.SetViewport(pixelRects[i]);
            cmd.DrawRenderer(shadowCaster.casters[i].renderer, shadowMaterial, 0, shaderLib.Passes.SHADOW_PASS_ID);
            shadowMatrices[i] = proj * view;
            shadowDistances[i] = isOrtho ? 0 : distance;
            shadowBiases[i] = shadowLight.shadowBias;
        }

        cmd.SetGlobalFloat(shaderLib.Variables.Global.SHADOW_INTENSITY, shadowLight.shadowStrength);
        cmd.SetGlobalVector(shaderLib.Variables.Global.SHADOW_BIAS, new Vector4(shadowBiases[0], shadowBiases[1], shadowBiases[2], shadowBiases[3]));
        cmd.SetGlobalVector(shaderLib.Variables.Global.SHADOW_DISTANCE, new Vector4(shadowDistances[0], shadowDistances[1], shadowDistances[2], shadowDistances[3]));
        cmd.SetGlobalFloat(shaderLib.Variables.Global.SHADOW_COUNT, shadowCaster.casters.Count);
        cmd.SetGlobalMatrixArray(shaderLib.Variables.Global.SHADOW_MATRICES, shadowMatrices);

        context.ExecuteCommandBuffer(cmd);
        commandBufferPool.Release(cmd);
    }
}
