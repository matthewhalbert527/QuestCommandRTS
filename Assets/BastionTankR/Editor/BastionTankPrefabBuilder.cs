using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BastionTankR.Editor
{
    internal static class BastionTankPrefabBuilder
    {
        private const string Root = "Assets/BastionTankR";
        private const string Meshes = Root + "/Meshes";
        private const string Textures = Root + "/Textures";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string PrefabPath = Prefabs + "/Bastion_TX9R.prefab";
        private const string RuntimePrefabs = "Assets/Resources/UnitModels";
        private const string RuntimePrefabPath = RuntimePrefabs + "/Bastion_TX9R.prefab";
        private const string MaterialPath = Materials + "/Bastion_Palette.mat";

        private static readonly string[] RequiredModels =
        {
            Meshes + "/Bastion_Hull.obj",
            Meshes + "/Bastion_LeftTrack.obj",
            Meshes + "/Bastion_RightTrack.obj",
            Meshes + "/Bastion_Turret.obj",
            Meshes + "/Bastion_Cannon.obj",
            Meshes + "/Bastion_Tank_LOD1.obj"
        };

        [InitializeOnLoadMethod]
        private static void QueueAutomaticBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null && AssetsReady())
                {
                    Build(false);
                }
            };
        }

        [MenuItem("Tools/Bastion TX-9R/Create or Rebuild Prefab")]
        private static void BuildFromMenu()
        {
            Build(true);
        }

        private static bool AssetsReady()
        {
            foreach (string path in RequiredModels)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    return false;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Palette.png") != null;
        }

        private static void Build(bool selectResult)
        {
            if (!AssetsReady())
            {
                Debug.LogWarning("Bastion TX-9R: model imports are not ready yet. Re-run Tools > Bastion TX-9R > Create or Rebuild Prefab after Unity finishes importing.");
                return;
            }

            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            EnsureFolder(RuntimePrefabs);
            Material material = CreateOrUpdateMaterial();

            GameObject root = new GameObject("Bastion_TX9R");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject hull = InstantiateModel("Bastion_Hull", RequiredModels[0], root.transform, new Vector3(0f, 1.00f, 0f), material);
            GameObject leftTrack = InstantiateModel("LeftTrack", RequiredModels[1], root.transform, new Vector3(-1.43f, 0.65f, 0f), material);
            GameObject rightTrack = InstantiateModel("RightTrack", RequiredModels[2], root.transform, new Vector3(1.43f, 0.65f, 0f), material);

            GameObject turretYaw = new GameObject("TurretYaw");
            turretYaw.transform.SetParent(root.transform, false);
            turretYaw.transform.localPosition = new Vector3(0f, 1.70f, -0.10f);
            InstantiateModel("Bastion_Turret", RequiredModels[3], turretYaw.transform, Vector3.zero, material);

            GameObject cannonPitch = new GameObject("CannonPitch");
            cannonPitch.transform.SetParent(turretYaw.transform, false);
            cannonPitch.transform.localPosition = new Vector3(0f, 0.12f, 0.82f);
            InstantiateModel("Bastion_Cannon", RequiredModels[4], cannonPitch.transform, Vector3.zero, material);

            GameObject lod1 = InstantiateModel("Bastion_LOD1", RequiredModels[5], root.transform, Vector3.zero, material);
            lod1.SetActive(true);

            ConfigureLODGroup(root, new[] { hull, leftTrack, rightTrack, turretYaw }, lod1);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.05f, -0.02f);
            collider.size = new Vector3(3.45f, 2.20f, 5.10f);

            BastionTankRController controller = root.AddComponent<BastionTankRController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("turretYaw").objectReferenceValue = turretYaw.transform;
            serializedController.FindProperty("cannonPitch").objectReferenceValue = cannonPitch.transform;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, RuntimePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (selectResult && prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log("Bastion TX-9R: created " + PrefabPath);
        }

        private static GameObject InstantiateModel(string name, string path, Transform parent, Vector3 localPosition, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                int materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                Material[] shared = new Material[materialCount];
                for (int i = 0; i < shared.Length; i++)
                {
                    shared[i] = material;
                }
                renderer.sharedMaterials = shared;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            }

            return instance;
        }

        private static void ConfigureLODGroup(GameObject root, IEnumerable<GameObject> lod0Roots, GameObject lod1Root)
        {
            List<Renderer> lod0Renderers = new List<Renderer>();
            foreach (GameObject item in lod0Roots)
            {
                lod0Renderers.AddRange(item.GetComponentsInChildren<Renderer>(true));
            }

            Renderer[] lod1Renderers = lod1Root.GetComponentsInChildren<Renderer>(true);
            LODGroup group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = false;
            LOD high = new LOD(0.18f, lod0Renderers.ToArray());
            high.fadeTransitionWidth = 0.08f;
            LOD low = new LOD(0.045f, lod1Renderers);
            low.fadeTransitionWidth = 0.10f;
            LOD culled = new LOD(0.008f, Array.Empty<Renderer>());
            group.SetLODs(new[] { high, low, culled });
            group.RecalculateBounds();
        }

        private static Material CreateOrUpdateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                throw new InvalidOperationException("Bastion TX-9R: neither URP/Lit nor Standard shader could be found.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = "Bastion_Palette" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Palette.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Emission.png");

            SetTextureIfPresent(material, "_BaseMap", palette);
            SetTextureIfPresent(material, "_MainTex", palette);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Metallic", 0.12f);
            SetFloatIfPresent(material, "_Smoothness", 0.34f);
            SetFloatIfPresent(material, "_Glossiness", 0.34f);

            if (emission != null)
            {
                SetTextureIfPresent(material, "_EmissionMap", emission);
                SetColorIfPresent(material, "_EmissionColor", Color.white * 1.25f);
                material.EnableKeyword("_EMISSION");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetTextureIfPresent(Material material, string property, Texture texture)
        {
            if (material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
