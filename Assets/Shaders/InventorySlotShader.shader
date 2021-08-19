Shader "Unlit/InventorySlotShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		[HDR] _HighlightColor("HighlightColor", Color) = (1,1,1,1)
		_Alpha("Alpha", Float) = 1
		_r1("r1", Float) = 1
		_r2("r2", Float) = 1
		_r3("r3", Float) = 1
		_r4("r4", Float) = 1
		_slant("_slant", Float) = 1
		_highlight("_highlight", Float) = 0
		_hueshift("_hueshift", Float) = 0

			// required for UI.Mask
_StencilComp("Stencil Comparison", Float) = 8
_Stencil("Stencil ID", Float) = 0
_StencilOp("Stencil Operation", Float) = 0
_StencilWriteMask("Stencil Write Mask", Float) = 255
_StencilReadMask("Stencil Read Mask", Float) = 255
_ColorMask("Color Mask", Float) = 15


    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

			// required for UI.Mask
Stencil
{
	Ref[_Stencil]
	Comp[_StencilComp]
	Pass[_StencilOp]
	ReadMask[_StencilReadMask]
	WriteMask[_StencilWriteMask]
}
 ColorMask[_ColorMask]

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
				float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _HighlightColor;

			float _Alpha;
			float _r1;
			float _r2;
			float _r3;
			float _r4;
			float _slant;
			float _highlight;
			float _hueshift;

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


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				col.a *= _Alpha;

				if (_hueshift != 0.0) {
					float3 hsv = RGBtoHSV(col.rgb);
					hsv[0] += _hueshift;
					col.rgb = HSVtoRGB(hsv);
				}

				if (col.r + col.g + col.b < 0.02) {
					fixed r = max(0.0, sin(_Time*_r1 + i.uv.x*_r2 + i.uv.y*_r2*_slant)-_r3)*_r4;
					col.rgb = lerp(col.rgb, _HighlightColor.rgb, r);
				}

				col.rgb += fixed3(_highlight,_highlight,_highlight);

				col = col * i.color;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
