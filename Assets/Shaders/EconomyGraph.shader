Shader "Unlit/EconomyGraph"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _time ("time", Float) = 0
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

            int popLen;
            float popOverTime[25];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                
                float2 uv = i.uv;

                //uv.x += frac(_Time.y) / (popLen - 1);// - floor(_Time.x);
                int index = floor(uv.x * (popLen - 1));
                float brite = (index) / (float)popLen;
                brite = pow(pow(brite * 1.5, 0.8) * 0.4, 2);
                int nextIndex = min(index + 1, (popLen - 1));

                float diff =  popOverTime[nextIndex] - popOverTime[index]; 
                float height = lerp(popOverTime[index], popOverTime[nextIndex], frac(uv.x * (popLen - 1)));

                //height step
                brite *= step(uv.y, height) + (1 - step(uv.y, height)) * 0.2;

                //top line
                brite += step(abs(uv.y - height), 0.01); 
                //brite = 0.5;
                //brightness mound
                brite *= pow(1 - abs((uv.y) - height), 3);

                //col.x = popLen;
                float3 col = float3(1, 1, 1);
                col *= step(0, diff) * float3(0.3, 1, 0.3) + (1 - step(0, diff)) * float3(1, 0.3, 0.3);
                //col = frac(_Time.y) * 0.1;//(_Time.y - floor(_Time.y)) * 0.1f;
                return fixed4(col * brite, 1);
            }
            ENDCG
        }
    }
}
