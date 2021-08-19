Shader "Unlit/UnitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Waterline("Waterline", Float) = 0

		[HDR] _Color("Color", Color) = (0,0,0,0)

		_ColorMult("ColorMult", Color) = (1,1,1,1)

		_hueshift("hueshift", Float) = 0
		_lummult("lummult", Float) = 1



		_Alpha("Alpha", Float) = 1
		_TeamColorHue("TeamColorHue", Float) = 0
		_Poisoned("Poisoned", Float) = 0
    }
    SubShader
    {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 100

		Cull Off
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
			float _Alpha;
			float _Waterline;
			float _TeamColorHue;
			float _Poisoned;
			float4 _Color;
			float4 _ColorMult;
			float _hueshift;
			float _lummult;


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
				return clamp(((RGB - 1) * HSV.y + 1) * HSV.z, float3(0,0,0), float3(1,1,1));
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

				//detect if team color.
				if (col.a > 0.0 && col.r > 0.23 && col.r > col.b && col.b > col.g && (col.g <= 0.0 || col.r > 0.9)) {
					float3 hsv = RGBtoHSV(col.rgb);
					hsv.r = _TeamColorHue;
					col.rgb = HSVtoRGB(hsv);
				}
				else {
					float3 hsv = RGBtoHSV(col.rgb);
					hsv.r += _hueshift;
					hsv.b *= _lummult;
					col.rgb = HSVtoRGB(hsv);
				}

				if (_Poisoned > 0.0) {
					col.rb *= 1.0 - _Poisoned*0.5;
				}

				if (i.uv.y < _Waterline) {
					float below = _Waterline - i.uv.y;
					col.a *= max(0.0, 0.4f - below * 2.0);
				}

				if (_Color.a > 0.0) {
					col.rgb = lerp(col.rgb, _Color.rgb, _Color.a);
				}

				col.a *= _Alpha;
				col = col * _ColorMult;

                return col;
            }
            ENDCG
        }
    }
}
