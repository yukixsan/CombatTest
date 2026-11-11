using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-click setup for Level_01: URP Volumetric Fog + CombatToon shaders.
/// Everything goes through Undo, so Ctrl+Z reverts scene changes; material shader swaps keep
/// the old properties in the material, so switching the shader back restores the old look.
/// </summary>
public static class CombatToonSetup
{
    private const string Root            = "Assets/Projects/Shaders/CombatToon";
    private const string MenuRoot        = "Tools/Combat Toon/";
    private const string FogProfilePath  = Root + "/Level01_VolumetricFog.asset";
    private const string FogVolumeName   = "Volumetric Fog Volume";
    private const string AtmosphereName  = "Combat Atmosphere";
    private const string EnvShaderName   = "CombatToon/Environment";
    private const string CharShaderName  = "CombatToon/Character";
    private const string OutlinePass     = "SRPDefaultUnlit";

    // Volumes on this layer are only seen by cameras whose Volume Mask includes it.
    // TransparentFX (1) is in Main Camera's mask but not in the VFX / Background cameras',
    // so the fog is rendered exactly once in the camera stack.
    private const int FogVolumeLayer = 1;

    /// <summary>When true (automation / CLI), dialogs are logged instead of shown and confirmations auto-accept.</summary>
    public static bool SuppressDialogs;

    private static bool Dialog(string title, string message, string ok, string cancel = null)
    {
        if (SuppressDialogs || Application.isBatchMode)
        {
            Debug.Log($"[Combat Toon] {message}");
            return true;
        }
        return cancel == null
            ? EditorUtility.DisplayDialog(title, message, ok)
            : EditorUtility.DisplayDialog(title, message, ok, cancel);
    }

    // -----------------------------------------------------------------------
    // 1. Volumetric fog
    // -----------------------------------------------------------------------
    [MenuItem(MenuRoot + "1. Setup Volumetric Fog (open scene)", priority = 1)]
    private static void SetupVolumetricFog()
    {
#if !CT_VOLUMETRIC_FOG
        Dialog("Combat Toon",
            "Package com.cqf.urpvolumetricfog is not installed yet.\n" +
            "Wait for Package Manager to finish resolving it, then run this again.", "OK");
#else
        var rendererData = GetDefaultRendererData();
        if (rendererData == null)
        {
            Dialog("Combat Toon", "Could not find the active URP Universal Renderer Data.", "OK");
            return;
        }

        var log = new List<string>();
        EnsureFogRendererFeature(rendererData, log);

        Transform player = FindPlayer();
        Camera mainCam   = FindMainCamera();
        float groundY    = player != null ? player.position.y : 0f;
        float camDist    = (player != null && mainCam != null)
            ? Vector3.Distance(mainCam.transform.position, player.position) : 40f;

        VolumeProfile profile = GetOrCreateFogProfile(groundY, log);
        Volume volume = GetOrCreateFogVolume(profile, log);
        ConfigureCameraVolumeMasks(mainCam, log);
        AddVolumetricAdditionalLights(log);
        SetupAtmosphere(volume, groundY, camDist, log);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Combat Toon] Volumetric fog setup:\n- " + string.Join("\n- ", log));
        Dialog("Combat Toon", "Volumetric fog ready.\n\n- " + string.Join("\n- ", log) +
            "\n\nSave the scene to keep the changes.", "OK");
#endif
    }

#if CT_VOLUMETRIC_FOG
    private static void EnsureFogRendererFeature(UniversalRendererData data, List<string> log)
    {
        foreach (var f in data.rendererFeatures)
        {
            if (f is VolumetricFogRendererFeature)
            {
                if (!f.isActive) { f.SetActive(true); EditorUtility.SetDirty(data); log.Add("Re-enabled Volumetric Fog renderer feature"); }
                else log.Add("Volumetric Fog renderer feature already present");
                return;
            }
        }

        var feature = ScriptableObject.CreateInstance<VolumetricFogRendererFeature>();
        feature.name = "VolumetricFog";
        AssetDatabase.AddObjectToAsset(feature, data);
        data.rendererFeatures.Add(feature);

        // Keep URP's feature map in sync, the same way the renderer inspector does.
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId))
        {
            var so  = new SerializedObject(data);
            var map = so.FindProperty("m_RendererFeatureMap");
            if (map != null)
            {
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        data.SetDirty();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        log.Add("Added Volumetric Fog renderer feature to " + data.name);
    }

    private static VolumeProfile GetOrCreateFogProfile(float groundY, List<string> log)
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FogProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, FogProfilePath);
        }

        if (profile.TryGet(out VolumetricFogVolumeComponent fog))
        {
            log.Add("Fog profile exists, values left untouched (" + FogProfilePath + ")");
            return profile;
        }

        fog = profile.Add<VolumetricFogVolumeComponent>(false);
        fog.name = "VolumetricFog";
        AssetDatabase.AddObjectToAsset(fog, profile);

        // Low fog layer below the gameplay plane: the street gaps fill with glowing mist,
        // buildings rise out of it and the fighters (above the layer) stay crisp.
        // A fog volume that also fills the camera->player gap only washes the screen white,
        // and the main-light shadow distance (50 m) is too short to cut shafts at building range.
        // Pair with a backlit sun (menu 5) — forward scattering is ~20x stronger toward the camera.
        fog.enabled.Override(true);
        fog.distance.Override(220f);
        fog.baseHeight.Override(groundY - 65f);
        // Top slightly above the roofs so god-ray spots (menu 6) have fog to scatter in;
        // sun scattering kept low so the fog sea isn't white and the warm beams have contrast.
        fog.maximumHeight.Override(groundY + 6f);
        fog.density.Override(0.35f);
        fog.attenuationDistance.Override(140f);
        fog.enableMainLightContribution.Override(true);
        fog.anisotropy.Override(0.6f);
        fog.scattering.Override(0.12f);
        fog.tint.Override(new Color(1f, 0.95f, 0.88f, 1f));
        fog.enableAdditionalLightsContribution.Override(true);
        fog.maxSteps.Override(64);
        fog.blurIterations.Override(1);   // keeps cookie shafts readable

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        log.Add("Created fog profile " + FogProfilePath);
        return profile;
    }

    private static void AddVolumetricAdditionalLights(List<string> log)
    {
        int added = 0;
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light.type != LightType.Point && light.type != LightType.Spot) continue;
            if (light.GetComponent<VolumetricAdditionalLight>() != null) continue;
            if (light.GetComponent<UniversalAdditionalLightData>() == null)
                Undo.AddComponent<UniversalAdditionalLightData>(light.gameObject);
            Undo.AddComponent<VolumetricAdditionalLight>(light.gameObject);
            added++;
        }
        log.Add(added > 0 ? $"Added VolumetricAdditionalLight to {added} point/spot light(s)"
                          : "No new point/spot lights to register");
    }
