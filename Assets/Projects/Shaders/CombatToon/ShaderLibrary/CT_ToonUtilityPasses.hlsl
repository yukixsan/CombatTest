#ifndef CT_TOON_UTILITY_PASSES_INCLUDED
#define CT_TOON_UTILITY_PASSES_INCLUDED

// Outline, ShadowCaster, DepthOnly and DepthNormals passes shared by both CombatToon shaders.
// Depth / DepthNormals matter here: the volumetric fog raymarch and SSAO both read the
// camera depth texture, so every opaque CombatToon surface must write into it.

#include "CT_ToonInput.hlsl"

struct CT_AttributesBasic
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ---------------------------------------------------------------------------
// Outline (inverted hull). Width convention matches Toon City's TC_CustomToonOutline
// so converted materials keep their look; extruded along the world normal so a
// scaled FBX (e.g. 0.01) doesn't shrink the outline.
// ---------------------------------------------------------------------------
#if defined(CT_OUTLINE_PASS)
#include "CT_ToonLighting.hlsl"

struct CT_OutlineVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half   fogFactor  : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

CT_OutlineVaryings CT_OutlineVertex(CT_AttributesBasic input)
{
    CT_OutlineVaryings output = (CT_OutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS   = normalize(TransformObjectToWorldNormal(input.normalOS));
    float  clipW      = TransformWorldToHClip(positionWS).w;
    positionWS       += normalWS * _OutlineWidth * min(clipW, 1.5);

    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
    output.fogFactor  = ComputeFogFactor(output.positionCS.z);
    return output;
}

half4 CT_OutlineFragment(CT_OutlineVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half4 albedo = SampleAlbedo(input.uv);
    ClipAlpha(albedo.a);

    half3 color = lerp(_OutlineColor.rgb, albedo.rgb * _OutlineColor.rgb, _OutlineAlbedoMix);
    color = CT_ApplyHaze(color, input.positionWS);
    color = MixFog(color, input.fogFactor);
    return half4(color, 1.0h);
}
#endif // CT_OUTLINE_PASS

// ---------------------------------------------------------------------------
// ShadowCaster
// ---------------------------------------------------------------------------
#if defined(CT_SHADOW_PASS)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct CT_ShadowVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

CT_ShadowVaryings CT_ShadowVertex(CT_AttributesBasic input)
{
    CT_ShadowVaryings output = (CT_ShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
        float3 lightDirectionWS = _LightDirection;
    #endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    output.positionCS = ApplyShadowClamping(positionCS);
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 CT_ShadowFragment(CT_ShadowVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(_ALPHATEST_ON)
        ClipAlpha(SampleAlbedo(input.uv).a);
    #endif
    return 0;
}
#endif // CT_SHADOW_PASS

// ---------------------------------------------------------------------------
// DepthOnly
// ---------------------------------------------------------------------------
#if defined(CT_DEPTH_PASS)
struct CT_DepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

CT_DepthVaryings CT_DepthVertex(CT_AttributesBasic input)
{
    CT_DepthVaryings output = (CT_DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half CT_DepthFragment(CT_DepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(_ALPHATEST_ON)
        ClipAlpha(SampleAlbedo(input.uv).a);
    #endif
    return input.positionCS.z;
}
#endif // CT_DEPTH_PASS

// ---------------------------------------------------------------------------
// DepthNormals (SSAO / screen-space outline sources)
// ---------------------------------------------------------------------------
#if defined(CT_DEPTH_NORMALS_PASS)
#if defined(_WRITE_RENDERING_LAYERS)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#endif

struct CT_DepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    half3  normalWS   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

CT_DepthNormalsVaryings CT_DepthNormalsVertex(CT_AttributesBasic input)
{
    CT_DepthNormalsVaryings output = (CT_DepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
    output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

void CT_DepthNormalsFragment(CT_DepthNormalsVaryings input
    , out half4 outNormalWS : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    #if defined(_ALPHATEST_ON)
        ClipAlpha(SampleAlbedo(input.uv).a);
    #endif
    outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
    #if defined(_WRITE_RENDERING_LAYERS)
        outRenderingLayers = EncodeMeshRenderingLayer();
    #endif
}
#endif // CT_DEPTH_NORMALS_PASS

#endif // CT_TOON_UTILITY_PASSES_INCLUDED
