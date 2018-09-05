using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MZ.LWD
{
    public class LWDAsset : RenderPipelineAsset
    {
        public bool SupportDynamicBatching = true;
        public ShadowResolution ShadowAltasResolution = ShadowResolution._2048;
        public float ShadowDistance = 50.0f;
        public ShadowCascades ShadowCascades = ShadowCascades.FOUR_CASCADES;
        public bool useSoftShadow = false;

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/LWD Asset")]
        static void CreateLWDpipeline()
        {
            var instance = ScriptableObject.CreateInstance<pipeline_basic_asset>();
            UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/pipeline_assets/LWD.asset");
        }
#endif
        protected override IRenderPipeline InternalCreatePipeline()
        {
            throw new NotImplementedException();
        }
    }

    public enum Downsampling
    {
        None = 0,
        _2xBilinear,
        _4xBox,
        _4xBilinear,
    }

    public enum ShadowCascades
    {
        NO_CASCADES = 0,
        TWO_CASCADES,
        FOUR_CASCADES,
    }

    public enum ShadowResolution
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }
}
