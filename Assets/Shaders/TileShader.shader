﻿Shader "Unlit/TileShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Saturation("Saturation", Float) = 1
		_Luminance("Luminance", Float) = 1
		_Alpha("Alpha", Float) = 1

		_IsUnderworld("IsUnderworld", Float) = 0
		_ShowingUnderworld("ShowingUnderworld", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha


        Pass
        {
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float _Saturation;
			float _Luminance;


			float _IsUnderworld;
			float _ShowingUnderworld;
			float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

			float3 HUEtoRGB(in float H)
			{
				float R = abs(H * 6 - 3) - 1;
				float G = 2 - abs(H * 6 - 2);
				float B = 2 - abs(H * 6 - 4);
				return saturate(float3(R, G, B));
			}

			float Epsilon = 1e-10;

			float3 RGBtoHCV(in float3 RGB)
			{
				// Based on work by Sam Hocevar and Emil Persson
				float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0 / 3.0) : float4(RGB.gb, 0.0, -1.0 / 3.0);
				float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
				float C = Q.x - min(Q.w, Q.y);
				float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
				return float3(H, C, Q.x);
			}

			float3 HSVtoRGB(in float3 HSV)
			{
				float3 RGB = HUEtoRGB(HSV.x);
				return ((RGB - 1) * HSV.y + 1) * HSV.z;
			}

			float3 RGBtoHSV(in float3 RGB)
			{
				float3 HCV = RGBtoHCV(RGB);
				float S = HCV.y / (HCV.z + Epsilon);
				return float3(HCV.x, S, HCV.z);
			}


            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

				if (_Saturation != 1.0 || _Luminance != 1.0) {
					//float grayscale = (col.r + col.g + col.b) / 3.0;
					//col.rgb = lerp(float3(grayscale, grayscale, grayscale), col.rgb, _Saturation);
					
					float3 hsv = RGBtoHSV(col.rgb);
					hsv.z *= _Luminance;
					hsv.y *= _Saturation;
					col.rgb = min(float3(1,1,1), max(float3(0,0,0), HSVtoRGB(hsv)));
				}

				col.a *= (1.0 - _ShowingUnderworld * _IsUnderworld);
				col.a *= _Alpha;
                return col;
            }
            ENDCG
        }
    }
}
