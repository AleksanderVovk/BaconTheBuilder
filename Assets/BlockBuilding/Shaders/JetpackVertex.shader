Shader "BlockBuilding/Jetpack Vertex"
{
    SubShader
    {
        Tags {"RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"}
        Pass
        {
            Tags {"LightMode" = "UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A
            {
                float4 p : POSITION;
                float3 n : NORMAL;
                half4 c : COLOR;
            };
            struct V
            {
                float4 p : SV_POSITION;
                half4 c : COLOR;
                half3 n : TEXCOORD0;
            };
            V Vert(A i)
            {
                V o;
                o.p = TransformObjectToHClip(i.p.xyz);
                o.n = TransformObjectToWorldNormal(i.n);
                o.c = i.c;
                return o;
            }
            half4 Frag(V i) : SV_Target
            {
                half shade = .8h + .12h * saturate(i.n.y) + .08h * saturate(dot(normalize(i.n), normalize(half3(-.4, .8, -.5))));
                return half4(i.c.rgb * shade, 1);
            }
            ENDHLSL
        }
    }
}
