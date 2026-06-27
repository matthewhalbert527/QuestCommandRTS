using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BastionFabrication.Editor
{
    internal static class BastionFabricationHubPrefabBuilder
    {
        private const string Root = "Assets/BastionFabricationHub";
        private const string Meshes = Root + "/Meshes";
        private const string Textures = Root + "/Textures";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string LocalMaterialPath = Materials + "/Bastion_FabricationHub_Palette.mat";
        private const string SharedStructuresMaterialPath = "Assets/BastionStructures/Materials/Bastion_Structures_Palette.mat";
        private const string PrefabPath = Prefabs + "/Bastion_FabricationHub.prefab";

        private static readonly string[] RequiredModels =
        {
            Meshes + "/Bastion_FabricationHub_Base.obj",
            Meshes + "/Bastion_FabricationHub_AssemblyDoor.obj",
            Meshes + "/Bastion_FabricationHub_CraneYaw.obj",
            Meshes + "/Bastion_FabricationHub_CraneBoomPitch.obj",
            Meshes + "/Bastion_FabricationHub_CraneTrolley.obj",
            Meshes + "/Bastion_FabricationHub_BeaconSpin.obj",
            Meshes + "/Bastion_FabricationHub_LOD1.obj"
        };

        [InitializeOnLoadMethod]
        private static void QueueAutomaticBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetsReady() && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                    Build(false);
            };
        }

        [MenuItem("Tools/Bastion Fabrication Hub/Create or Rebuild Prefab")]
        private static void BuildFromMenu() => Build(true);

        private static bool AssetsReady()
        {
            foreach (string path in RequiredModels)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return false;
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_FabricationHub_Palette.png") != null;
        }

        private static void Build(bool selectPrefab)
        {
            if (!AssetsReady())
            {
                Debug.LogWarning("Bastion Fabrication Hub: models are still importing.");
                return;
            }

            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            Material material = CreateOrReuseMaterial();

            GameObject root = new GameObject("Bastion_FabricationHub");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_FabricationHub_Base.obj", Base.transform, material);
            GameObject AssemblyDoor = NewChild("AssemblyDoor", lod0.transform, new Vector3(0.00000f, 0.30000f, 2.18000f));
            InstantiateModel("AssemblyDoorModel", Meshes + "/Bastion_FabricationHub_AssemblyDoor.obj", AssemblyDoor.transform, material);
            GameObject CraneYaw = NewChild("CraneYaw", lod0.transform, new Vector3(3.95000f, 0.28000f, -2.64000f));
            InstantiateModel("CraneYawModel", Meshes + "/Bastion_FabricationHub_CraneYaw.obj", CraneYaw.transform, material);
            GameObject CraneBoomPitch = NewChild("CraneBoomPitch", CraneYaw.transform, new Vector3(0.00000f, 5.73000f, 0.00000f));
            CraneBoomPitch.transform.localRotation = Quaternion.Euler(-7.00000f, 0.00000f, 0.00000f);
            InstantiateModel("CraneBoomPitchModel", Meshes + "/Bastion_FabricationHub_CraneBoomPitch.obj", CraneBoomPitch.transform, material);
            GameObject CraneTrolley = NewChild("CraneTrolley", CraneBoomPitch.transform, new Vector3(0.00000f, 0.00000f, 2.72000f));
            InstantiateModel("CraneTrolleyModel", Meshes + "/Bastion_FabricationHub_CraneTrolley.obj", CraneTrolley.transform, material);
            GameObject BeaconSpin = NewChild("BeaconSpin", lod0.transform, new Vector3(-3.34000f, 5.86000f, -1.65000f));
            InstantiateModel("BeaconSpinModel", Meshes + "/Bastion_FabricationHub_BeaconSpin.obj", BeaconSpin.transform, material);
            GameObject BuildOrigin = NewChild("BuildOrigin", root.transform, new Vector3(0.00000f, 0.22000f, 4.12000f));
            GameObject RallyPoint = NewChild("RallyPoint", root.transform, new Vector3(0.00000f, 0.22000f, 7.25000f));
            GameObject VehicleExit = NewChild("VehicleExit", root.transform, new Vector3(0.00000f, 0.22000f, 4.65000f));
            GameObject BlueprintEmitter = NewChild("BlueprintEmitter", root.transform, new Vector3(-3.34000f, 4.40000f, -0.18000f));
            GameObject ConstructionFXSocket = NewChild("ConstructionFXSocket", CraneTrolley.transform, new Vector3(0.00000f, -3.34000f, 0.00000f));
            GameObject CraneTip = NewChild("CraneTip", CraneBoomPitch.transform, new Vector3(0.00000f, 0.00000f, 4.92000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_FabricationHub_LOD1.obj", lod1.transform, material);

            BastionFabricationHub metadata = root.AddComponent<BastionFabricationHub>();
            metadata.Configure("Bastion Fabrication Hub", new Vector2(12.00000f, 10.40000f),
                3200.00000f, 45, 16.00000f,
                BuildOrigin.transform, RallyPoint.transform, VehicleExit.transform,
                BlueprintEmitter.transform, ConstructionFXSocket.transform);

            BastionFabricationDoor door = root.AddComponent<BastionFabricationDoor>();
            door.Configure(AssemblyDoor.transform, new Vector3(0f, 2.72f, 0f), 3.1f, false);

            BastionFabricationCrane crane = root.AddComponent<BastionFabricationCrane>();
            crane.Configure(CraneYaw.transform, CraneBoomPitch.transform, CraneTrolley.transform,
                ConstructionFXSocket.transform, 45f, 25f, 2.2f, -18f, 18f, 0.25f, 4.25f);

            BastionFabricationSpin spin = BeaconSpin.AddComponent<BastionFabricationSpin>();
            spin.Configure(BeaconSpin.transform, Vector3.up, 38f, true);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 3.62500f, 0f);
            collider.size = new Vector3(12.00000f, 7.25000f, 10.40000f);
            ConfigureLODGroup(root, lod0, lod1);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (selectPrefab)
            {
                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PrefabPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            Debug.Log("Bastion Fabrication Hub prefab created at " + PrefabPath);
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

        private static GameObject InstantiateModel(string name, string path, Transform parent, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Missing model: " + path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return instance;
        }

        private static void ConfigureLODGroup(GameObject root, GameObject lod0, GameObject lod1)
        {
            LODGroup group = root.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(0.34f, lod0.GetComponentsInChildren<Renderer>(true)),
                new LOD(0.08f, lod1.GetComponentsInChildren<Renderer>(true)),
                new LOD(0.01f, Array.Empty<Renderer>())
            });
            group.fadeMode = LODFadeMode.None;
            group.RecalculateBounds();
        }

        private static Material CreateOrReuseMaterial()
        {
            Material shared = AssetDatabase.LoadAssetAtPath<Material>(SharedStructuresMaterialPath);
            if (shared != null) return shared;

            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_FabricationHub_Palette.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_FabricationHub_Emission.png");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(LocalMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = "Bastion_FabricationHub_Palette" };
                AssetDatabase.CreateAsset(material, LocalMaterialPath);
            }
            else material.shader = shader;

            SetTexture(material, "_BaseMap", palette);
            SetTexture(material, "_MainTex", palette);
            SetColor(material, "_BaseColor", Color.white);
            SetColor(material, "_Color", Color.white);
            SetFloat(material, "_Metallic", 0.12f);
            SetFloat(material, "_Smoothness", 0.26f);
            SetFloat(material, "_Glossiness", 0.26f);
            if (emission != null)
            {
                SetTexture(material, "_EmissionMap", emission);
                SetColor(material, "_EmissionColor", Color.white * 1.15f);
                material.EnableKeyword("_EMISSION");
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetTexture(Material m, string p, Texture v) { if (m.HasProperty(p)) m.SetTexture(p, v); }
        private static void SetColor(Material m, string p, Color v) { if (m.HasProperty(p)) m.SetColor(p, v); }
        private static void SetFloat(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
