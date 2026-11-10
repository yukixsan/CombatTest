// CombatToon / Character
// Same lighting model as CombatToon/Environment so characters and level share one
// light response, tuned to read clearly through volumetric fog:
//  - strong sun-tinted rim (boosted when backlit) separates the silhouette from fog
//  - toon specular highlight
//  - haze off by default so the gameplay plane stays crisp
//  - _FlashColor / _FlashAmount for hit flashes (works with MaterialPropertyBlock)
//  - _BaseColor tint stays compatible with HitFX.cs
Shader "CombatToon/Character"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor ("Tint", Color) = (1,1,1,1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        [Header(Cel Lighting)]
        [Toggle(_RAMPMAP_ON)] _UseRamp ("Use Ramp Texture", Float) = 0
        [NoScaleOffset] _RampMap ("Ramp (x = half lambert)", 2D) = "white" {}
        _ShadowColor ("Shadow Tint", Color) = (0.62, 0.55, 0.7, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.52
        _ShadowSoftness ("Shadow Softness", Range(0.001,0.5)) = 0.02
        _ReceivedShadowStrength ("Received Shadow Strength", Range(0,1)) = 0.75
        _AmbientStrength ("Ambient Strength", Range(0,2)) = 0.5
        _AdditionalLightSoftness ("Point/Spot Light Band Softness", Range(0.001,1)) = 0.08

        [Header(Specular)]
        [Toggle(_CT_SPECULAR)] _UseSpecular ("Use Toon Specular", Float) = 1
        _SpecularColor ("Specular Color", Color) = (0.35, 0.35, 0.35, 1)
        _SpecularSize ("Specular Size", Range(0,0.5)) = 0.06
        _SpecularSoftness ("Specular Softness", Range(0.001,0.2)) = 0.01

        [Header(Atmospheric Rim)]
        _RimColor ("Rim Color (A = strength)", Color) = (1, 0.95, 0.85, 0.65)
        _RimWidth ("Rim Width", Range(0,1)) = 0.3
        _RimSoftness ("Rim Softness", Range(0.001,0.5)) = 0.04
        _RimLightInfluence ("Rim Takes Sun Color", Range(0,1)) = 0.8
        _RimBacklitBoost ("Backlit Boost", Range(0,4)) = 2

        [Header(Emission)]
        [Toggle(_EMISSION)] _UseEmission ("Use Emission", Float) = 0
        [NoScaleOffset] _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        [Header(Atmosphere Haze)]
        [Toggle] _UseGlobalAtmosphere ("Use Global Atmosphere (CombatAtmosphere)", Float) = 1
        _HazeStrength ("Haze Strength", Range(0,1)) = 0
        _HazeColor ("Local Haze Color", Color) = (0.78, 0.84, 1, 1)
        _HazeStart ("Local Haze Start", Float) = 45
        _HazeEnd ("Local Haze End", Float) = 180
        _HazeHeightBase ("Local Haze Height Base", Float) = 0
        _HazeHeightFalloff ("Local Haze Height Falloff", Float) = 0.02

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.08, 0.06, 0.1, 1)
        _OutlineWidth ("Outline Width", Range(0,0.5)) = 0.012
        _OutlineAlbedoMix ("Outline Takes Albedo", Range(0,1)) = 0.35

        [Header(Hit Flash)]
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Unlit" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CT_ForwardVertex
            #pragma fragment CT_ForwardFragment

            #pragma shader_feature_local _RAMPMAP_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _CT_SPECULAR
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "ShaderLibrary/CT_ToonForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CT_OutlineVertex
            #pragma fragment CT_OutlineFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #define CT_OUTLINE_PASS
            #include "ShaderLibrary/CT_ToonUtilityPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CT_ShadowVertex
            #pragma fragment CT_ShadowFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #define CT_SHADOW_PASS
            #include "ShaderLibrary/CT_ToonUtilityPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CT_DepthVertex
            #pragma fragment CT_DepthFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #define CT_DEPTH_PASS
            #include "ShaderLibrary/CT_ToonUtilityPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CT_DepthNormalsVertex
            #pragma fragment CT_DepthNormalsFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing

            #define CT_DEPTH_NORMALS_PASS
            #include "ShaderLibrary/CT_ToonUtilityPasses.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
