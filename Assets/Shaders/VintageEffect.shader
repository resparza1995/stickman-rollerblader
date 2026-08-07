Shader "Hidden/VintageEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VignetteIntensity ("Vignette Intensity", Range(0, 2)) = 0.8
        _VignetteSmoothness ("Vignette Smoothness", Range(0.1, 2)) = 0.7
        _SepiaAmount ("Sepia Amount", Range(0, 1)) = 0.35
        _Desaturation ("Desaturation", Range(0, 1)) = 0.2
        _GrainIntensity ("Grain Intensity", Range(0, 0.2)) = 0.04
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _VignetteIntensity;
            float _VignetteSmoothness;
            float _SepiaAmount;
            float _Desaturation;
            float _GrainIntensity;

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Desaturation
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, float3(gray, gray, gray), _Desaturation);

                // Sepia / Warm Vintage Tint
                float3 sepia = float3(
                    gray * 1.2,
                    gray * 0.9,
                    gray * 0.6
                );
                col.rgb = lerp(col.rgb, sepia, _SepiaAmount);

                // Vignette (Darkened edges)
                float2 uvOffset = i.uv - float2(0.5, 0.5);
                float dist = length(uvOffset);
                float vignette = smoothstep(0.8, 0.8 - _VignetteSmoothness, dist * _VignetteIntensity);
                col.rgb *= vignette;

                // Film Grain Noise
                float noise = (rand(i.uv + _Time.yy) - 0.5) * _GrainIntensity;
                col.rgb += noise;

                return col;
            }
            ENDCG
        }
    }
}
