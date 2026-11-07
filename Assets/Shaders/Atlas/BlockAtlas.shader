Shader "Custom/Block Atlas Stable"
{
    Properties
    {
        [MainTexture]
        [NoScaleOffset]
        _MainTex ("Atlas Texture", 2D) = "white" {}

        _AtlasColumns ("Atlas Columns", Float) = 6
        _AtlasRows ("Atlas Rows", Float) = 2

        _TextureIndex ("Texture Index", Float) = 0

        _Brightness ("Brightness", Range(0, 2)) = 1

        _DamageColor ("Damage Overlay Color", Color) = (1, 0, 0, 1)

        _DamageFlash ("Damage Flash Amount", Range(0, 1)) = 0

        _DamageOverlayStrength
        (
            "Damage Overlay Strength",
            Range(0, 1)
        ) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _AtlasColumns;
            float _AtlasRows;
            float _Brightness;

            float _DamageOverlayStrength;

            UNITY_INSTANCING_BUFFER_START(Props)

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _TextureIndex
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _DamageFlash
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float4,
                    _DamageColor
                )

            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata input)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );

                output.vertex = UnityObjectToClipPos(
                    input.vertex
                );

                output.uv = input.uv;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float textureIndex =
                    UNITY_ACCESS_INSTANCED_PROP(
                        Props,
                        _TextureIndex
                    );

                float damageFlash =
                    UNITY_ACCESS_INSTANCED_PROP(
                        Props,
                        _DamageFlash
                    );

                float4 damageColor =
                    UNITY_ACCESS_INSTANCED_PROP(
                        Props,
                        _DamageColor
                    );

                float columns = max(
                    1.0,
                    floor(_AtlasColumns)
                );

                float rows = max(
                    1.0,
                    floor(_AtlasRows)
                );

                float textureCount =
                    columns * rows;

                float safeIndex = clamp(
                    floor(textureIndex),
                    0.0,
                    textureCount - 1.0
                );

                /*
                 * Индексы:
                 *
                 * 0  1  2  3  4  5
                 * 6  7  8  9 10 11
                 */
                float column = fmod(
                    safeIndex,
                    columns
                );

                float rowFromTop = floor(
                    safeIndex / columns
                );

                float rowFromBottom =
                    rows - 1.0 - rowFromTop;

                float2 localUV =
                    saturate(input.uv);

                float2 tileSize = float2(
                    1.0 / columns,
                    1.0 / rows
                );

                float2 tileMin = float2(
                    column * tileSize.x,
                    rowFromBottom * tileSize.y
                );

                /*
                 * Берём центры крайних пикселей,
                 * чтобы не цеплять соседние клетки.
                 */
                float2 halfTexel =
                    _MainTex_TexelSize.xy * 0.5;

                float2 uvMin =
                    tileMin + halfTexel;

                float2 uvMax =
                    tileMin + tileSize - halfTexel;

                float2 atlasUV = lerp(
                    uvMin,
                    uvMax,
                    localUV
                );

                /*
                 * Mipmap level 0.
                 */
                float4 color = tex2Dlod(
                    _MainTex,
                    float4(atlasUV, 0.0, 0.0)
                );

                color.rgb *= _Brightness;

                /*
                 * Исходный цвет текстуры.
                 */
                float3 originalColor =
                    color.rgb;

                /*
                 * Цвет overlay.
                 *
                 * Используем смешивание, а не полную замену.
                 * Поэтому детали текстуры остаются видимыми.
                 */
                float3 overlayColor = lerp(
                    originalColor,
                    originalColor * 0.35 +
                    damageColor.rgb * 0.65,
                    _DamageOverlayStrength
                );

                /*
                 * Плавно включаем overlay
                 * только во время получения урона.
                 */
                color.rgb = lerp(
                    originalColor,
                    overlayColor,
                    saturate(damageFlash)
                );

                return color;
            }

            ENDCG
        }
    }

    FallBack Off
}