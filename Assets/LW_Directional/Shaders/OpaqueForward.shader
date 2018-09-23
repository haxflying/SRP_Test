// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/OpaqueForward"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        LOD 100

        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float3 wnormal : TEXCOORD2;
                float4 screenUV : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D  _ScreenSpaceShadowmapTexture;
            float4 _MainTex_ST;
            float4 _LightDirection;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                o.wnormal = UnityObjectToWorldNormal(v.normal);
                o.screenUV = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                i.wnormal = normalize(i.wnormal);
                i.screenUV.xy /= i.screenUV.w;
                float diffuse = saturate(dot(_LightDirection, i.wnormal));
                float attenuation = tex2D(_ScreenSpaceShadowmapTexture, i.screenUV.xy);
                fixed4 col = tex2D(_MainTex, i.uv) * diffuse * attenuation;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        Pass
        {
        	Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
            	float4 position : POSITION;
            	float3 normal : NORMAL;
            	float2 texcoord : TEXCOORD0;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 clipPos : SV_POSITION;
            };

            float4 _ShadowBias;
            float3 _LightDirection;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 GetShadowPositionHClip(appdata v)
            {
            	float3 positionWS = mul(unity_ObjectToWorld, v.position).xyz;
            	float3 normalWS = UnityObjectToWorldNormal(v.normal);

            	float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
            	float scale = invNdotL * _ShadowBias.y;

            	positionWS = normalWS * scale.xxx + positionWS;
            	float4 clipPos = mul(UNITY_MATRIX_VP, float4(positionWS, 1));

            	clipPos.z += _ShadowBias.x;

            	#if UNITY_REVERSED_Z
            	//clipPos.z = min(clipPos.z, clipPos.w * _ProjectionParams.y);
            	#else
            	//clipPos.z = max(clipPos.z, clipPos.w * _ProjectionParams.y);
            	#endif

            	return clipPos;
            }

            float4 Test(appdata v)
            {
                return UnityObjectToClipPos(v.position);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.clipPos = GetShadowPositionHClip(v);
                //o.clipPos = UnityObjectToClipPos(v.position);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return 1;
            }
            ENDCG
        }

        Pass
        {
        	Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }

}