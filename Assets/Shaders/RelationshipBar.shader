Shader "Unlit/RelationshipBar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _col1 ("color1", Color) = (1, 0, 0, 1)
        _col2 ("color2", Color) = (0, 1, 0, 1)
        maxWidth ("maxWidth", Float) = 10
        minWidth ("minWidth", Float) = 1
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True"} 
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 vertex : SV_POSITION;
   
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float maxWidth;
            float minWidth;
            float4 _col1;
            float4 _col2;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = (i.uv - 0.5) * 2;

                float bltr = step(-0.1, uv.y - uv.x);
                float tlbr = step(-0.1, -uv.y - uv.x);

                //float right = step(-0.1, -uv.y + uv.x);
                //float right2 = step(-0.1, uv.y + uv.x);

                bltr = step(uv.x + 1, uv.y * 2 + 2);
                tlbr = step(uv.x - 1, -uv.y * 2);

                float alpha = tlbr * bltr;

                //alpha += right * right2;

                float4 col = _col2;//step(0, uv.x) * _col1 + (1 - step(0, uv.x)) * _col2;
                col.a *= 0.6;
                //col *= 0.3;
                float shapeterm = uv.x;

                col *= 0.1 + step(frac(_Time.y + -shapeterm * 5 ), 0.2);
                col.a *= alpha;
                return float4(col);
            }
            ENDCG
        }
    }
}