#endif

    private static Volume GetOrCreateFogVolume(VolumeProfile profile, List<string> log)
    {
        GameObject go = GameObject.Find(FogVolumeName);
        if (go == null)
        {
            go = new GameObject(FogVolumeName);
            Undo.RegisterCreatedObjectUndo(go, "Create Volumetric Fog Volume");
            log.Add("Created '" + FogVolumeName + "' (layer TransparentFX, Main Camera only)");
        }
        Undo.RecordObject(go, "Configure Volumetric Fog Volume");
        go.layer = FogVolumeLayer;

        // Unity "fake null" breaks ??, so compare explicitly.
        var volume = go.GetComponent<Volume>();
        if (volume == null) volume = Undo.AddComponent<Volume>(go);
        Undo.RecordObject(volume, "Configure Volumetric Fog Volume");
        volume.isGlobal      = true;
        volume.priority      = 10f;
        volume.sharedProfile = profile;
        return volume;
    }

    private static void ConfigureCameraVolumeMasks(Camera mainCam, List<string> log)
    {
        int bit = 1 << FogVolumeLayer;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) continue;
            Undo.RecordObject(data, "Configure camera volume mask");

            if (cam == mainCam)
            {
                data.volumeLayerMask      = data.volumeLayerMask.value | bit;
                data.renderPostProcessing = true;   // fog pass is skipped when post-processing is off
                log.Add($"'{cam.name}' renders the fog (post-processing on)");
            }
            else if ((data.volumeLayerMask & bit) != 0)
            {
                data.volumeLayerMask = data.volumeLayerMask.value & ~bit;
                log.Add($"'{cam.name}' excluded from fog volume (avoids double fog in the stack)");
            }
            EditorUtility.SetDirty(data);
        }
    }

    private static void SetupAtmosphere(Volume fogVolume, float groundY, float camDist, List<string> log)
    {
        var atmo = Object.FindFirstObjectByType<CombatAtmosphere>(FindObjectsInactive.Include);
        bool created = atmo == null;
        if (created)
        {
            var go = new GameObject(AtmosphereName);
            Undo.RegisterCreatedObjectUndo(go, "Create Combat Atmosphere");
            atmo = go.AddComponent<CombatAtmosphere>();

            atmo.hazeColor     = RenderSettings.fog ? RenderSettings.fogColor : atmo.hazeColor;
            atmo.hazeStart     = camDist + 15f;                     // well behind the gameplay plane
            atmo.hazeEnd       = Mathf.Max(atmo.hazeStart + 120f,
                                  RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear
                                      ? RenderSettings.fogEndDistance + 60f : 260f);
            atmo.strength      = 0.5f;   // volumetric fog already adds depth; haze only separates layers
            atmo.heightBase    = groundY;
            atmo.heightFalloff = 0.02f;
        }

        Undo.RecordObject(atmo, "Configure Combat Atmosphere");
        atmo.fogVolume = fogVolume;
        if (atmo.sun == null) atmo.sun = FindSun();
        EditorUtility.SetDirty(atmo);
        log.Add(created ? "Created 'Combat Atmosphere' (global haze for CombatToon shaders)"
                        : "Linked existing CombatAtmosphere to the fog volume");
    }

    // -----------------------------------------------------------------------
    // 2. Environment materials
    // -----------------------------------------------------------------------
    [MenuItem(MenuRoot + "2. Convert Environment Materials (Toon City + CelShadeBuilding)", priority = 2)]
    private static void ConvertEnvironmentMaterials()
    {
        var shader = GetValidShader(EnvShaderName);
        if (shader == null) return;

        var done = new HashSet<Material>();
        foreach (var mat in SceneMaterials(r => true))
        {
            if (done.Contains(mat) || mat.shader == null) continue;
            string sn = mat.shader.name;
            if (sn == "Shader Graphs/CelShadeBuilding" || sn == "Shader Graphs/ToonBuilding")
            {
                if (IsEditableMaterial(mat)) { ConvertCelShadeBuilding(mat, shader); done.Add(mat); }
                continue;
            }
            if (!sn.StartsWith("Toon/TC_CustomToon")) continue;
            if (!IsEditableMaterial(mat)) continue;

            bool hasOutline = sn.Contains("Outline");
            Texture albedo  = GetTex(mat, "_TextureSample", "_BaseMap", "_MainTex");
            Vector2 scale   = GetScale(mat, "_TextureSample");
            Vector2 offset  = GetOffset(mat, "_TextureSample");
            Texture ramp    = GetTex(mat, "_TextureRamp");
            float  oWidth   = mat.HasProperty("_OutlineWidth") ? mat.GetFloat("_OutlineWidth") : 0.0065f;
            Color  oColor   = mat.HasProperty("_OutlineColor") ? mat.GetColor("_OutlineColor") : Color.black;

            Undo.RecordObject(mat, "Convert to CombatToon/Environment");
            mat.shader = shader;
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", scale);
            mat.SetTextureOffset("_BaseMap", offset);
            SetRamp(mat, ramp);

            mat.SetColor("_OutlineColor", new Color(oColor.r, oColor.g, oColor.b, 1f));
            mat.SetFloat("_OutlineWidth", hasOutline ? oWidth : 0f);
            mat.SetShaderPassEnabled(OutlinePass, hasOutline);

            EditorUtility.SetDirty(mat);
            done.Add(mat);
        }

        AssetDatabase.SaveAssets();
        Report($"Converted {done.Count} environment material(s) to {EnvShaderName}.", done);
    }

    // CelShadeBuilding = posterised N·L baked into URP Lit BaseColor (then lit again by PBR).
    // Map it to the same two-tone model as Toon City so the whole level shares one light response.
    private static void ConvertCelShadeBuilding(Material mat, Shader shader)
    {
        Texture albedo = GetTex(mat, "_MainTex", "_BaseMap");
        string  stProp = mat.HasProperty("_MainTex") ? "_MainTex" : "_BaseMap";
        Vector2 scale  = GetScale(mat, stProp);
        Vector2 offset = GetOffset(mat, stProp);
        Color   tint   = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        if (tint.maxColorComponent < 0.01f) tint = Color.white;          // graph default is black
        float   minB   = mat.HasProperty("_MinBright") ? mat.GetFloat("_MinBright") : 0.3f;
        float   maxB   = mat.HasProperty("_MaxBright") ? mat.GetFloat("_MaxBright") : 1f;

        Undo.RecordObject(mat, "Convert to CombatToon/Environment");
        mat.shader = shader;
        mat.SetTexture("_BaseMap", albedo);
        mat.SetTextureScale("_BaseMap", scale);
        mat.SetTextureOffset("_BaseMap", offset);
        // Normalise so the lit side keeps the old brightness ratio without blowing out.
        mat.SetColor("_BaseColor", tint);
        float shade = Mathf.Clamp01(minB / Mathf.Max(maxB, 0.01f) + 0.35f);
        mat.SetColor("_ShadowColor", new Color(shade * 0.92f, shade * 0.9f, shade * 1.1f, 1f));
        SetRamp(mat, null);
        mat.SetFloat("_OutlineWidth", 0f);
        mat.SetShaderPassEnabled(OutlinePass, false);
        EditorUtility.SetDirty(mat);
    }

    // -----------------------------------------------------------------------
    // 3. Enemy materials
    // -----------------------------------------------------------------------
    [MenuItem(MenuRoot + "3. Convert Enemy Materials", priority = 3)]
    private static void ConvertEnemyMaterials()
    {
        var shader = GetValidShader(CharShaderName);
        if (shader == null) return;

        var done = new HashSet<Material>();
        foreach (var mat in SceneMaterials(IsEnemyRenderer))
        {
            if (done.Contains(mat) || mat.shader == null) continue;
            if (!IsConvertibleCharacterShader(mat.shader.name)) continue;
            if (!IsEditableMaterial(mat)) continue;

            CopyToCharacter(mat, mat, shader);
            done.Add(mat);
        }

        AssetDatabase.SaveAssets();
        Report($"Converted {done.Count} enemy material(s) to {CharShaderName}.", done);
    }

    // -----------------------------------------------------------------------
    // 4. Player (optional — keeps the ASP originals untouched)
    // -----------------------------------------------------------------------
    [MenuItem(MenuRoot + "4. Characters (player + enemies): use CombatToon Character copies", priority = 4)]
    private static void ConvertPlayerMaterials()
    {
        var shader = GetValidShader(CharShaderName);
        if (shader == null) return;

        // Player plus every enemy root in the scene: they all share the same lighting model
        // as the environment (real sun colour + shadows + backlit rim) instead of ASP's.
        var roots = new List<Transform>();
        Transform player = FindPlayer();
        if (player != null) roots.Add(player);
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (mb != null && mb.GetType().Name == "EnemyHealth") roots.Add(mb.transform);
        if (roots.Count == 0) { Dialog("Combat Toon", "No player or enemies found in the open scene.", "OK"); return; }

        if (!Dialog("Combat Toon",
                "This creates *_CT.mat copies of the ASP Character materials used by the player and enemies and " +
                "assigns them in this scene only (prefab overrides). ASP materials are not modified; ASP-specific " +
                "features (SDF face shadow, eye shader) are not carried over.\n\nContinue?", "Continue", "Cancel"))
            return;

        var copies = new Dictionary<Material, Material>();
        foreach (var r in roots.SelectMany(t => t.GetComponentsInChildren<Renderer>(true)))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                if (src == null || src.shader == null || src.shader.name != "ASP/Character") continue;

                if (!copies.TryGetValue(src, out var copy))
                {
                    string srcPath = AssetDatabase.GetAssetPath(src);
                    string dir     = string.IsNullOrEmpty(srcPath) ? Root : Path.GetDirectoryName(srcPath).Replace('\\', '/');
                    string path    = $"{dir}/{src.name}_CT.mat";
                    copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (copy == null)
                    {
                        copy = new Material(shader) { name = src.name + "_CT" };
                        AssetDatabase.CreateAsset(copy, path);
                    }
                    CopyToCharacter(src, copy, shader);
                    copies[src] = copy;
                }
                mats[i] = copy;
                changed = true;
            }
            if (changed)
            {
                Undo.RecordObject(r, "Assign CombatToon player materials");
                r.sharedMaterials = mats;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Report($"Assigned {copies.Count} CombatToon copy material(s) to the player and enemies. Revert the prefab overrides to go back to ASP.",
               new HashSet<Material>(copies.Values));
    }

    // -----------------------------------------------------------------------
    // 5. Optional sun flip for backlit god rays
    // -----------------------------------------------------------------------
    [MenuItem(MenuRoot + "5. (Optional) Mirror Sun Toward Camera (backlit god rays)", priority = 20)]
    private static void MirrorSun()
    {
        Light sun = FindSun();
        if (sun == null) { Dialog("Combat Toon", "No directional light found.", "OK"); return; }

        // Mirror the travel direction on Z. Elevation and side angle are kept, so running
        // it again restores the original orientation.
        Vector3 f = sun.transform.forward;
        Vector3 mirrored = new Vector3(f.x, f.y, -f.z);
        Undo.RecordObject(sun.transform, "Mirror sun");
        sun.transform.rotation = Quaternion.LookRotation(mirrored, Vector3.up);
        EditorSceneManager.MarkSceneDirty(sun.gameObject.scene);
        Debug.Log($"[Combat Toon] Sun forward {f} -> {mirrored}. " +
                  (mirrored.z < 0 ? "Light now travels toward the camera: shafts + rim light become visible."
                                  : "Light travels away from the camera again (original setup)."));
    }

    // -----------------------------------------------------------------------
    // 6. God rays
    // -----------------------------------------------------------------------
    private const string GodRayRootName  = "God Rays";
    private const string GodRayCookiePath = Root + "/Textures/CT_GodRayCookie.png";

    [MenuItem(MenuRoot + "6. Add God Ray Lights (around player)", priority = 21)]
    private static void AddGodRays()
    {
#if !CT_VOLUMETRIC_FOG
        Dialog("Combat Toon", "Volumetric fog package missing.", "OK");
#else
        Transform player = FindPlayer();
        Camera cam = FindMainCamera();
        if (player == null || cam == null) { Dialog("Combat Toon", "Need a player and Main Camera in the open scene.", "OK"); return; }

        var log = new List<string>();
        EnableLightCookies(log);
        Texture2D cookie = GetOrCreateGodRayCookie(log);

        GameObject root = GameObject.Find(GodRayRootName);
        if (root != null) Undo.DestroyObjectImmediate(root);
        root = new GameObject(GodRayRootName);
        Undo.RegisterCreatedObjectUndo(root, "Add God Rays");

        // The fog is a low layer (dense below the roofs, thin above), so beams are aimed to
        // plunge INTO the fog sea where it is dense: that's where in-scattering is visible.
        // One beam lands on the roof around the player as a lit pool.
        // Direction follows the backlit sun sideways and slants down toward the camera.
        // It must be clearly diagonal: a near-vertical wide cone reads as a flat white wall,
        // not as rays. Several narrow-ish cones + the striped cookie give crepuscular streaks.
        Light sun = FindSun();
        float side = sun != null ? Mathf.Sign(sun.transform.forward.x) : -1f;
        Vector3 dir = new Vector3(side * 0.55f, -0.70f, -0.45f).normalized;
        Vector3 p = player.position;
        // (dx, dy, angle, intensity): targets relative to the player's feet.
        // Sources sit 60 m up the beam so they're above the top of the frame (reads as
        // sunlight, not a searchlight). URP spot intensity falls off with 1/d², hence the
        // large values: ~14000 at 60 m is ~4 at the target.
        const float sourceDistance = 60f;
        const float k = (sourceDistance * sourceDistance) / (48f * 48f);   // tuned at 48 m
        Vector4[] beams =
        {
            new Vector4(-24f, -20f, 12f, 9000f * k),  // streaks into the fog sea
            new Vector4(-15f, -18f, 14f, 9000f * k),
            new Vector4( -7f, -16f,  9f, 7000f * k),
            new Vector4(  6f,   0f, 16f, 2200f * k),  // warm pool on the roof near the player
        };
        Color warm = sun != null ? Color.Lerp(sun.color, new Color(1f, 0.86f, 0.62f), 0.6f) : new Color(1f, 0.86f, 0.62f);

        for (int i = 0; i < beams.Length; i++)
        {
            var go = new GameObject($"God Ray {i + 1}");
            go.transform.SetParent(root.transform, false);
            Vector3 target = new Vector3(p.x + beams[i].x, p.y + beams[i].y, p.z);
            go.transform.position = target - dir * sourceDistance;
            go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            var light = go.AddComponent<Light>();
            light.type      = LightType.Spot;
            light.color     = warm;
            light.intensity = beams[i].w;
            light.range     = sourceDistance * 2f;
            light.spotAngle = beams[i].z;
            light.innerSpotAngle = light.spotAngle * 0.55f;
            light.shadows   = LightShadows.None;   // additional-light shadows are off in this URP asset
            light.cookie    = cookie;
            light.renderMode = LightRenderMode.ForcePixel;

            go.AddComponent<UniversalAdditionalLightData>();
            var vol = go.AddComponent<VolumetricAdditionalLight>();
            var so = new SerializedObject(vol);
            so.FindProperty("anisotropy").floatValue = 0.55f;   // forward-scatter toward camera
            so.FindProperty("scattering").floatValue = 2.5f;    // beams pop over the sun haze
            so.FindProperty("radius").floatValue     = 0.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        log.Add($"Created '{GodRayRootName}' with {beams.Length} cookie spot lights");

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[Combat Toon] God rays:\n- " + string.Join("\n- ", log));
        Dialog("Combat Toon", "God rays added.\n\n- " + string.Join("\n- ", log), "OK");
#endif
    }

    private static void EnableLightCookies(List<string> log)
    {
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null) return;
        var so = new SerializedObject(asset);
        var prop = so.FindProperty("m_SupportsLightCookies");
        if (prop != null && !prop.boolValue)
        {
            prop.boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            log.Add("Enabled Light Cookies on " + asset.name);
        }
    }

    // Procedural "light through gaps" cookie: soft vertical bands of varying width,
    // which split one spot cone into several shafts inside the fog.
    private static Texture2D GetOrCreateGodRayCookie(List<string> log)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GodRayCookiePath);
        if (existing != null) return existing;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        var rng = new System.Random(7);
        float[] bands = new float[size];
        float x = 0f;
        while (x < size)
        {
            float w   = 6f + (float)rng.NextDouble() * 22f;
            float on  = rng.NextDouble() < 0.55 ? 1f : 0.08f;
            for (int i = (int)x; i < Mathf.Min(size, x + w); i++) bands[i] = on;
            x += w;
        }
        for (int py = 0; py < size; py++)
        for (int px = 0; px < size; px++)
        {
            // blur bands horizontally for soft shaft edges
            float b = 0f; int n = 0;
            for (int k = -4; k <= 4; k++) { int ix = Mathf.Clamp(px + k, 0, size - 1); b += bands[ix]; n++; }
            b /= n;
            Vector2 uv = new Vector2(px / (size - 1f) * 2f - 1f, py / (size - 1f) * 2f - 1f);
            float edge = Mathf.Clamp01(1f - uv.magnitude);           // round spot falloff
            float v = b * Mathf.SmoothStep(0f, 1f, edge * 1.6f);
            tex.SetPixel(px, py, new Color(v, v, v, v));
        }
        tex.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(GodRayCookiePath));
        File.WriteAllBytes(GodRayCookiePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(GodRayCookiePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(GodRayCookiePath);
        importer.textureType   = TextureImporterType.Cookie;
        importer.alphaSource   = TextureImporterAlphaSource.FromGrayScale;
        importer.wrapMode      = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.sRGBTexture   = false;
        importer.SaveAndReimport();

        log.Add("Created cookie " + GodRayCookiePath);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(GodRayCookiePath);
    }

    // -----------------------------------------------------------------------
    // 7. Performance
    // -----------------------------------------------------------------------
    private const string LiteRendererPath = Root + "/CT_LiteRenderer.asset";

    // Measured on Level_01 @1080p (GPU ms, cumulative): baseline 24.4 -> render scale 1.0 12.3
    // -> SSAO off 8.9 -> fog off 4.8. Every overlay camera also ran all 10 renderer features
    // and its own 4096 shadow map.
    [MenuItem(MenuRoot + "7. Apply Performance Settings", priority = 30)]
    private static void ApplyPerformanceSettings()
    {
        var log = new List<string>();
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var main  = GetDefaultRendererData();
        if (asset == null || main == null) { Dialog("Combat Toon", "URP asset / renderer not found.", "OK"); return; }

        // 1. Native resolution (1.5 = 2.25x pixels). Edges are handled by SMAA below.
        Undo.RecordObject(asset, "Combat Toon perf");
        if (asset.renderScale > 1f) { log.Add($"Render scale {asset.renderScale} -> 1.0"); asset.renderScale = 1f; }
        EditorUtility.SetDirty(asset);

        // 2. Cheaper SSAO: half resolution, medium samples / blur.
        foreach (var f in main.rendererFeatures)
        {
            if (f == null || f.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
            var so = new SerializedObject(f);
            SetIfExists(so, "m_Settings.Downsample", p => p.boolValue = true);
            SetIfExists(so, "m_Settings.BlurQuality", p => p.enumValueIndex = 1);
            SetIfExists(so, "m_Settings.Samples", p => p.enumValueIndex = 1);      // AOSampleOption.Medium
            so.ApplyModifiedProperties();
            log.Add("SSAO: half resolution, medium samples + blur");
        }

        // 3. Lite renderer for cameras that only draw sky / VFX.
        int liteIndex = EnsureLiteRenderer(asset, main, log);

        // 4. Cameras: one shadow map, AA on the base camera.
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) continue;
            Undo.RecordObject(data, "Combat Toon perf");
            bool isMain = cam == FindMainCamera();
            if (!isMain)
            {
                data.renderShadows = false;
                if (liteIndex >= 0) data.SetRenderer(liteIndex);
                log.Add($"'{cam.name}': no shadow map, lite renderer");
            }
            if (data.renderType == CameraRenderType.Base)
            {
                data.antialiasing        = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.Medium;
                log.Add($"'{cam.name}': SMAA (replaces render-scale supersampling)");
            }
            EditorUtility.SetDirty(data);
        }

#if CT_VOLUMETRIC_FOG
        // 5. Fog raymarch steps.
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FogProfilePath);
        if (profile != null && profile.TryGet(out VolumetricFogVolumeComponent fog))
        {
            Undo.RecordObject(fog, "Combat Toon perf");
            fog.maxSteps.Override(Mathf.Min(fog.maxSteps.value, 48));
            EditorUtility.SetDirty(fog);
            log.Add("Fog max steps -> " + fog.maxSteps.value);
        }
