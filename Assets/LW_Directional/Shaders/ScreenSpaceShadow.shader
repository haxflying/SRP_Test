Shader "Unlit/ScreenSpaceShadow"
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
			#pragma multi_compile _ _VSM
			#pragma multi_compile _ _PCSS

			#define MAX_SHADOW_CASCADES 4
			#include "UnityCG.cginc"

			sampler2D_float _CameraDepthTexture;
			//UNITY_DECLARE_SHADOWMAP(_DirectionalShadowmapTexture);
			sampler2D_float _DirectionalShadowmapTexture;
			sampler2D_float _BluredDirectionalShadowmapTexture;
			float4 _DirectionalShadowmapTexture_TexelSize;		

			float _MipLevel;	

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
				float deviceDepth = tex2D(_CameraDepthTexture, i.uv.xy);
				
				#if UNITY_REVERSED_Z
				deviceDepth = 1 - deviceDepth;
				#endif

				float4 clipPos = float4(i.uv.zw, deviceDepth, 1.0);
				clipPos.xyz = 2 * clipPos.xyz - 1;
				#if UNITY_UV_STARTS_AT_TOP
				clipPos.y = -clipPos.y;
				#endif
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

				#if _SOFTSHADOW 
				
				float2 moments = tex2D(_BluredDirectionalShadowmapTexture, coords.xy).rg;
				//return float4(moments, 0, 0);
				if(coords.z >= moments.x)
					return 1;

				float variance = moments.y - (moments.x * moments.x);
				variance = max(variance, 0.00002);

				float d_minus_mean = coords.z - moments.x;
				float p_max = variance / (variance + d_minus_mean * d_minus_mean);
				return 1 - (1 - p_max) * _ShadowData.x;
				//fixed shadow = (tex2D(_DirectionalShadowmapTexture, coords.xy));
				//return coords.z * 0.5 + 0.5;
				//return shadow;
				//fixed res = (shadow) > coords.z ? 0 : 1;
				//return lerp(1, res, _ShadowData.x);
				#else
				float shadow = tex2D(_DirectionalShadowmapTexture, coords.xy).r;//UNITY_SAMPLE_SHADOW(_DirectionalShadowmapTexture, coords);
				fixed res = (shadow) > coords.z ? 0 : 1;
				return lerp(1, res, _ShadowData.x);
				#endif
			}
			ENDCG
		}
	}
}
