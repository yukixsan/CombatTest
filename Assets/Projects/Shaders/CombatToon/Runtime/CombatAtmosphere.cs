using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Drives the global haze used by every CombatToon material (_CT_Atmosphere*),
/// so environment depth-fade, characters and the URP Volumetric Fog agree on one colour.
/// One per scene. Runs in edit mode so the look is visible without entering Play.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class CombatAtmosphere : MonoBehaviour
{
    [Header("Haze")]
    [Tooltip("Base haze colour. Setup copies the scene fog colour here.")]
    public Color hazeColor = new Color(0.78f, 0.84f, 1f, 1f);

    [Tooltip("Distance from camera where haze starts. Keep it behind the gameplay plane.")]
    [Min(0f)] public float hazeStart = 45f;

    [Min(0f)] public float hazeEnd = 180f;

    [Tooltip("World Y where haze is at full height strength.")]
    public float heightBase = 0f;

    [Tooltip("How fast haze thins out above Height Base (0 = uniform).")]
    [Min(0f)] public float heightFalloff = 0.02f;

    [Range(0f, 1f)] public float strength = 1f;

    [Header("Sync")]
    [Tooltip("Volume holding the Volumetric Fog override. Its tint colours the haze.")]
    public Volume fogVolume;

    [Range(0f, 1f)] public float fogTintInfluence = 0.6f;

    [Tooltip("Sun used to warm/cool the haze like the in-scattered fog light.")]
    public Light sun;

    [Range(0f, 1f)] public float sunColorInfluence = 0.5f;

    private static readonly int AtmosphereColorId   = Shader.PropertyToID("_CT_AtmosphereColor");
    private static readonly int AtmosphereParamsId  = Shader.PropertyToID("_CT_AtmosphereParams");
    private static readonly int AtmosphereParams2Id = Shader.PropertyToID("_CT_AtmosphereParams2");

    private void OnEnable()   => Apply();
    private void OnValidate() => Apply();
    private void Update()     => Apply();

    private void OnDisable()
    {
        // a = 0 tells the shaders to fall back to their per-material haze values.
        Shader.SetGlobalColor(AtmosphereColorId, new Color(0f, 0f, 0f, 0f));
    }

    private void Apply()
    {
        if (!isActiveAndEnabled) return;

        Color color = hazeColor;

        Color fogTint = Color.white;
#if CT_VOLUMETRIC_FOG
        if (fogVolume != null && fogVolume.sharedProfile != null
            && fogVolume.sharedProfile.TryGet(out VolumetricFogVolumeComponent fog)
            && fog.active && fog.enabled.value)
        {
            fogTint = fog.tint.value;
        }
#endif
        color *= Color.Lerp(Color.white, fogTint, fogTintInfluence);

        if (sun != null && sun.isActiveAndEnabled)
            color *= Color.Lerp(Color.white, sun.color, sunColorInfluence);

        color.a = 1f;

        Shader.SetGlobalColor(AtmosphereColorId, color);
        Shader.SetGlobalVector(AtmosphereParamsId,
            new Vector4(hazeStart, Mathf.Max(hazeEnd, hazeStart + 0.01f), heightBase, heightFalloff));
        Shader.SetGlobalVector(AtmosphereParams2Id, new Vector4(strength, 0f, 0f, 0f));
    }
}