#endif
        main.SetDirty();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Combat Toon] Performance:\n- " + string.Join("\n- ", log));
        Dialog("Combat Toon", "Performance settings applied.\n\n- " + string.Join("\n- ", log), "OK");
    }

    // -----------------------------------------------------------------------
    // 8. Golden hour look (reference: CristianQiu Towers.gif)
    // -----------------------------------------------------------------------
    private const string PostProfilePath = Root + "/Level01_PostProfile.asset";
    private const string OccluderRootName = "Light Occluders";

    // Findings that drove these values:
    //  - ScreenSpaceShadows makes the fog sample a screen-space texture with world coords,
    //    so volumetric shadows (shafts) never appear -> the feature is disabled.
    //  - SampleSceneProfile ran two tonemappers (URP ACES + ASP KhronosNeutral x1.5 exposure)
    //    and Bloom with threshold 0 / intensity 5, which washed the whole frame.
    //  - Shafts need an occluder with gaps between sun and camera inside dense fog: a backlit
    //    billboard behind the gameplay plane gives a visible source for the light/shadow volumes.
    [MenuItem(MenuRoot + "8. Apply Golden Hour Look (sun, fog, post, occluder)", priority = 31)]
    private static void ApplyGoldenHourLook()
    {
#if !CT_VOLUMETRIC_FOG
        Dialog("Combat Toon", "Volumetric fog package missing.", "OK");
#else
        var log = new List<string>();
        Transform player = FindPlayer();
        float g = player != null ? player.position.y : 25.34f;
        float px = player != null ? player.position.x : -130.6f;

        // Renderer: screen-space shadows off (required for volumetric shadows).
        var main = GetDefaultRendererData();
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (main != null)
            foreach (var f in main.rendererFeatures)
                if (f != null && f.GetType().Name == "ScreenSpaceShadows" && f.isActive)
                { f.SetActive(false); main.SetDirty(); EditorUtility.SetDirty(main); log.Add("Screen Space Shadows off (fog needs cascaded shadows)"); }
        if (asset != null)
        {
            Undo.RecordObject(asset, "Golden hour");
            asset.shadowDistance = 60f; asset.shadowCascadeCount = 2; asset.cascade2Split = 0.35f;
            EditorUtility.SetDirty(asset);
            log.Add("Shadows: 60 m, 2 cascades (split 0.35)");
        }

        // Sun: warm, backlit from the side so shadow volumes cross the frame diagonally.
        Light sun = FindSun();
        if (sun != null)
        {
            Undo.RecordObject(sun.transform, "Golden hour"); Undo.RecordObject(sun, "Golden hour");
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-0.62f, -0.55f, -0.56f).normalized, Vector3.up);
            sun.color = new Color(1f, 0.8f, 0.6f);
            sun.intensity = 1.4f;
            log.Add("Sun: warm (1, .8, .6) x1.4, backlit diagonal");
        }

        // Fake spot god rays are replaced by real shadowed shafts.
        var gr = GameObject.Find(GodRayRootName);
        if (gr != null) { Undo.DestroyObjectImmediate(gr); log.Add("Removed spot-light 'God Rays'"); }

        // Occluder with gaps behind the gameplay plane.
        var occ = GameObject.Find(OccluderRootName);
        if (occ == null)
        {
            occ = new GameObject(OccluderRootName);
            Undo.RegisterCreatedObjectUndo(occ, "Golden hour");
            var prefab = FindPrefab("Billboard_2A");
            if (prefab != null)
            {
                var bb = (GameObject)PrefabUtility.InstantiatePrefab(prefab, occ.transform);
                bb.transform.position   = new Vector3(px + 9.6f, g, 9f);
                bb.transform.rotation   = Quaternion.Euler(0f, 90f, 0f);
                bb.transform.localScale = Vector3.one * 1.4f;
                log.Add("Added backlit Billboard_2A occluder behind the player");
            }
        }

        // Fog: dense around the roofs, thinning upward.
        var fogProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FogProfilePath);
        if (fogProfile != null && fogProfile.TryGet(out VolumetricFogVolumeComponent fog))
        {
            Undo.RecordObject(fog, "Golden hour");
            fog.renderPassEvent.Override(VolumetricFogRenderPassEvent.BeforeRenderingPostProcessing);
            fog.baseHeight.Override(g - 10f);
            fog.maximumHeight.Override(g + 30f);
            fog.density.Override(0.22f);
            fog.scattering.Override(0.35f);
            fog.anisotropy.Override(0.7f);
            fog.distance.Override(90f);
            fog.tint.Override(new Color(1f, 0.9f, 0.78f));
            fog.maxSteps.Override(48);
            fog.blurIterations.Override(1);
            EditorUtility.SetDirty(fog);
            log.Add("Fog: density .22 around roofs, warm tint, anisotropy .7");
        }

        // Post: own profile (SampleSceneProfile left untouched).
        var post = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
        if (post == null)
        {
            AssetDatabase.CopyAsset("Assets/Settings/SampleSceneProfile.asset", PostProfilePath);
            post = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
            log.Add("Created " + PostProfilePath + " (copy of SampleSceneProfile)");
        }
        if (post != null)
        {
            if (post.TryGet(out Tonemapping tm)) tm.active = false;   // ASPToneMap stays the single tonemapper
            foreach (var c in post.components)
            {
                if (c.GetType().Name != "ASPToneMap") continue;
                var exposure = c.parameters[1];
                exposure.overrideState = true;
                exposure.GetType().GetProperty("value").SetValue(exposure, 1.0f);
            }
            if (post.TryGet(out Bloom bloom))
            {
                bloom.threshold.Override(1.0f); bloom.intensity.Override(0.7f); bloom.scatter.Override(0.65f);
                bloom.tint.Override(new Color(1f, 0.92f, 0.8f)); bloom.clamp.Override(65000f);
            }
            GetOrAdd<ColorAdjustments>(post).contrast.Override(14f);
            GetOrAdd<ColorAdjustments>(post).saturation.Override(10f);
            GetOrAdd<WhiteBalance>(post).temperature.Override(4f);
            GetOrAdd<WhiteBalance>(post).tint.Override(2f);
            var vig = GetOrAdd<Vignette>(post);
            vig.intensity.Override(0.22f); vig.smoothness.Override(0.45f); vig.color.Override(new Color(0.12f, 0.06f, 0.08f));
            EditorUtility.SetDirty(post);

            var gvGo = GameObject.Find("Global Volume");
            if (gvGo != null && gvGo.TryGetComponent(out Volume gv))
            {
                Undo.RecordObject(gv, "Golden hour");
                gv.sharedProfile = post;
            }
            log.Add("Post: one tonemapper (ASP, exposure 1.0), bloom threshold 1 / 0.7, contrast +14, saturation +10, warm WB, vignette");
        }

        var atmo = Object.FindFirstObjectByType<CombatAtmosphere>();
        if (atmo != null) { Undo.RecordObject(atmo, "Golden hour"); atmo.hazeColor = new Color(0.95f, 0.82f, 0.7f); EditorUtility.SetDirty(atmo); }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Combat Toon] Golden hour:\n- " + string.Join("\n- ", log));
        Dialog("Combat Toon", "Golden hour look applied.\n\n- " + string.Join("\n- ", log), "OK");
