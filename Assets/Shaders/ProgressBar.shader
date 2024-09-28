Shader "ProgressBar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _scale ("scale Vector", Vector) = (0.5, 1, 1, 1)
    }
    SubShader
    {
        // No culling or depth
        //Cull Off ZWrite Off ZTest Always
        Tags {"Queue"="Transparent" "IgnoreProjector"="True"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float3 _scale;

            fixed4 frag (v2f i) : SV_Target
            {

                float op = 1;
                op *= frac((i.worldPos.y * _scale.y) - i.worldPos.x * _scale.x);
                op = step(0.5, op);
                return float4(0, op, 0, 1);
            }
            ENDCG
        }
    }
}
