Shader "Unlit/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 cUV = i.uv - 0.5;
                cUV.x *= 1.1;
                cUV *= 0.5;
                
                float dist = length(cUV);
                float d2 = dist * dist;
                float2 offset1 = float2(0, 0.05) * d2;
                float2 offset2 = float2(0.05, 0) * d2;

                fixed4 col = 0;//tex2D(_MainTex, i.uv);

                col += tex2D(_MainTex, i.uv + offset1);
                col += tex2D(_MainTex, i.uv + offset2);
                col += tex2D(_MainTex, i.uv - offset1);
                col += tex2D(_MainTex, i.uv - offset2);

                col *= 0.25;
                    
                col += d2 * 0.1;

                return col;
            }
            ENDCG
        }
    }
}
