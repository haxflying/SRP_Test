using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class pipeline_basic_asset : RenderPipelineAsset {

    public Color clearColor = Color.green;

    public const int MAX_SHADOWCASTERS = 4;

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
        return new pipeline_basic_instance(clearColor);
    }

    public class pipeline_basic_instance : RenderPipeline
    {
        private Color m_ClearColor = Color.black;

        public pipeline_basic_instance(Color clearColor)
        {
            m_ClearColor = clearColor;
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
    }
}
