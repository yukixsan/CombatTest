using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns the Blender-split city meshes (Assets/Environmets/Split/*/Meshes/*.fbx) into prefabs
/// that use CombatToon/Environment. One material per source texture, shared by every prefab.
/// Re-runnable: existing materials/prefabs are updated in place.
/// </summary>
public static class EnvironmentSplitSetup
{
    private const string SplitRoot   = "Assets/Environmets/Split";
    private const string MaterialDir = SplitRoot + "/Materials";
    private const string PrefabDir   = SplitRoot + "/Prefabs";
    private const string EnvShader   = "CombatToon/Environment";

    // Source texture folder per split set (textures are referenced, not copied).
    private static readonly Dictionary<string, string> TextureFolders = new Dictionary<string, string>
    {
        { "ruined_town", "Assets/Environmets/ruined_town" },
        { "CityApo",     "Assets/Environmets/CityApo" },
    };

    public static bool SuppressDialogs;

    [MenuItem("Tools/Combat Toon/9. Setup Split Environment Prefabs", priority = 40)]
    public static void Run()
    {
        var shader = Shader.Find(EnvShader);
        if (shader == null) { Notify(EnvShader + " not found."); return; }
        EnsureFolder(MaterialDir); EnsureFolder(PrefabDir);

        var log = new List<string>();
        int prefabs = 0;
        var materials = new Dictionary<string, Material>();

        foreach (var set in TextureFolders)
        {
            string meshDir = $"{SplitRoot}/{set.Key}/Meshes";
            if (!AssetDatabase.IsValidFolder(meshDir)) continue;
            string prefabSetDir = $"{PrefabDir}/{set.Key}";
            EnsureFolder(prefabSetDir);

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { meshDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                // Import settings: static scenery, no animation/rig, metres already baked.
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.isReadable = false;
                importer.generateSecondaryUV = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

                // Remap every source material (named after its texture stem) to a CombatToon one.
                // Names come from embedded materials on first import, and from the existing
                // remap table on re-runs (remapped models no longer expose embedded materials).
                var names = new HashSet<string>(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m => m.name));
                foreach (var kv in importer.GetExternalObjectMap())
                    if (kv.Key.type == typeof(Material)) names.Add(kv.Key.name);
                foreach (var name in names)
                {
                    if (!materials.TryGetValue(name, out var mat))
                    {
                        mat = GetOrCreateMaterial(name, set.Value, shader, log);
                        materials[name] = mat;
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
                }
                importer.SaveAndReimport();

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                        StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                }
                string prefabPath = $"{prefabSetDir}/{Path.GetFileNameWithoutExtension(path)}.prefab";
                PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
                Object.DestroyImmediate(inst);
                prefabs++;
            }
        }

        AssetDatabase.SaveAssets();
        log.Insert(0, $"{prefabs} prefabs, {materials.Count} materials in {SplitRoot}");
        Debug.Log("[Combat Toon] Split environment:\n- " + string.Join("\n- ", log));
        Notify("Split environment ready.\n\n- " + string.Join("\n- ", log.Take(12)));
    }

    private static Material GetOrCreateMaterial(string stem, string textureFolder, Shader shader, List<string> log)
    {
        string matPath = $"{MaterialDir}/{stem}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        bool created = mat == null;
        if (created) { mat = new Material(shader) { name = stem }; AssetDatabase.CreateAsset(mat, matPath); }
        else mat.shader = shader;

        Texture2D tex = FindTexture(stem, textureFolder);
        bool alpha = false;
        if (tex != null)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            alpha = ti != null && ti.DoesSourceTextureHaveAlpha() && HasRealTransparency(AssetDatabase.GetAssetPath(tex));
            if (ti != null && alpha && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
        }
        else log.Add($"Texture '{stem}' not found in {textureFolder}");

        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        // Background scenery: no outline pass, alpha-clip + double-sided for cut-out textures
        // (foliage, rebar, cut-out facades), haze on so distant blocks fade into the atmosphere.
        mat.SetFloat("_OutlineWidth", 0f);
        mat.SetShaderPassEnabled("SRPDefaultUnlit", false);
        mat.SetFloat("_AlphaClip", alpha ? 1f : 0f);
        if (alpha) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cutoff", 0.4f);
        mat.SetFloat("_Cull", alpha ? 0f : 2f);
        mat.SetFloat("_UseGlobalAtmosphere", 1f);
        mat.SetFloat("_HazeStrength", 1f);
        EditorUtility.SetDirty(mat);
        if (created) log.Add($"Material {stem}{(alpha ? " (alpha clip, double-sided)" : "")}");
        return mat;
    }

    // Many albedo PNGs carry an alpha channel that is fully opaque (ground, roads); alpha
    // testing those only costs performance. Read the file and require >1% cut-out pixels.
    private static bool HasRealTransparency(string assetPath)
    {
        var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tmp.LoadImage(File.ReadAllBytes(assetPath))) return false;
            var px = tmp.GetPixels32();
            int step = Mathf.Max(1, px.Length / 65536), cut = 0, n = 0;
            for (int i = 0; i < px.Length; i += step, n++) if (px[i].a < 128) cut++;
            return n > 0 && cut > n / 100;
        }
        finally { Object.DestroyImmediate(tmp); }
    }

    private static Texture2D FindTexture(string stem, string folder)
    {
        foreach (var guid in AssetDatabase.FindAssets(stem + " t:Texture2D", new[] { folder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == stem) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void Notify(string msg)
    {
        if (SuppressDialogs || Application.isBatchMode) Debug.Log("[Combat Toon] " + msg);
        else EditorUtility.DisplayDialog("Combat Toon", msg, "OK");
    }
}
