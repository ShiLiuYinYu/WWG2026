Shader "WWG/UI/Vitals Frame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _HealthTrack ("Health Track (UV Bounds)", Vector) = (0, 0, 0, 0)
        _SanTrack ("SAN Track (UV Bounds)", Vector) = (0, 0, 0, 0)
        _EmptyBrightness ("Empty Track Brightness", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" "RenderPipeline"="UniversalPipeline" }
        Cull Off
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
            float4 _HealthTrack;
            float4 _SanTrack;
            float _EmptyBrightness;

            struct Vertex
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Interpolators
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            Interpolators Vert(Vertex input)
            {
                Interpolators output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float InTrack(float2 uv, float4 bounds)
            {
                float2 inside = step(bounds.xy, uv) * step(uv, bounds.zw);
                return inside.x * inside.y;
            }

            fixed4 Frag(Interpolators input) : SV_Target
            {
                fixed4 frame = tex2D(_MainTex, input.uv) * input.color;
                float track = max(InTrack(input.uv, _HealthTrack), InTrack(input.uv, _SanTrack));
                frame.rgb *= lerp(1.0, _EmptyBrightness, track);
                return frame;
            }
            ENDCG
        }
    }
}
