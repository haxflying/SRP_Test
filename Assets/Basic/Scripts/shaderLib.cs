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
            public const string SHADOW_TEX = "shadowTexture";
            public const string TEMP_TEX = "_TempTex";
            public static int id_ShadowTex;
            public static int id_TempTex;
        }
    }
}
