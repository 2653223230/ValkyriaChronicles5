Shader "VC5PvE/ScenicBackdrop"
{
    Properties { _BaseMap("Landscape", 2D)="white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;return v;}
            half4 Frag(V v):SV_Target{return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv);}
            ENDHLSL
        }
    }
}
