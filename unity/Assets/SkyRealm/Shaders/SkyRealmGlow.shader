// Simple unlit glow / transparent shader for the Sky Realm world (URP).
// Used for rune circles, flames, clouds, petals, waterfalls, portals and the moon.
Shader "SkyRealm/Glow"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        [HDR] _BaseColor ("Color", Color) = (1,1,1,1)
        _Scroll ("UV Scroll (xy per second)", Vector) = (0,0,0,0)
        _FogAmount ("Fog Amount", Range(0,1)) = 1
        _Additive ("Fog fades to black (additive)", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Glow"
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _Scroll;
                half _FogAmount;
                half _Additive;
                half _SrcBlend;
                half _DstBlend;
                half _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half fog : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap) + _Scroll.xy * _Time.y;
                o.color = v.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor * i.color;
                half3 toFog = MixFog(c.rgb, i.fog);
                half3 toBlack = MixFogColor(c.rgb, half3(0, 0, 0), i.fog);
                half3 fogged = lerp(toFog, toBlack, _Additive);
                c.rgb = lerp(c.rgb, fogged, _FogAmount);
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
