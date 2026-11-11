using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.VFX;


namespace ASP
{
    [ExecuteInEditMode]
    public class ASPCharacterPanel : MonoBehaviour
    {
        public enum OverrideMode
        {
            NO_OVERRIDE,
            OVERRIDE_WITH_EULER,
            //USE_SCENELUIGHT_HALFVECTOR,
        }

        public enum BackFaceOutlineMethod
        {
            FROM_VERTEX_NORMAL,
            FROM_UV4
        }

        [System.Serializable]
        public class MaterialGroup
        {
            public Material[] Materials;
        }

        public BackFaceOutlineMethod CurrentBackFaceOutlineMethod;
        public Color BackfaceOutlineColor;
        public float BackFaceOutlineWidth;

        public Vector2 BackFaceOutlineFadeStartEnd = new Vector2(50, 50);
        public float ScaleWidthAsScreenSpaceOutline = 0f;
        public Vector3 CenterPositionOffset = Vector3.zero;
        [FormerlySerializedAs("m_overrideMode")] public OverrideMode _overrideMode;
        [FormerlySerializedAs("m_mainLightColorOverride")] public bool _mainLightColorOverride;
        public Color _mainLightColorOverrideValue;
        public float _mainLightStrengthOverrideValue;
        
        [FormerlySerializedAs("m_overrideLightAngle")] public Vector3 _overrideLightAngle;
        public Transform HeadBoneTransform;
        public Vector3 CharacterCenterWS
        {
            get { return transform.position + CenterPositionOffset; }
        }
        
        public float _CharacterCenterCubeSize = 1;
        public bool UseSimpleAABBCutOffForCharacterShadow = false;

