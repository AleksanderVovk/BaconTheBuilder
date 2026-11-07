Shader "BlockBuilding/Pixel Atlas"
{
    Properties
    {
        _BaseMap("Padded pixel atlas",2D)="white" {}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Ghost("Ghost preview",Float)=0
        _Brightness("Block brightness",Range(0.5,2))=1.25
        _Saturation("Block saturation",Range(0,2))=1.18
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        Pass
        {
            Name "VoxelForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Ghost;
                half _Brightness;
                half _Saturation;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 faceUV : TEXCOORD1;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 faceUV : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 color : COLOR;
            };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                o.faceUV = input.faceUV;
                o.color = input.color;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                tex.rgb *= input.color.rgb;
                half luminance = dot(tex.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                tex.rgb = max(0.0h, lerp(luminance.xxx, tex.rgb, _Saturation)) * _Brightness;
                if (input.color.a < 0.5h)
                {
                    // Clear pane with a cyan frame and diagonal glints. One cutout material,
                    // correct depth, no transparent sorting and no second draw call.
                    float2 p = input.faceUV;
                    float edge = min(min(p.x, p.y), min(1 - p.x, 1 - p.y));
                    float diagonal = min(abs(p.x - p.y - 0.22), abs(p.x - p.y + 0.32));
                    clip(max(0.045 - edge, 0.018 - diagonal));
                    tex.rgb = lerp(tex.rgb, half3(0.65, 0.95, 1), 0.3h);
                }
                if (_Ghost > 0.5)
                {
                    float2 cell = floor(input.positionCS.xy / 4);
                    clip(frac((cell.x + cell.y) * 0.5) - 0.25);
                    tex.rgb = lerp(tex.rgb, half3(0.15, 0.9, 1), 0.65h);
                }
                half shade = 0.82h + 0.12h * saturate(input.normalWS.y) + 0.06h * saturate(dot(input.normalWS, normalize(half3(-0.5, 0.6, -0.4))));
                return half4(tex.rgb * shade, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Ghost;
                half _Brightness;
                half _Saturation;
            CBUFFER_END
            struct A
            {
                float4 p : POSITION;
                float2 uv : TEXCOORD1;
                half4 color : COLOR;
            };
            struct V
            {
                float4 p : SV_POSITION;
                float2 uv : TEXCOORD0;
                half flag : COLOR;
            };
            V Vert(A i)
            {
                V o;
                o.p = TransformObjectToHClip(i.p.xyz);
                o.uv = i.uv;
                o.flag = i.color.a;
                return o;
            }
            half4 Frag(V i) : SV_Target
            {
                if (i.flag < 0.5h)
                {
                    float2 p = i.uv;
                    float e = min(min(p.x, p.y), min(1 - p.x, 1 - p.y));
                    float d = min(abs(p.x - p.y - 0.22), abs(p.x - p.y + 0.32));
                    clip(max(0.045 - e, 0.018 - d));
                }
                if (_Ghost > 0.5)
                {
                    float2 c = floor(i.p.xy / 4);
                    clip(frac((c.x + c.y) * 0.5) - 0.25);
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
