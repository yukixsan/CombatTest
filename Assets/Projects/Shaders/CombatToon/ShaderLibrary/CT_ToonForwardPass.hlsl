#ifndef CT_TOON_FORWARD_PASS_INCLUDED
#define CT_TOON_FORWARD_PASS_INCLUDED

#include "CT_ToonInput.hlsl"
#include "CT_ToonLighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3  normalWS   : TEXCOORD2;
    half   fogFactor  : TEXCOORD3;
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord: TEXCOORD4;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings CT_ForwardVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs   vni = GetVertexNormalInputs(input.normalOS);

    output.positionCS = vpi.positionCS;
    output.positionWS = vpi.positionWS;
    output.normalWS   = vni.normalWS;
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
    output.fogFactor  = ComputeFogFactor(vpi.positionCS.z);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vpi);
    #endif
    return output;
}

half4 CT_ForwardFragment(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 albedo = SampleAlbedo(input.uv);
    ClipAlpha(albedo.a);

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    CT_SurfaceData s;
    s.albedo     = albedo.rgb;
    s.normalWS   = NormalizeNormalPerPixel(input.normalWS) * (IS_FRONT_VFACE(facing, 1.0h, -1.0h));
    s.positionWS = input.positionWS;
    s.viewDirWS  = GetWorldSpaceNormalizeViewDir(input.positionWS);
    s.normalizedScreenUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        s.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        s.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #else
        s.shadowCoord = float4(0, 0, 0, 0);
    #endif

    half3 color = CT_ShadeToon(s);

    #if defined(_EMISSION)
        color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
    #endif

    color = CT_ApplyHaze(color, input.positionWS);
    color = lerp(color, _FlashColor.rgb, _FlashAmount);
    color = MixFog(color, input.fogFactor);

    return half4(color, 1.0h);
}

#endif // CT_TOON_FORWARD_PASS_INCLUDED