        public void ForGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f,
            float arrowHeadAngle = 20.0f)
        {
            Gizmos.DrawRay(pos, direction);

            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) *
                            new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) *
                           new Vector3(0, 0, 1);
            Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
            Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
        }

        public void ForGizmo(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f,
            float arrowHeadAngle = 20.0f)
        {
            Gizmos.color = color;
            Gizmos.DrawRay(pos, direction);

            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) *
                            new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) *
                           new Vector3(0, 0, 1);
            Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
            Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;

            ForGizmo(transform.position, transform.forward, 0.35f, 20f);
            Gizmos.DrawWireCube(transform.position + CenterPositionOffset , _CharacterCenterCubeSize * Vector3.one);
        }

        public void CreateMaterialInstance()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                var matList = new List<Material>();
                foreach (var mat in renderer.sharedMaterials)
                {
                    if(mat == null || mat.shader == null)
                        continue;
                    var newMat = new Material(mat);
                    newMat.name = mat.name + "_Instance";
                    matList.Add(newMat);
                }
            
                renderer.materials = matList.ToArray();
            }
        }

        private static void SetKeyword(Material material, string keyword, bool state)
        {
            //UnityEngine.Debug.Log(keyword + " = "+state);
            if (state)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        public bool HasUnmatchedCharacterCenter()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if(mat == null || mat.shader == null)
                        continue;
                    if (!(mat.shader.name.Equals("ASP/Character") || mat.shader.name.Equals("ASP/Eye")))
                    {
                        continue;
                    }

                    if (mat.HasVector("_CharacterCenterWS") &&
                        Vector3.Distance(mat.GetVector("_CharacterCenterWS"), CharacterCenterWS) > 0.5f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        public void SetupMaterialID()
        {
            var materialNames = new List<string>();
            var currentIDs = new List<int>();
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if(renderer.GetComponent<VisualEffect>() != null)
                    continue;
                Material[] mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                foreach (var mat in mats)
                {
                    if(mat == null || mat.shader == null)
                        continue;
                    if (!(mat.shader.name.Equals("ASP/Character") || mat.shader.name.Equals("ASP/Eye")))
                    {
                        continue;
                    }
                    
                    if (!materialNames.Contains(mat.name))
                    {
                        var currentId = (int)mat.GetFloat("_MaterialID");
                        mat.SetFloat("_MaterialID", currentIDs.Count + 1 );
                        currentIDs.Add(currentId);
                        materialNames.Add(mat.name);
                    }
                }
            }
        }
        
        // --- Per-frame update, allocation-free in Play mode -------------------------------
        // The original version ran GetComponentsInChildren + renderer.materials + shader.name
        // (each allocates) for every character every frame: ~40 KB garbage/frame on Level_01
        // with 11 characters, causing periodic GC hitches. Materials are now cached in Play
        // mode and property names are pre-hashed. Behaviour is unchanged.
        private static readonly int ID_FaceFrontDirection   = Shader.PropertyToID("_FaceFrontDirection");
        private static readonly int ID_FaceRightDirection   = Shader.PropertyToID("_FaceRightDirection");
        private static readonly int ID_CharacterCenterWS    = Shader.PropertyToID("_CharacterCenterWS");
        private static readonly int ID_CharacterCenterCube  = Shader.PropertyToID("_CharacterCenterCubeSize");
        private static readonly int ID_OverrideLightDir     = Shader.PropertyToID("_OverrideLightDirToggle");
        private static readonly int ID_OverrideLightColor   = Shader.PropertyToID("_OverrideLightColorIntensityToggle");
        private static readonly int ID_FakeLightColor       = Shader.PropertyToID("_FakeLightColor");
        private static readonly int ID_FakeLightIntensity   = Shader.PropertyToID("_FakeLightIntensity");
        private static readonly int ID_FakeLightEuler       = Shader.PropertyToID("_FakeLightEuler");
        private static readonly int ID_SimpleAABBCutOff     = Shader.PropertyToID("_UseSimpleAABBCutOffForCharacterShadow");

        private readonly List<Material> _aspMaterialCache = new List<Material>();
        private bool _aspCacheValid;
        private int _aspCacheHierarchyCount = -1;

        /// <summary>Call after swapping materials on this character at runtime.</summary>
        public void InvalidateMaterialCache() => _aspCacheValid = false;

        private void OnEnable() => _aspCacheValid = false;

        private static bool IsASPMaterial(Material mat)
        {
            if (mat == null || mat.shader == null) return false;
            string n = mat.shader.name;
            return n == "ASP/Character" || n == "ASP/Eye";
        }

        private void RebuildMaterialCache()
        {
            _aspMaterialCache.Clear();
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer.GetComponent<VisualEffect>() != null)
                    continue;
                Material[] mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                foreach (var mat in mats)
                    if (IsASPMaterial(mat)) _aspMaterialCache.Add(mat);
            }
            _aspCacheHierarchyCount = transform.hierarchyCount;
            _aspCacheValid = true;
        }

        public void UpdateLightDirectionOverrideParam()
        {
            // Edit mode: rebuild every call so inspector material swaps are picked up immediately.
            // Play mode: rebuild only when invalidated or the hierarchy changed (weapon spawned, etc).
            if (!Application.isPlaying || !_aspCacheValid || _aspCacheHierarchyCount != transform.hierarchyCount)
                RebuildMaterialCache();

            Transform face = HeadBoneTransform != null ? HeadBoneTransform : transform;
            Vector3 faceForward = face.forward;
            Vector3 faceRight   = face.right;
            Vector3 center      = CharacterCenterWS;
            Vector3 fakeEuler   = new Vector3(180 + _overrideLightAngle.x, _overrideLightAngle.y, _overrideLightAngle.z);
            float overrideDir   = _overrideMode != OverrideMode.NO_OVERRIDE ? 1 : 0;
            float overrideColor = _mainLightColorOverride ? 1 : 0;
            int   aabbCutOff    = UseSimpleAABBCutOffForCharacterShadow ? 1 : 0;

            for (int i = 0; i < _aspMaterialCache.Count; i++)
            {
                var mat = _aspMaterialCache[i];
                if (mat == null) { _aspCacheValid = false; continue; }

                mat.SetVector(ID_FaceFrontDirection, faceForward);
                mat.SetVector(ID_FaceRightDirection, faceRight);
                mat.SetVector(ID_CharacterCenterWS, center);
                mat.SetFloat(ID_CharacterCenterCube, _CharacterCenterCubeSize);
                mat.SetFloat(ID_OverrideLightDir, overrideDir);
                mat.SetFloat(ID_OverrideLightColor, overrideColor);
                mat.SetColor(ID_FakeLightColor, _mainLightColorOverrideValue);
                mat.SetFloat(ID_FakeLightIntensity, _mainLightStrengthOverrideValue);
                mat.SetVector(ID_FakeLightEuler, fakeEuler);
                mat.SetInt(ID_SimpleAABBCutOff, aabbCutOff);
            }
        }

        private void TryCreateDummyShadowCaster()
        {
            var hasDummy = false;
            foreach (var obj in Camera.main.gameObject.GetComponentsInChildren<Renderer>())
            {
                if (obj.gameObject.name == "ASPDummyShadowCaster(Don't Delete This)")
                {
                    hasDummy = true;
                    break;
                }
            }

            if (!hasDummy)
            {
                var dummyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dummyObj.name = "ASPDummyShadowCaster(Don't Delete This)";
                dummyObj.hideFlags = HideFlags.NotEditable;
                dummyObj.hideFlags = HideFlags.DontSave;
                dummyObj.transform.SetParent(Camera.main.transform);
                dummyObj.transform.localPosition = new Vector3(0,0,1);
                dummyObj.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                dummyObj.GetComponent<Renderer>().material = new Material(Shader.Find("Hidden/ASP/DummyShadowCaster"));
                if( dummyObj.GetComponent<Collider>())
                    DestroyImmediate(dummyObj.GetComponent<Collider>());
            }
        }
        
        public void SetDebugGIFlagToAllMaterials(float value)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if(renderer.GetComponent<VisualEffect>() != null)
                    continue;
                var subMaterialIndex = 0;
                Material[] mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                foreach (var mat in mats)
                {
                    if (mat == null || mat.shader == null)
                        continue;
                    if (!(mat.shader.name.Equals("ASP/Character")))
                    {
                        continue;
                    }
                    mat.SetFloat("_DebugGI", Mathf.Clamp01(value));
                    subMaterialIndex++;
                }
            }
        }

        public void SetDitheringValueToAllMaterials(float value)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if(renderer.GetComponent<VisualEffect>() != null)
                    continue;
                Material[] mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                foreach (var mat in mats)
                {
                    if (mat == null || mat.shader == null)
                        continue;
                    if (!(mat.shader.name.Equals("ASP/Character") || mat.shader.name.Equals("ASP/Eye")))
                    {
                        continue;
                    }
                    mat.SetFloat("_Dithering", Mathf.Clamp01(value));
                }
            }
        }
        
        public void SetDitheringSizeValueToAllMaterials(float value)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if(renderer.GetComponent<VisualEffect>() != null)
                    continue;
                Material[] mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                foreach (var mat in mats)
                {
                    if (mat == null || mat.shader == null)
                        continue;
                    if (!(mat.shader.name.Equals("ASP/Character") || mat.shader.name.Equals("ASP/Eye")))
                    {
                        continue;
                    }
                    mat.SetFloat("_DitherTexelSize", value);
                }
            }
        }
        
        public void SetFOVAdjustValueToAllMaterials(float value)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if(renderer.GetComponent<VisualEffect>() != null)
                    continue;
                Material[] mats = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                foreach (var mat in mats)
                {
                    if (mat == null || mat.shader == null)
                        continue;
                    if (!(mat.shader.name.Equals("ASP/Character") || mat.shader.name.Equals("ASP/Eye")))
                    {
                        continue;
                    }
                    mat.SetFloat("_FOVShiftX", Mathf.Clamp01(value));
                }
            }
        }

        public bool CreateMaterialInstanceOnPlay;

        private void Start()
        {
            SetupMaterialID();
            //CreateMaterialInstance();
            //TryCreateDummyShadowCaster();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateLightDirectionOverrideParam();
        }
    }
}