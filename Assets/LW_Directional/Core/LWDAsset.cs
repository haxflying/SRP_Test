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
        public SoftShadowType softShadowType;

        [Header("Shaders")]
        public Shader blitShader;
        public Shader screenSpaceShadowShader;
        public Shader blurShader;

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/LWD Asset")]
        static void CreateLWDpipeline()
        {
            var instance = ScriptableObject.CreateInstance<LWDAsset>();
            UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/pipeline_assets/LWD.asset");
        }
#endif
        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new LWDpipeline(this);
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
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    public enum SoftShadowType
    {
        VSM, PCSS
    }

    public static class KeywordStrings
    {
        public static string SoftShadows = "_SOFTSHADOW";
        public static string CascadeShadows = "_CASCADED";
        public static string VSM = "_VSM";
        public static string PCSS = "_PCSS";
    }
}
