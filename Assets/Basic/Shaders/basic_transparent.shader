Shader "SRP/basic_transparent"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	_Color("Color", Color) = (1,1,1,1)
	}
		SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent"}
		LOD 100

		Pass
	{
		Tags{ "LightMode" = "basic" }
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float3 normal : normal;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		UNITY_FOG_COORDS(1)
			float3 wnormal : TEXCOORD2;
		float4 vertex : SV_POSITION;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	fixed4 _Color;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		UNITY_TRANSFER_FOG(o,o.vertex);
		o.wnormal = UnityObjectToWorldNormal(v.normal);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		i.wnormal = normalize(i.wnormal);
	fixed4 col = tex2D(_MainTex, i.uv);
	float diffuse = saturate(dot(_WorldSpaceLightPos0, i.wnormal));
	col.rgb *= diffuse;
	UNITY_APPLY_FOG(i.fogCoord, col);
	return col * _Color;
	}
		ENDCG
	}
	}
		FallBack "Diffuse"
}
