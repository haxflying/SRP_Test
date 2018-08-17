using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class shaderLib {

	public static class Shaders
    {
        public static Shader SafeFind(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if(shader == null)
                Debug.LogWarningFormat("Couldn't locate Shader. '{0}'.");
            return shader;
        }

        const string DYNAMIC_SHADOW = "Hidden/Dynamic Shadow";
        public static Shader DynamicShadow { get { return SafeFind(DYNAMIC_SHADOW); } }
        public const string BASIC = "";
    }

    public static class Variables
    {
        public static class Global
        {
            //Light
            public const string LIGHTS_COLOR = "globalLightColors";
            public const string LIGHTS_POSITION = "globalLightPositions";
            public const string LIGHTS_ATTEN = "globalLightAtten";
            public const string LIGHTS_COUNT = "globalLightCount";
            public const string LIGHTS_SPOT_DIRS = "globalSpotDirections";

            //shadow
            public const string SHADOW_INTENSITY = "shadowIntensity";
            public const string SHADOW_BIAS = "shadowBiases";
            public const string SHADOW_DISTANCE = "shadowDistances";
            public const string SHADOW_COUNT = "shadowCount";
            public const string SHADOW_MATRICES = "shadowMatrices";

            public const string SHADOW_TEX = "shadowTexture";
            public const string TEMP_TEX = "_TempTex";
            public static int id_ShadowTex;
            public static int id_TempTex;
        }

        public static class Renderer
        {
            public const string SHADOW_INDEX = "shadowIndex";
        }
    }

    public static class Keywords
    {
        public const string SHADOW_PROJ_ORTHO = "SHADOW_PROJ_ORTHO";
    }

    public static class Passes
    {
        public const int SHADOW_PASS_ID = 0;
    }

    
}