#endif
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T comp)) return comp;
        comp = profile.Add<T>();
        AssetDatabase.AddObjectToAsset(comp, profile);
        return comp;
    }

    private static GameObject FindPrefab(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/" + name + ".prefab")) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    private static int EnsureLiteRenderer(UniversalRenderPipelineAsset asset, UniversalRendererData main, List<string> log)
    {
        var lite = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(LiteRendererPath);
        if (lite == null)
        {
            lite = ScriptableObject.CreateInstance<UniversalRendererData>();
            lite.postProcessData      = main.postProcessData;
            lite.renderingMode        = main.renderingMode;
            lite.depthPrimingMode     = DepthPrimingMode.Disabled;
            lite.copyDepthMode        = main.copyDepthMode;
            lite.shadowTransparentReceive = false;
            AssetDatabase.CreateAsset(lite, LiteRendererPath);
            log.Add("Created " + LiteRendererPath + " (no renderer features)");
        }

        var so   = new SerializedObject(asset);
        var list = so.FindProperty("m_RendererDataList");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == lite) return i;

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = lite;
        so.ApplyModifiedProperties();
        log.Add("Registered lite renderer at index " + (list.arraySize - 1));
        return list.arraySize - 1;
    }

    private static void SetIfExists(SerializedObject so, string path, System.Action<SerializedProperty> set)
    {
        var p = so.FindProperty(path);
        if (p != null) set(p);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    /// <summary>Returns the shader only if it exists and compiled without errors; logs every message otherwise.</summary>
    private static Shader GetValidShader(string name)
    {
        var shader = Shader.Find(name);
        if (shader == null)
        {
            Dialog("Combat Toon", name + " not found / not imported yet.", "OK");
            return null;
        }

        var messages = ShaderUtil.GetShaderMessages(shader);
        bool hasError = ShaderUtil.ShaderHasError(shader);
        foreach (var m in messages)
        {
            string line = $"[Combat Toon] {name}: {m.message} ({m.file}:{m.line}, {m.platform})";
            if (m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) { Debug.LogError(line, shader); hasError = true; }
            else Debug.LogWarning(line, shader);
        }

        if (hasError)
        {
            Dialog("Combat Toon",
                name + " has compile errors (see Console). No materials were changed.", "OK");
            return null;
        }
        return shader;
    }

    [MenuItem(MenuRoot + "Validate CombatToon Shaders", priority = 40)]
    private static void ValidateShaders()
    {
        bool ok = GetValidShader(EnvShaderName) != null & GetValidShader(CharShaderName) != null;
        if (ok) Dialog("Combat Toon", "Both CombatToon shaders compiled without errors.", "OK");
    }

    private static UniversalRendererData GetDefaultRendererData()
    {
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null) return null;
        var so    = new SerializedObject(asset);
        var list  = so.FindProperty("m_RendererDataList");
        int index = so.FindProperty("m_DefaultRendererIndex").intValue;
        if (list == null || index < 0 || index >= list.arraySize) return null;
        return list.GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData;
    }

    private static Transform FindPlayer()
    {
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            string n = mb.GetType().Name;
            if (n == "PlayerCombat" || n == "PlayerStateController" || n == "PlayerMovement") return mb.transform;
        }
        return null;
    }

    private static Camera FindMainCamera()
    {
        if (Camera.main != null) return Camera.main;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.name == "Main Camera") return c;
        return null;
    }

    private static Light FindSun()
    {
        if (RenderSettings.sun != null) return RenderSettings.sun;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l.type == LightType.Directional) return l;
        return null;
    }

    private static bool IsEnemyRenderer(Renderer r)
    {
        for (Transform t = r.transform; t != null; t = t.parent)
            if (t.GetComponent("EnemyHealth") != null || t.GetComponent("EnemyStateController") != null)
                return true;
        return false;
    }

    private static bool IsConvertibleCharacterShader(string name) =>
        name.StartsWith("Legacy Shaders/") || name.StartsWith("Mobile/") || name == "Standard" ||
        name == "Hidden/InternalErrorShader" ||
        name == "Universal Render Pipeline/Lit" || name == "Universal Render Pipeline/Simple Lit" ||
        name == "Universal Render Pipeline/Unlit";

    private static IEnumerable<Material> SceneMaterials(System.Func<Renderer, bool> filter)
    {
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (!filter(r)) continue;
            foreach (var m in r.sharedMaterials)
                if (m != null) yield return m;
        }
    }

    private static bool IsEditableMaterial(Material mat)
    {
        string path = AssetDatabase.GetAssetPath(mat);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".mat") || !path.StartsWith("Assets/"))
        {
            Debug.LogWarning($"[Combat Toon] Skipped '{mat.name}' ({(string.IsNullOrEmpty(path) ? "scene/runtime material" : path)}): " +
                             "not an editable .mat asset. Extract it from the model first.", mat);
            return false;
        }
        return true;
    }

    private static void CopyToCharacter(Material src, Material dst, Shader shader)
    {
        Texture albedo = GetTex(src, "_BaseMap", "_MainTex");
        Vector2 scale  = GetScale(src, src.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex");
        Vector2 offset = GetOffset(src, src.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex");
        Color   tint   = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                       : src.HasProperty("_Color")     ? src.GetColor("_Color") : Color.white;
        Texture emis   = GetTex(src, "_EmissionMap");
        Color   emisC  = src.HasProperty("_EmissionColor") ? src.GetColor("_EmissionColor") : Color.black;
        bool    useEm  = src.IsKeywordEnabled("_EMISSION") && emisC.maxColorComponent > 0.001f;
        bool    clip   = src.IsKeywordEnabled("_ALPHATEST_ON");
        float   cutoff = src.HasProperty("_Cutoff") ? src.GetFloat("_Cutoff") : 0.5f;
        Color   oColor = src.HasProperty("_OutlineColor") ? src.GetColor("_OutlineColor") : new Color(0.08f, 0.06f, 0.1f, 1f);

        Undo.RecordObject(dst, "Convert to CombatToon/Character");
        dst.shader = shader;
        dst.SetTexture("_BaseMap", albedo);
        dst.SetTextureScale("_BaseMap", scale);
        dst.SetTextureOffset("_BaseMap", offset);
        dst.SetColor("_BaseColor", tint);
        dst.SetFloat("_Cutoff", cutoff);
        SetKeyword(dst, "_ALPHATEST_ON", "_AlphaClip", clip);

        dst.SetTexture("_EmissionMap", emis);
        dst.SetColor("_EmissionColor", emisC);
        SetKeyword(dst, "_EMISSION", "_UseEmission", useEm);

        SetKeyword(dst, "_CT_SPECULAR", "_UseSpecular", true);
        dst.SetColor("_OutlineColor", new Color(oColor.r, oColor.g, oColor.b, 1f));
        dst.SetShaderPassEnabled(OutlinePass, true);
        EditorUtility.SetDirty(dst);
    }

    private static void SetRamp(Material mat, Texture ramp)
    {
        mat.SetTexture("_RampMap", ramp);
        SetKeyword(mat, "_RAMPMAP_ON", "_UseRamp", ramp != null);
    }

    private static void SetKeyword(Material mat, string keyword, string toggleProp, bool on)
    {
        mat.SetFloat(toggleProp, on ? 1f : 0f);
        if (on) mat.EnableKeyword(keyword); else mat.DisableKeyword(keyword);
    }

    private static Texture GetTex(Material m, params string[] names)
    {
        foreach (var n in names)
            if (m.HasProperty(n) && m.GetTexture(n) != null) return m.GetTexture(n);
        return null;
    }

    private static Vector2 GetScale(Material m, string n)  => m.HasProperty(n) ? m.GetTextureScale(n)  : Vector2.one;
    private static Vector2 GetOffset(Material m, string n) => m.HasProperty(n) ? m.GetTextureOffset(n) : Vector2.zero;

    private static void Report(string message, HashSet<Material> mats)
    {
        var names = new List<string>();
        foreach (var m in mats) names.Add(m.name);
        Debug.Log($"[Combat Toon] {message}\n{string.Join(", ", names)}");
        Dialog("Combat Toon", message, "OK");
    }
}
