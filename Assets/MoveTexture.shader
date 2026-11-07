Shader "Custom/MoveTexture"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _Speed ("Speed", Float) = 1.0
        _Direction ("Direction (XY)", Vector) = (1,0,0,0)
        _Tiling ("Tiling", Vector) = (1,1,0,0)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTest]
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Speed;
            float2 _Direction;
            float2 _Tiling;
            fixed4 _Color;
            float _Cutoff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Расчет смещения UV
                float2 uv = i.uv * _Tiling;
                uv += _Direction * _Speed * _Time.y;
                
                // Чтение текстуры с учетом прозрачности
                fixed4 col = tex2D(_MainTex, uv) * _Color * i.color;
                
                // Применение отсечения по альфа-каналу
                clip(col.a - _Cutoff);
                
                // Применение тумана
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/Diffuse"
}