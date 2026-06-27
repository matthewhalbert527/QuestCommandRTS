using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BastionHeavyTank.Editor
{
    internal static class BastionHT77PrefabBuilder
    {
        private const string Root = "Assets/BastionHeavyTank";
        private const string Meshes = Root + "/Meshes";
        private const string Textures = Root + "/Textures";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string PrefabPath = Prefabs + "/Bastion_HT77_Mammoth.prefab";
        private const string RuntimePrefabs = "Assets/Resources/UnitModels";
        private const string RuntimePrefabPath = RuntimePrefabs + "/Bastion_HT77_Mammoth.prefab";
        private const string MaterialPath = Materials + "/Bastion_HT77_Palette.mat";

        private static readonly string[] RequiredModels =
        {
            Meshes + "/Bastion_HT77_Hull.obj",
            Meshes + "/Bastion_HT77_LeftTrack.obj",
            Meshes + "/Bastion_HT77_RightTrack.obj",
            Meshes + "/Bastion_HT77_Turret.obj",
            Meshes + "/Bastion_HT77_TwinCannons.obj",
            Meshes + "/Bastion_HT77_MissileBase.obj",
            Meshes + "/Bastion_HT77_MissilePod.obj",
            Meshes + "/Bastion_HT77_LOD1.obj"
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

        [MenuItem("Tools/Bastion HT-77 Mammoth/Create or Rebuild Prefab")]
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
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_HT77_Palette.png") != null;
        }

        private static void Build(bool selectResult)
        {
            if (!AssetsReady())
            {
                Debug.LogWarning("Bastion HT-77 Mammoth: model imports are not ready. Re-run the prefab command after Unity finishes importing.");
                return;
            }

            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            EnsureFolder(RuntimePrefabs);
            Material material = CreateOrUpdateMaterial();

            GameObject root = new GameObject("Bastion_HT77_Mammoth");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject hull = InstantiateModel("Hull", RequiredModels[0], root.transform, new Vector3(0f, 1.18f, 0f), material);
            GameObject leftTrack = InstantiateModel("LeftTrack", RequiredModels[1], root.transform, new Vector3(-1.94f, 0.84f, 0f), material);
            GameObject rightTrack = InstantiateModel("RightTrack", RequiredModels[2], root.transform, new Vector3(1.94f, 0.84f, 0f), material);

            GameObject turretYaw = NewChild("TurretYaw", root.transform, new Vector3(0f, 2.20f, -0.12f));
            GameObject turretModel = InstantiateModel("TurretModel", RequiredModels[3], turretYaw.transform, Vector3.zero, material);

            GameObject cannonPitch = NewChild("CannonPitch", turretYaw.transform, new Vector3(0f, 0.08f, 1.48f));
            GameObject cannonModel = InstantiateModel("TwinCannons", RequiredModels[4], cannonPitch.transform, Vector3.zero, material);
            Transform leftMuzzle = NewAnchor("LeftCannonMuzzle", cannonPitch.transform, new Vector3(-0.43f, 0f, 5.55f));
            Transform rightMuzzle = NewAnchor("RightCannonMuzzle", cannonPitch.transform, new Vector3(0.43f, 0f, 5.55f));

            GameObject missileYaw = NewChild("MissileYaw", turretYaw.transform, new Vector3(1.06f, 0.58f, -0.62f));
            GameObject missileBase = InstantiateModel("MissileBase", RequiredModels[5], missileYaw.transform, Vector3.zero, material);
            GameObject missilePitch = NewChild("MissilePitch", missileYaw.transform, new Vector3(0f, 0.62f, 0.05f));
            GameObject missilePod = InstantiateModel("MissilePod", RequiredModels[6], missilePitch.transform, Vector3.zero, material);

            Transform[] missileMuzzles = new Transform[8];
            float[] xs = { -0.57f, -0.19f, 0.19f, 0.57f };
            int muzzleIndex = 0;
            foreach (float y in new[] { -0.23f, 0.23f })
            {
                foreach (float x in xs)
                {
                    missileMuzzles[muzzleIndex] = NewAnchor($"MissileMuzzle_{muzzleIndex + 1:00}", missilePitch.transform, new Vector3(x, y, 0.98f));
                    muzzleIndex++;
                }
            }

            GameObject lod1 = InstantiateModel("LOD1", RequiredModels[7], root.transform, Vector3.zero, material);
            lod1.SetActive(true);

            ConfigureLODGroup(
                root,
                new[] { hull, leftTrack, rightTrack, turretModel, cannonModel, missileBase, missilePod },
                lod1);

            BoxCollider hullCollider = root.AddComponent<BoxCollider>();
            hullCollider.center = new Vector3(0f, 1.32f, 0.04f);
            hullCollider.size = new Vector3(4.75f, 2.70f, 7.25f);

            BastionHT77Controller controller = root.AddComponent<BastionHT77Controller>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("turretYaw").objectReferenceValue = turretYaw.transform;
            serialized.FindProperty("cannonPitch").objectReferenceValue = cannonPitch.transform;
            serialized.FindProperty("leftCannonMuzzle").objectReferenceValue = leftMuzzle;
            serialized.FindProperty("rightCannonMuzzle").objectReferenceValue = rightMuzzle;
            serialized.FindProperty("missileYaw").objectReferenceValue = missileYaw.transform;
            serialized.FindProperty("missilePitch").objectReferenceValue = missilePitch.transform;
            SerializedProperty muzzleArray = serialized.FindProperty("missileMuzzles");
            muzzleArray.arraySize = missileMuzzles.Length;
            for (int i = 0; i < missileMuzzles.Length; i++)
            {
                muzzleArray.GetArrayElementAtIndex(i).objectReferenceValue = missileMuzzles[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

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

            Debug.Log("Bastion HT-77 Mammoth: created " + PrefabPath);
        }

        private static GameObject NewChild(string name, Transform parent, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static Transform NewAnchor(string name, Transform parent, Vector3 localPosition)
        {
            return NewChild(name, parent, localPosition).transform;
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
            LOD culled = new LOD(0.007f, Array.Empty<Renderer>());
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
                throw new InvalidOperationException("Bastion HT-77 Mammoth: neither URP/Lit nor Standard shader could be found.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = "Bastion_HT77_Palette" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_HT77_Palette.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_HT77_Emission.png");

            SetTextureIfPresent(material, "_BaseMap", palette);
            SetTextureIfPresent(material, "_MainTex", palette);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Metallic", 0.18f);
            SetFloatIfPresent(material, "_Smoothness", 0.29f);
            SetFloatIfPresent(material, "_Glossiness", 0.29f);

            if (emission != null)
            {
                SetTextureIfPresent(material, "_EmissionMap", emission);
                SetColorIfPresent(material, "_EmissionColor", Color.white * 1.15f);
                material.EnableKeyword("_EMISSION");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetTextureIfPresent(Material material, string property, Texture texture)
        {
            if (material.HasProperty(property)) material.SetTexture(property, texture);
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property)) material.SetColor(property, value);
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
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
