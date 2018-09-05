Shader "SRP/basic"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#pragma multi_compile __ SHADOW_PROJ_ORTHO
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
		float3 wnormal : TEXCOORD2;
		half4 shadowCoords[2] : TEXCOORD3;
		half4 shadowDepths : TEXCOORD5;
		float4 vertex : SV_POSITION;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	CBUFFER_START(ShadowData)
	float4x4 shadowMatrices[4];
	sampler2D shadowTexture;
	CBUFFER_END

	half computeShadow(half2 shadowUV, sampler2D shadowSampler, half vertDepth, half bias, half mask, half shadowDistance, half falloff)
	{
		#define depthScale 32.0
		half2 depthTex;
		half depth;

		depthTex = tex2D(shadowSampler, shadowUV).rg;
		depth = depthTex.r * depthScale + bias;
		half depthDelta = depth - vertDepth;
		half fade = saturate(1.0 + depthDelta * falloff);
		half depthDeltaScaled = saturate(16.0 * depthDelta);

		half atten = max(0.0, vertDepth * shadowDistance);
		half shadow = 1.0 - depthTex.g + depthDeltaScaled * depthTex.g;
		shadow = saturate(shadow + mask + atten);
		return shadow * fade + 1.0 - fade;

		#undef depthScale
	}
	
	v2f vert (appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		o.wnormal = UnityObjectToWorldNormal(v.normal);
		float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
		float4 shadowCoord = mul(shadowMatrices[0], worldPos);
		o.shadowCoords[0].xy = shadowCoord.xy;
		#if defined(SHADOW_PROJ_ORTHO)
		o.shadowDepths[0] = (shadowCoord.z / shadowCoord.w * 0.5 + 0.5);
		#else
		o.shadowDepths[0] = shadowCoord.w;
		#endif

		shadowCoord = mul(shadowMatrices[1], worldPos);
		o.shadowCoords[0].zw = shadowCoord.xy;
		#if defined(SHADOW_PROJ_ORTHO)
		o.shadowDepths[1] = (shadowCoord.z / shadowCoord.w * 0.5 + 0.5);
		#else
		o.shadowDepths[1] = shadowCoord.w;
		#endif

		shadowCoord = mul(shadowMatrices[2], worldPos);
		o.shadowCoords[1].xy = shadowCoord.xy;
		#if defined(SHADOW_PROJ_ORTHO)
		o.shadowDepths[2] = (shadowCoord.z / shadowCoord.w * 0.5 + 0.5);
		#else
		o.shadowDepths[2] = shadowCoord.w;
		#endif

		shadowCoord = mul(shadowMatrices[3], worldPos);
		o.shadowCoords[1].zw = shadowCoord.xy;
		#if defined(SHADOW_PROJ_ORTHO)
		o.shadowDepths[3] = (shadowCoord.z / shadowCoord.w * 0.5 + 0.5);
		#else
		o.shadowDepths[3] = shadowCoord.w;
		#endif
		return o;
	}
	
	fixed4 frag (v2f i) : SV_Target
	{
		i.wnormal = normalize(i.wnormal);
		fixed4 col = tex2D(_MainTex, i.uv);
		float diffuse = saturate(dot(_WorldSpaceLightPos0, i.wnormal));
		col.rgb *= diffuse;

		half shadow = 1;
		half2 coord;

		coord = i.shadowCoords[0].xy / i.shadowDepths[0];
		coord = coord * 0.5 + 0.5;		
		shadow *= computeShadow(coord, shadowTexture, i.shadowDepths[0], 0, 0, 0, 1);

		coord = i.shadowCoords[0].zw / i.shadowDepths[1];
		coord = coord * 0.5 + 0.5;		
		shadow *= computeShadow(coord, shadowTexture, i.shadowDepths[1], 0, 0, 0, 1);

		coord = i.shadowCoords[1].xy / i.shadowDepths[2];
		coord = coord * 0.5 + 0.5;		
		shadow *= computeShadow(coord, shadowTexture, i.shadowDepths[2], 0, 0, 0, 1);

		coord = i.shadowCoords[1].zw / i.shadowDepths[3];
		coord = coord * 0.5 + 0.5;		
		shadow *= computeShadow(coord, shadowTexture, i.shadowDepths[3], 0, 0, 0, 1);
		return col * shadow;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{"LightMode" = "basic"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			ENDCG
		}
	}
	FallBack "Diffuse"
}
