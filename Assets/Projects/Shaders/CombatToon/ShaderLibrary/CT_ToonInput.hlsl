#ifndef CT_TOON_INPUT_INCLUDED
#define CT_TOON_INPUT_INCLUDED

// Shared material inputs for CombatToon/Environment and CombatToon/Character.
// Both shaders declare the same CBUFFER so every pass stays SRP Batcher compatible.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_RampMap);        SAMPLER(sampler_RampMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _Cutoff;

    // Cel lighting
    half4  _ShadowColor;
    half   _ShadowThreshold;
    half   _ShadowSoftness;
    half   _ReceivedShadowStrength;
    half   _AmbientStrength;
    half   _AdditionalLightSoftness;

    // Specular (character)
    half4  _SpecularColor;
    half   _SpecularSize;
    half   _SpecularSoftness;

    // Atmospheric rim — tinted by the sun so silhouettes read through volumetric fog
    half4  _RimColor;
    half   _RimWidth;
    half   _RimSoftness;
    half   _RimLightInfluence;
    half   _RimBacklitBoost;

    // Emission
    half4  _EmissionColor;

    // Aerial-perspective haze (environment depth layering)
    half4  _HazeColor;
    half   _HazeStrength;
    half   _HazeStart;
    half   _HazeEnd;
    half   _HazeHeightBase;
    half   _HazeHeightFalloff;
    half   _UseGlobalAtmosphere;

    // Outline (inverted hull)
    half4  _OutlineColor;
    half   _OutlineWidth;
    half   _OutlineAlbedoMix;

    // Hit flash (character)
    half4  _FlashColor;
    half   _FlashAmount;
CBUFFER_END

// Written by CombatAtmosphere.cs so every CombatToon material shares the same haze.
// rgb = colour, a = 1 when a CombatAtmosphere is active in the scene.
half4 _CT_AtmosphereColor;
// x = start distance, y = end distance, z = height base, w = height falloff
float4 _CT_AtmosphereParams;
// x = strength multiplier
float4 _CT_AtmosphereParams2;

half4 SampleAlbedo(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
}

void ClipAlpha(half alpha)
{
    #if defined(_ALPHATEST_ON)
        clip(alpha - _Cutoff);
    #endif
}

#endif // CT_TOON_INPUT_INCLUDED
