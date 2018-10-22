Shader "Hidden/MipBlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float _MipLevel;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col0 = tex2Dlod(_MainTex, float4(i.uv, 0, 0 + _MipLevel));
				fixed4 col1 = tex2Dlod(_MainTex, float4(i.uv, 0, 1 + _MipLevel));
				fixed4 col2 = tex2Dlod(_MainTex, float4(i.uv, 0, 2 + _MipLevel));
				fixed4 col3 = tex2Dlod(_MainTex, float4(i.uv, 0, 3 + _MipLevel));
				return (col0 + col1 + col2 + col3) / 4.0;
			}
			ENDCG
		}
	}
}
