// 立绘白色描边：在 uGUI Image 上按相邻像素的 alpha 扩张出一圈描边。
// _OutlineWidth 以「贴图像素」为单位（立绘贴图 1080×1080，显示约 245×245，
// 因此 14 texels ≈ 屏幕上 3 像素）。
Shader "WWG/UI/Portrait Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (texels)", Range(0, 48)) = 14
        _OutlineSoftness ("Outline Softness", Range(0.005, 0.5)) = 0.18
        _OutlineCutoff ("Outline Cutoff", Range(0.05, 0.95)) = 0.35

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "PortraitOutline"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _OutlineCutoff;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            static const float2 kDirs[16] =
            {
                float2(1, 0), float2(0.92388, 0.38268), float2(0.70711, 0.70711), float2(0.38268, 0.92388),
                float2(0, 1), float2(-0.38268, 0.92388), float2(-0.70711, 0.70711), float2(-0.92388, 0.38268),
                float2(-1, 0), float2(-0.92388, -0.38268), float2(-0.70711, -0.70711), float2(-0.38268, -0.92388),
                float2(0, -1), float2(0.38268, -0.92388), float2(0.70711, -0.70711), float2(0.92388, -0.38268)
            };

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // 描边：在半径 _OutlineWidth 的圆上取 16 个方向的 alpha 最大值，
                // 内外两圈叠加让粗描边中间不留空。
                float2 stepUV = _MainTex_TexelSize.xy * _OutlineWidth;
                float spread = 0.0;
                float spreadInner = 0.0;
                [unroll]
                for (int k = 0; k < 16; k++)
                {
                    float2 offset = kDirs[k] * stepUV;
                    spread = max(spread, tex2D(_MainTex, i.texcoord + offset).a);
                    spreadInner = max(spreadInner, tex2D(_MainTex, i.texcoord + offset * 0.5).a);
                }
                spread = max(spread, spreadInner);
                spread *= _OutlineColor.a * i.color.a;

                float outlineCoverage = smoothstep(_OutlineCutoff - _OutlineSoftness,
                                                   _OutlineCutoff + _OutlineSoftness, spread);

                fixed outAlpha = col.a + outlineCoverage * (1.0 - col.a);
                fixed3 outRgb = (col.rgb * col.a + _OutlineColor.rgb * outlineCoverage * (1.0 - col.a))
                                / max(outAlpha, 1e-5);

                fixed4 result = fixed4(outRgb, outAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
