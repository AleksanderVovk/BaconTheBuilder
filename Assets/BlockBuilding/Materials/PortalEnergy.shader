Shader "BlockBuilding/Portal Energy"
{
    Properties
    {
        _Color ("Glow", Color) = (.48, .05, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            Varyings vert(Attributes a)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(a.positionOS.xyz);
                o.uv = a.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float2 grid = floor(float2(uv.x * 12, (uv.y - _Time.y * .08) * 18));
                float noise = frac(sin(dot(grid, float2(127.1, 311.7))) * 43758.5453);
                float edge = 1 - saturate(min(min(uv.x, 1 - uv.x), min(uv.y, 1 - uv.y)) * 10);
                float flow = .5 + .5 * sin(uv.y * 24 + sin(uv.x * 9 + _Time.y) * 2 - _Time.y * 2);
                half3 core = lerp(half3(.13, .015, .37), _Color.rgb, flow * .5 + noise * .25);
                return half4(lerp(core, half3(.45, .8, 1), edge * .8) + step(.965, noise) * half3(.27, .2, .4), 1);
            }
            ENDHLSL
        }
    }
}
