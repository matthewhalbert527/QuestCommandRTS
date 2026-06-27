using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BastionMediumTank.Editor
{
    internal static class BastionMT12PrefabBuilder
    {
        private const string Root = "Assets/BastionMediumTank";
        private const string Meshes = Root + "/Meshes";
        private const string Textures = Root + "/Textures";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string PrefabPath = Prefabs + "/Bastion_MT12_Rider.prefab";
        private const string RuntimePrefabs = "Assets/Resources/UnitModels";
        private const string RuntimePrefabPath = RuntimePrefabs + "/Bastion_MT12_Rider.prefab";
        private const string MaterialPath = Materials + "/Bastion_MT12_Palette.mat";

        private static readonly string[] RequiredModels =
        {
            Meshes + "/Bastion_MT12_Hull.obj",
            Meshes + "/Bastion_MT12_LeftTrack.obj",
            Meshes + "/Bastion_MT12_RightTrack.obj",
            Meshes + "/Bastion_MT12_Turret.obj",
            Meshes + "/Bastion_MT12_Cannon.obj",
            Meshes + "/Bastion_MT12_MinigunBase.obj",
            Meshes + "/Bastion_MT12_MinigunBody.obj",
            Meshes + "/Bastion_MT12_MinigunBarrels.obj",
            Meshes + "/Bastion_MT12_LOD1.obj"
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

        [MenuItem("Tools/Bastion MT-12 Rider/Create or Rebuild Prefab")]
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
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_MT12_Palette.png") != null;
        }

        private static void Build(bool selectResult)
        {
            if (!AssetsReady())
            {
                Debug.LogWarning("Bastion MT-12 Rider: model imports are not ready. Re-run the prefab command after Unity finishes importing.");
                return;
            }

            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            EnsureFolder(RuntimePrefabs);
            Material material = CreateOrUpdateMaterial();

            GameObject root = new GameObject("Bastion_MT12_Rider");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject hull = InstantiateModel("Hull", RequiredModels[0], root.transform, new Vector3(0f, 1.06f, 0f), material);
            GameObject leftTrack = InstantiateModel("LeftTrack", RequiredModels[1], root.transform, new Vector3(-1.58f, 0.74f, 0f), material);
            GameObject rightTrack = InstantiateModel("RightTrack", RequiredModels[2], root.transform, new Vector3(1.58f, 0.74f, 0f), material);

            GameObject turretYaw = NewChild("TurretYaw", root.transform, new Vector3(0f, 1.82f, -0.08f));
            GameObject turretModel = InstantiateModel("TurretModel", RequiredModels[3], turretYaw.transform, Vector3.zero, material);

            GameObject cannonPitch = NewChild("CannonPitch", turretYaw.transform, new Vector3(0f, 0.16f, 1.00f));
            GameObject cannonModel = InstantiateModel("CannonModel", RequiredModels[4], cannonPitch.transform, Vector3.zero, material);
            Transform mainMuzzle = NewAnchor("MainMuzzle", cannonPitch.transform, new Vector3(0f, 0f, 4.74f));

            GameObject gunnerStationObject = NewChild("GunnerStation", turretYaw.transform, new Vector3(0.78f, 0.64f, -0.78f));
            Transform occupantAnchor = NewAnchor("OccupantAnchor", gunnerStationObject.transform, Vector3.zero);
            Transform leftFootAnchor = NewAnchor("LeftFootAnchor", gunnerStationObject.transform, new Vector3(-0.16f, 0.02f, -0.03f));
            Transform rightFootAnchor = NewAnchor("RightFootAnchor", gunnerStationObject.transform, new Vector3(0.16f, 0.02f, -0.03f));
            Transform leftHandGrip = NewAnchor("LeftHandGrip", gunnerStationObject.transform, new Vector3(-0.18f, 1.10f, 0.34f));
            Transform rightHandGrip = NewAnchor("RightHandGrip", gunnerStationObject.transform, new Vector3(0.18f, 1.10f, 0.34f));
            Transform headLookAnchor = NewAnchor("HeadLookAnchor", gunnerStationObject.transform, new Vector3(0f, 1.64f, 0.48f));

            GameObject gunnerYaw = NewChild("GunnerYaw", gunnerStationObject.transform, new Vector3(0f, 0.18f, 0.60f));
            GameObject minigunBase = InstantiateModel("MinigunBase", RequiredModels[5], gunnerYaw.transform, Vector3.zero, material);

            GameObject minigunPitch = NewChild("MinigunPitch", gunnerYaw.transform, new Vector3(0f, 0.88f, 0.14f));
            GameObject minigunBody = InstantiateModel("MinigunBody", RequiredModels[6], minigunPitch.transform, Vector3.zero, material);

            GameObject barrelSpin = NewChild("BarrelSpin", minigunPitch.transform, Vector3.zero);
            GameObject minigunBarrels = InstantiateModel("MinigunBarrels", RequiredModels[7], barrelSpin.transform, Vector3.zero, material);
            Transform minigunMuzzle = NewAnchor("MinigunMuzzle", barrelSpin.transform, new Vector3(0f, 0f, 1.84f));

            Transform boardingPoint = NewAnchor("GunnerBoardingPoint", root.transform, new Vector3(1.96f, 0.72f, -0.82f));
            boardingPoint.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Transform dismountPoint = NewAnchor("GunnerDismountPoint", root.transform, new Vector3(2.28f, 0.02f, -0.82f));
            dismountPoint.localRotation = Quaternion.Euler(0f, -90f, 0f);

            GameObject boardingZone = NewChild("GunnerBoardingZone", root.transform, new Vector3(1.96f, 1.06f, -0.82f));
            BoxCollider boardingTrigger = boardingZone.AddComponent<BoxCollider>();
            boardingTrigger.size = new Vector3(0.90f, 2.10f, 1.30f);
            boardingTrigger.isTrigger = true;

            GameObject lod1 = InstantiateModel("LOD1", RequiredModels[8], root.transform, Vector3.zero, material);
            lod1.SetActive(true);

            ConfigureLODGroup(
                root,
                new[] { hull, leftTrack, rightTrack, turretModel, cannonModel, minigunBase, minigunBody, minigunBarrels },
                lod1);

            BoxCollider hullCollider = root.AddComponent<BoxCollider>();
            hullCollider.center = new Vector3(0f, 1.10f, -0.02f);
            hullCollider.size = new Vector3(3.82f, 2.30f, 5.90f);

            BastionMT12Controller tankController = root.AddComponent<BastionMT12Controller>();
            SerializedObject tankSerialized = new SerializedObject(tankController);
            tankSerialized.FindProperty("turretYaw").objectReferenceValue = turretYaw.transform;
            tankSerialized.FindProperty("cannonPitch").objectReferenceValue = cannonPitch.transform;
            tankSerialized.FindProperty("mainMuzzle").objectReferenceValue = mainMuzzle;
            tankSerialized.ApplyModifiedPropertiesWithoutUndo();

            BastionGunnerStation station = gunnerStationObject.AddComponent<BastionGunnerStation>();
            SerializedObject stationSerialized = new SerializedObject(station);
            stationSerialized.FindProperty("gunnerYaw").objectReferenceValue = gunnerYaw.transform;
            stationSerialized.FindProperty("minigunPitch").objectReferenceValue = minigunPitch.transform;
            stationSerialized.FindProperty("barrelSpin").objectReferenceValue = barrelSpin.transform;
            stationSerialized.FindProperty("muzzle").objectReferenceValue = minigunMuzzle;
            stationSerialized.FindProperty("occupantAnchor").objectReferenceValue = occupantAnchor;
            stationSerialized.FindProperty("leftHandGrip").objectReferenceValue = leftHandGrip;
            stationSerialized.FindProperty("rightHandGrip").objectReferenceValue = rightHandGrip;
            stationSerialized.FindProperty("leftFootAnchor").objectReferenceValue = leftFootAnchor;
            stationSerialized.FindProperty("rightFootAnchor").objectReferenceValue = rightFootAnchor;
            stationSerialized.FindProperty("headLookAnchor").objectReferenceValue = headLookAnchor;
            stationSerialized.FindProperty("boardingPoint").objectReferenceValue = boardingPoint;
            stationSerialized.FindProperty("dismountPoint").objectReferenceValue = dismountPoint;
            stationSerialized.ApplyModifiedPropertiesWithoutUndo();

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

            Debug.Log("Bastion MT-12 Rider: created " + PrefabPath);
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
            LOD high = new LOD(0.20f, lod0Renderers.ToArray());
            high.fadeTransitionWidth = 0.08f;
            LOD low = new LOD(0.05f, lod1Renderers);
            low.fadeTransitionWidth = 0.10f;
            LOD culled = new LOD(0.009f, Array.Empty<Renderer>());
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
                throw new InvalidOperationException("Bastion MT-12 Rider: neither URP/Lit nor Standard shader could be found.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = "Bastion_MT12_Palette" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_MT12_Palette.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_MT12_Emission.png");

            SetTextureIfPresent(material, "_BaseMap", palette);
            SetTextureIfPresent(material, "_MainTex", palette);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Metallic", 0.16f);
            SetFloatIfPresent(material, "_Smoothness", 0.30f);
            SetFloatIfPresent(material, "_Glossiness", 0.30f);

            if (emission != null)
            {
                SetTextureIfPresent(material, "_EmissionMap", emission);
                SetColorIfPresent(material, "_EmissionColor", Color.white * 1.20f);
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
