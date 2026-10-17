// Основной PBR-шейдер (built-in render pipeline).
// Самодостаточный surface shader, чтобы не зависеть от Standard и не выпасть при стриппинге.
Shader "NPP/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0.1
        _Glossiness ("Smoothness", Range(0,1)) = 0.35
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _BumpScale;
        half _Metallic;
        half _Glossiness;
        fixed4 _EmissionColor;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap), _BumpScale);
            o.Emission = _EmissionColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
