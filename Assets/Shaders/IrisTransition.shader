Shader "UI/IrisTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OverlayColor ("Overlay Color", Color) = (0, 0, 0, 0.85)
        _Progress ("Expansion Progress", Range(0, 1.5)) = 0.0
        _Smoothness ("Edge Smoothness", Range(0.001, 0.2)) = 0.04
        _AspectRatio ("Aspect Ratio (X/Y)", Float) = 1.77778

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _OverlayColor;
            float _Progress;
            float _Smoothness;
            float _AspectRatio;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 centeredUV = IN.texcoord - float2(0.5, 0.5);
                centeredUV.x *= _AspectRatio;
                float dist = length(centeredUV);

                // Max radius needed to clear full screen corners (approx 0.85 * aspect)
                float maxRadius = 1.1;
                float currentRadius = _Progress * maxRadius;

                // Smooth iris cutout: 1 outside circle (dark overlay), 0 inside circle (clear map)
                float mask = smoothstep(currentRadius - _Smoothness, currentRadius + _Smoothness, dist);

                fixed4 finalColor = _OverlayColor;
                finalColor.a *= mask * IN.color.a;

                return finalColor;
            }
            ENDCG
        }
    }
}
