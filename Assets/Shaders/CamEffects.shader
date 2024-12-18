Shader "Unlit/CamEffects"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _uiTex ("Texture", 2D) = "black" {}
        _texScale1 ("texScale1", Float) = 400
        _texScale2 ("texScale2", Float) = 100
        _dist ("dist", Range(-1, 1)) = 0
        _scale ("scale", Range(-1, 1)) = 0
        _t ("time", Float) = 0
        _noiseAmt ("noise power", Float) = 0
        _fuzzAmt ("fuzz power", Float) = 1
        _sls("scanline strength", Float) = 1
        _lines("numscanlines", Integer) = 90
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

            sampler2D _uiTex;
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
    
            float _dist;
            float _scale;
            float _t;
            float _noiseAmt;
            float _fuzzAmt;
            float _sls;
            int _lines;
            float _texScale1;
            float _texScale2;         
            float3 sample(float2 uv){
                return tex2D(_MainTex, uv).rgb;
            }

            float3 box_sample(float2 uv){

                float2 ts = _MainTex_TexelSize.xy;
                float2 of1 = float2(-ts.x, ts.y);
                float2 of2 = float2(0, ts.y);
                float2 of3 = float2(ts.x, ts.y);

                float2 of4 = float2(-ts.x, 0);
                float2 of5 = float2(0, 0);
                float2 of6 = float2(ts.x, 0);

                float2 of7 = float2(-ts.x, -ts.y);
                float2 of8 = float2(0, -ts.y);
                float2 of9 = float2(ts.x, -ts.y);

                float3 outp = 0;
                outp += sample(uv + of1);
                outp += sample(uv + of2);
                outp += sample(uv + of3);
                outp += sample(uv + of4);
                outp += sample(uv + of5);
                outp += sample(uv + of6);
                outp += sample(uv + of7);
                outp += sample(uv + of8);
                outp += sample(uv + of9);

                return (outp * 0.11111);
            }


            float rand (float2 uv) { 
                return frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            float noise (float2 uv) {
                float2 ipos = floor(uv);
                float2 fpos = frac(uv); 
                
                float o  = rand(ipos);
                float x  = rand(ipos + float2(1, 0));
                float y  = rand(ipos + float2(0, 1));
                float xy = rand(ipos + float2(1, 1));

                float2 smooth = smoothstep(0, 1, fpos);
                return lerp( lerp(o,  x, smooth.x), 
                                lerp(y, xy, smooth.x), smooth.y);
            }


            float fractal_noise (float2 uv) {
                float n = 0;
                // fractal noise is created by adding together "octaves" of a noise
                // an octave is another noise value that is half the amplitude and double the frequency of the previously added noise
                // below the uv is multiplied by a value double the previous. multiplying the uv changes the "frequency" or scale of the noise becuase it scales the underlying grid that is used to create the value noise
                // the noise result from each line is multiplied by a value half of the previous value to change the "amplitude" or intensity or just how much that noise contributes to the overall resulting fractal noise.

                n  = (1 / 2.0)  * noise( uv * 1);
                n += (1 / 4.0)  * noise( uv * 2); 
                n += (1 / 8.0)  * noise( uv * 4); 
                n += (1 / 16.0) * noise( uv * 8);
                
                return n;
            }
            float2 scale(float2 uv, float scale)
            {
                return round(uv * scale)/scale;
            }

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

                float2 uv = i.uv;
                float2 luv = uv - 0.5;
                float distort = 1 + length(luv) * _dist;
                uv = luv * distort * _scale;
                uv += 0.5;

                float2 sUV = scale(uv, _texScale1);
                float3 col = 0; 
                //float2 uisUV = scale(i.uv, _texScale2);
                //col += tex2D(_uiTex, uisUV);
                //col += 0.1 * float3(0.7, 0.5, 0.1);

                //pow(fractal_noise(scale(i.uv.yy, 200) * 40 + scale(_t * 2, 5)), 4) * 0.2;
                float crtnoise = frac(uv.y * 20 + frac(_t * 0.2)) * 0.05;
                crtnoise += frac(uv.x * 200) * 0.01;
                //col += crtnoise;
                //col += box_sample(sUV + float2(0, crtnoise) * 0.2) * 2;

                col += sample(sUV + float2(0, crtnoise) * 0.2 * _noiseAmt) * 2;
                col = min(0.9, col);

                float2 nUV = rand(scale(sUV * 100 + frac(-_t), 10));
                float lensnoise = 1 * pow(noise(nUV), 0.7) * length(luv);
                lensnoise *= 0.12 * (1 -  2 * crtnoise);
		        lensnoise *= 1 - step(0.2, col); //cut the noise to preserve color
                col += lensnoise * _fuzzAmt * 1;

                int pixMask = 0;
                //pixMask += 1 - step(frac(i.uv.x * 100), 0.9);
                //pixMask += 1 - step(frac(i.uv.y * 90), 0.6);
                //pixMask = min(1, pixMask);
                //col = max(0.06 * (1 - length(nUV - 0.5)), col);
                //col *= 1 * (1 - pixMask) + 0.8 * (pixMask); 

                float ny = frac(lerp(i.uv.y, nUV.y, 0.0015) * _lines);
                float sld = pow(2 * abs(round(ny) - ny), 4);
                col *= lerp(1, max(min(1, sld * 5), 0.6), _sls);

      
                //col = pow(col, 1.3);
                return float4(col, 1);
            }
            ENDCG
        }
    }
}
