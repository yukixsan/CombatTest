#ifndef CT_TOON_LIGHTING_INCLUDED
#define CT_TOON_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ---------------------------------------------------------------------------
// Atmosphere
// ---------------------------------------------------------------------------

// Stylised aerial perspective: distant / low geometry fades toward the haze colour.
// When a CombatAtmosphere component is active, its global values replace the
// per-material ones so all materials agree with the volumetric fog tint.
half3 CT_ApplyHaze(half3 color, float3 positionWS)
{
    bool useGlobal = (_UseGlobalAtmosphere > 0.5h) && (_CT_AtmosphereColor.a > 0.5h);

    half3  hazeColor = useGlobal ? _CT_AtmosphereColor.rgb : _HazeColor.rgb;
    float4 p         = useGlobal ? _CT_AtmosphereParams
                                 : float4(_HazeStart, _HazeEnd, _HazeHeightBase, _HazeHeightFalloff);
    half strength    = _HazeStrength * (useGlobal ? (half)_CT_AtmosphereParams2.x : 1.0h);

    float dist     = distance(positionWS, GetCameraPositionWS());
    float distMask = saturate((dist - p.x) / max(p.y - p.x, 0.001));
    float height   = exp2(-max(0.0, positionWS.y - p.z) * p.w);

    return lerp(color, hazeColor, saturate(distMask * height * strength));
}

// ---------------------------------------------------------------------------
// Cel lighting
// ---------------------------------------------------------------------------

struct CT_SurfaceData
{
    half3  albedo;
    half3  normalWS;
    half3  viewDirWS;
    float3 positionWS;
    float4 shadowCoord;
    float2 normalizedScreenUV;
};

// Flattened ambient: blends per-normal SH with the "up" probe so toon surfaces
// don't pick up noisy gradients, while still following the scene's sky colour.
half3 CT_FlatAmbient(half3 normalWS)
{
    half3 shNormal = SampleSH(normalWS);
    half3 shUp     = SampleSH(half3(0, 1, 0));
    return lerp(shNormal, shUp, 0.5h);
}

half CT_MainLightMask(half halfLambert, half shadowAtten)
{
    return smoothstep(_ShadowThreshold - _ShadowSoftness,
                      _ShadowThreshold + _ShadowSoftness,
                      halfLambert) * shadowAtten;
}

half3 CT_ShadeToon(CT_SurfaceData s)
{
    half3 N = s.normalWS;
    half3 V = s.viewDirWS;

    // --- Main light -------------------------------------------------------
    Light mainLight  = GetMainLight(s.shadowCoord, s.positionWS, half4(1, 1, 1, 1));
    half  NdotL      = dot(N, mainLight.direction);
    half  halfLambert= NdotL * 0.5h + 0.5h;
    half  shadowAtten= lerp(1.0h, mainLight.shadowAttenuation, _ReceivedShadowStrength)
                       * mainLight.distanceAttenuation;

    half  litMask;
    half3 direct;
    #if defined(_RAMPMAP_ON)
        // Same lookup convention as the Toon City ramp textures (x = half-lambert).
        half rampU = halfLambert * shadowAtten;
        direct  = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(rampU, 0.5)).rgb;
        litMask = saturate(rampU);
    #else
        litMask = CT_MainLightMask(halfLambert, shadowAtten);
        direct  = lerp(_ShadowColor.rgb, half3(1, 1, 1), litMask);
    #endif

    half3 color = s.albedo * direct * mainLight.color;

    // --- Ambient (+ SSAO) ---------------------------------------------------
    // Ambient fills the shadow side only; the lit side stays at albedo * sun so
    // textures keep their saturation instead of washing out under a bright sky.
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(s.normalizedScreenUV);
    half ambientMask = lerp(1.0h, 0.15h, litMask);
    color += s.albedo * CT_FlatAmbient(N) * _AmbientStrength * ambientMask * ao.indirectAmbientOcclusion;

    // --- Specular (character only) -------------------------------------------
    #if defined(_CT_SPECULAR)
        half3 H     = SafeNormalize(mainLight.direction + V);
        half  NdotH = saturate(dot(N, H));
        half  edge  = 1.0h - _SpecularSize;
        half  spec  = smoothstep(edge - _SpecularSoftness, edge + _SpecularSoftness, NdotH) * litMask;
        color += _SpecularColor.rgb * mainLight.color * spec;
    #endif

    // --- Additional lights (point / spot), banded --------------------------
    #if defined(_ADDITIONAL_LIGHTS)
        InputData inputData = (InputData)0;
        inputData.positionWS              = s.positionWS;
        inputData.normalWS                = N;
        inputData.viewDirectionWS         = V;
        inputData.normalizedScreenSpaceUV = s.normalizedScreenUV;

        half soft = max(_AdditionalLightSoftness, 0.001h);

        #if USE_CLUSTER_LIGHT_LOOP
        [loop] for (uint li = 0; li < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); li++)
        {
            Light dl = GetAdditionalLight(li, s.positionWS, half4(1, 1, 1, 1));
            half  dm = smoothstep(0.0h, soft, saturate(dot(N, dl.direction))) * dl.shadowAttenuation;
            color += s.albedo * dl.color * dl.distanceAttenuation * dm;
        }
        #endif

        uint pixelLightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, s.positionWS, half4(1, 1, 1, 1));
            half  atten = light.distanceAttenuation * light.shadowAttenuation;
            half  band  = smoothstep(0.0h, soft, saturate(dot(N, light.direction)));
            color += s.albedo * light.color * atten * band;
        LIGHT_LOOP_END
    #endif

    // --- Atmospheric rim ---------------------------------------------------
    // Brighter when the sun is behind the subject (the same angle that makes
    // volumetric god rays visible), and tinted by the sun colour so the rim
    // reads as the same light that is scattering in the fog.
    half NdotV   = saturate(dot(N, V));
    half rimEdge = 1.0h - _RimWidth;
    half rim     = smoothstep(rimEdge - _RimSoftness, rimEdge + _RimSoftness, 1.0h - NdotV);
    half backlit = saturate(-dot(V, mainLight.direction));
    half rimMask = rim * lerp(0.35h, 1.0h, halfLambert) * (1.0h + backlit * _RimBacklitBoost);
    half3 rimTint= _RimColor.rgb * lerp(half3(1, 1, 1), mainLight.color, _RimLightInfluence);
    color += rimTint * rimMask * _RimColor.a;

    return color;
}

#endif // CT_TOON_LIGHTING_INCLUDED
