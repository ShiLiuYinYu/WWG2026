Shader "WWG/UI/Alpha Edge Highlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (1, 0.92, 0.05, 1)
        _Thickness ("Edge Width (UV)", Range(0.001, 0.05)) = 0.02
        _AlphaThreshold ("Ignore Alpha Below", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _EdgeColor;
            float _Thickness;
            float _AlphaThreshold;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 offsetX = float2(_Thickness, 0);
                float2 offsetY = float2(0, _Thickness);
                float center = tex2D(_MainTex, input.uv).a;
                float neighbor = tex2D(_MainTex, input.uv + offsetX).a;
                neighbor = min(neighbor, tex2D(_MainTex, input.uv - offsetX).a);
                neighbor = min(neighbor, tex2D(_MainTex, input.uv + offsetY).a);
                neighbor = min(neighbor, tex2D(_MainTex, input.uv - offsetY).a);
                neighbor = min(neighbor, tex2D(_MainTex, input.uv + offsetX + offsetY).a);
                neighbor = min(neighbor, tex2D(_MainTex, input.uv + offsetX - offsetY).a);
                neighbor = min(neighbor, tex2D(_MainTex, input.uv - offsetX + offsetY).a);
                neighbor = min(neighbor, tex2D(_MainTex, input.uv - offsetX - offsetY).a);
                if (_AlphaThreshold > 0)
                {
                    center = step(_AlphaThreshold, center);
                    neighbor = step(_AlphaThreshold, neighbor);
                }
                return fixed4(_EdgeColor.rgb, saturate(center - neighbor) * _EdgeColor.a * input.color.a);
            }
            ENDCG
        }
    }
}
