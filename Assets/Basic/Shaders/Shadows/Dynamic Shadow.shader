Shader "Hidden/Dynamic Shadow"
{
	Properties
	{
		
	}
	SubShader
	{
		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#pragma multi_compile __ SHADOW_PROJ_ORTHO
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 depth : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				#if defined(SHADOW_PROJ_ORTHO)
				o.depth = o.vertex.z / o.vertex.w * 0.5 + 0.5;
				#else
				o.depth = o.vertex.w;
				#endif
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				const float depthScale = 1.0 / 32.0;
				half d = i.depth * depthScale;
				return half4(d, 1.0, 1.0, 1.0); 
			}
			ENDCG
		}
	}
}
