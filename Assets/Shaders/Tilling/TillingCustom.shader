Shader "Custom/URP/DynamicTiling_URP_Mobile_Ambient_Normal"
{
    Properties
    {
        _MainTex ("Albedo (RGB) + Alpha", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1.0

        _OffsetX ("Offset X", Float) = 0.0
        _OffsetY ("Offset Y", Float) = 0.0
        _Scale ("Tiling Scale", Range(0.01, 50)) = 1.0

        _Metallic ("Metallic (approx)", Range(0,1)) = 0.0
        _Smoothness ("Smoothness (0..1)", Range(0,1)) = 0.5

        _Color ("Color Tint", Color) = (1,1,1,1)
        _AmbientIntensity ("Ambient Light Intensity", Range(0,2)) = 1.0
        _NormalStrength ("Normal Influence", Range(0,1)) = 1.0

        _IsDynamic ("Is Dynamic", Float) = 0
        _ObjectScale ("Object Scale", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // DBuffer-варианты для URP Decal Projector.
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float _BumpScale;

                float _OffsetX;
                float _OffsetY;
                float _Scale;

                float _Metallic;
                float _Smoothness;

                float4 _Color;

                float _AmbientIntensity;
                float _NormalStrength;

                float _IsDynamic;
                float4 _ObjectScale;

            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;

                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;

                float3 localPos : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 UnpackNormalFast(float4 nmap)
            {
                float3 normalTS;

                normalTS.xy = nmap.xy * 2.0 - 1.0;

                normalTS.z = sqrt(
                    saturate(
                        1.0 - dot(
                            normalTS.xy,
                            normalTS.xy
                        )
                    )
                );

                return normalTS;
            }

            void TriplanarUVs(
                float3 position,
                float3 normal,
                out float2 uvX,
                out float2 uvY,
                out float2 uvZ,
                out float3 blend
            )
            {
                float2 offset = float2(
                    _OffsetX,
                    _OffsetY
                );

                uvX = position.yz * _Scale + offset;
                uvY = position.xz * _Scale + offset;
                uvZ = position.xy * _Scale + offset;

                float3 weights = abs(normal);

                blend = weights / max(
                    weights.x + weights.y + weights.z,
                    0.0001
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        input.normalOS,
                        input.tangentOS
                    );

                output.positionHCS =
                    positionInputs.positionCS;

                output.worldPos =
                    positionInputs.positionWS;

                output.worldNormal =
                    normalize(normalInputs.normalWS);

                output.localPos =
                    input.positionOS.xyz;

                return output;
            }

            void InitializeInputDataCustom(
                Varyings input,
                float3 normalWS,
                out InputData inputData
            )
            {
                inputData = (InputData)0;

                inputData.positionWS =
                    input.worldPos;

                inputData.normalWS =
                    normalize(normalWS);

                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(
                        input.worldPos
                    );

                inputData.shadowCoord =
                    TransformWorldToShadowCoord(
                        input.worldPos
                    );

                inputData.fogCoord =
                    ComputeFogFactor(
                        input.positionHCS.z
                    );

                inputData.vertexLighting =
                    float3(0.0, 0.0, 0.0);

                inputData.bakedGI =
                    SampleSH(inputData.normalWS);

                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        input.positionHCS
                    );

                inputData.shadowMask =
                    half4(1.0, 1.0, 1.0, 1.0);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 baseNormalWS =
                    normalize(input.worldNormal);

                float dynamicMode =
                    saturate(_IsDynamic);

                float3 positionForUV =
                    lerp(
                        input.worldPos,
                        input.localPos * _ObjectScale.xyz,
                        dynamicMode
                    );

                float2 uvX;
                float2 uvY;
                float2 uvZ;
                float3 blend;

                TriplanarUVs(
                    positionForUV,
                    baseNormalWS,
                    uvX,
                    uvY,
                    uvZ,
                    blend
                );

                // Albedo.
                float4 albedoX =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uvX
                    );

                float4 albedoY =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uvY
                    );

                float4 albedoZ =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uvZ
                    );

                float3 albedo =
                    (
                        albedoX.rgb * blend.x +
                        albedoY.rgb * blend.y +
                        albedoZ.rgb * blend.z
                    ) * _Color.rgb;

                // Сохраняем исходное поведение.
                float alpha =
                    albedoX.a * _Color.a;

                // Normal map.
                float3 normalX =
                    UnpackNormalFast(
                        SAMPLE_TEXTURE2D(
                            _BumpMap,
                            sampler_BumpMap,
                            uvX
                        )
                    );

                float3 normalY =
                    UnpackNormalFast(
                        SAMPLE_TEXTURE2D(
                            _BumpMap,
                            sampler_BumpMap,
                            uvY
                        )
                    );

                float3 normalZ =
                    UnpackNormalFast(
                        SAMPLE_TEXTURE2D(
                            _BumpMap,
                            sampler_BumpMap,
                            uvZ
                        )
                    );

                float3 worldNormalX =
                    float3(
                        normalX.z,
                        normalX.x,
                        normalX.y
                    );

                float3 worldNormalY =
                    float3(
                        normalY.x,
                        normalY.z,
                        normalY.y
                    );

                float3 worldNormalZ =
                    float3(
                        normalZ.x,
                        normalZ.y,
                        normalZ.z
                    );

                float3 triplanarNormalWS =
                    normalize(
                        worldNormalX * blend.x +
                        worldNormalY * blend.y +
                        worldNormalZ * blend.z
                    );

                float bumpScale =
                    saturate(_BumpScale);

                float3 normalWS =
                    normalize(
                        lerp(
                            baseNormalWS,
                            triplanarNormalWS,
                            bumpScale
                        )
                    );

                // Исходное ambient-затемнение.
                float normalStrength =
                    saturate(_NormalStrength);

                float normalFactor =
                    0.5 + 0.5 * normalWS.y;

                normalFactor =
                    lerp(
                        1.0,
                        normalFactor,
                        normalStrength
                    );

                float3 finalAlbedo =
                    albedo *
                    _AmbientIntensity *
                    normalFactor;

                // Заполняем URP SurfaceData.
                SurfaceData surfaceData =
                    (SurfaceData)0;

                surfaceData.albedo =
                    finalAlbedo;

                surfaceData.alpha =
                    alpha;

                surfaceData.metallic =
                    saturate(_Metallic);

                surfaceData.specular =
                    half3(0.0, 0.0, 0.0);

                surfaceData.smoothness =
                    saturate(_Smoothness);

                // Нормаль уже рассчитана в World Space.
                surfaceData.normalTS =
                    half3(0.0, 0.0, 1.0);

                surfaceData.occlusion =
                    half(1.0);

                surfaceData.emission =
                    half3(0.0, 0.0, 0.0);

                #if defined(SURFACE_DATA_CLEAR_COAT)

                    surfaceData.clearCoatMask =
                        half(0.0);

                    surfaceData.clearCoatSmoothness =
                        half(0.0);

                #endif

                InputData inputData;

                InitializeInputDataCustom(
                    input,
                    normalWS,
                    inputData
                );

                // Для твоей версии URP функция принимает 3 параметра.
                #if defined(_DBUFFER_MRT1) || \
                    defined(_DBUFFER_MRT2) || \
                    defined(_DBUFFER_MRT3)

                    ApplyDecalToSurfaceData(
                        input.positionHCS,
                        surfaceData,
                        inputData
                    );

                #endif

                half4 color =
                    UniversalFragmentPBR(
                        inputData,
                        surfaceData
                    );

                color.rgb =
                    MixFog(
                        color.rgb,
                        inputData.fogCoord
                    );

                color.a =
                    alpha;

                return color;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}