﻿Shader "Unlit/ScreenSpaceShadow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZWrite Off ZTest Always Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _CASCADED
			#pragma multi_compile _ _SOFTSHADOW

			#define MAX_SHADOW_CASCADES 4
			#include "UnityCG.cginc"

			sampler2D_float _CameraDepthTexture;
			UNITY_DECLARE_SHADOWMAP(_DirectionalShadowmapTexture);
			float4 _DirectionalShadowmapTexture_TexelSize;

			CBUFFER_START(_DirectionShadowBuffer)
				float4x4 _WorldToShadow[MAX_SHADOW_CASCADES + 1];
				float4 _DirShadowSplitSpheres0;
				float4 _DirShadowSplitSpheres1;
				float4 _DirShadowSplitSpheres2;
				float4 _DirShadowSplitSpheres3;
				float4 _DirShadowSplitSphereRadii;
				half4 _ShadowOffset0;
				half4 _ShadowOffset1;
				half4 _ShadowOffset2;
				half4 _ShadowOffset3;
				half4 _ShadowData; //x.shadowStength
				float4 _ShadowmapSize; //xy: 1/res zw : res
			CBUFFER_END


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.vertex = UnityObjectToClipPos(v.vertex);
				float4 projPos = o.vertex * 0.5;
				projPos.xy = projPos.xy + projPos.w;
				o.uv.xy = v.uv;
				o.uv.zw = projPos.xy;
				return o;
			}

			inline float3 computeCameraSpacePos(v2f i)
			{
				float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

				#if UNITY_REVERSED_Z
				deviceDepth = 1 - deviceDepth;
				#endif
				float4 clipPos = float4(i.uv.zw, deviceDepth, 1.0);
				clipPos.xyz = 2 * clipPos.xyz - 1;
				float4 camPos = mul(unity_CameraInvProjection, clipPos);
				camPos.xyz /= camPos.w;
				camPos.z *= -1;
				return camPos.xyz;
			}

			half computeCascadeIndex(float3 positionWS)
			{
				float3 fromCenter0 = positionWS - _DirShadowSplitSpheres0.xyz;
				float3 fromCenter1 = positionWS - _DirShadowSplitSpheres1.xyz;
				float3 fromCenter2 = positionWS - _DirShadowSplitSpheres2.xyz;
				float3 fromCenter3 = positionWS - _DirShadowSplitSpheres3.xyz;
				float4 distance2 = float4(dot(fromCenter0, fromCenter0),
					dot(fromCenter1, fromCenter1),
					dot(fromCenter2, fromCenter2),
					dot(fromCenter3, fromCenter3));
				half4 weights = half4(distance2 < _DirShadowSplitSphereRadii);
				weights.yzw = saturate(weights.yzw - weights.xyz);
				return 4 - dot(weights, half4(4,3,2,1));
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 vpos = computeCameraSpacePos(i);
				float3 wpos = mul(unity_CameraToWorld, float4(vpos, 1)).xyz;

				half cascadeIndex = computeCascadeIndex(wpos);
				float4 coords = mul(_WorldToShadow[cascadeIndex], float4(wpos, 1.0));

				fixed shadow = UNITY_SAMPLE_SHADOW(_DirectionalShadowmapTexture, coords);

				return shadow;
			}
			ENDCG
		}
	}
}
