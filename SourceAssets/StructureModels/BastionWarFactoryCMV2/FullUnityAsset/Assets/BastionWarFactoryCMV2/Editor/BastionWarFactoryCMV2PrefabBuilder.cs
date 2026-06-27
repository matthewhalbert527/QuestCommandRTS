
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Rendering;

namespace BastionWarFactoryCMV2.Editor
{
    public static class BastionWarFactoryCMV2PrefabBuilder
    {
        private const string Root = "Assets/BastionWarFactoryCMV2";
        private const string Meshes = Root + "/Meshes";
        private const string Prefabs = Root + "/Prefabs";
        private const string Materials = Root + "/Materials";
        private const string Textures = Root + "/Textures";
        private const string Slug = "Bastion_WarFactoryCMV2";

        [DidReloadScripts]
        private static void AutoBuildAfterCompile()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Bastion_WarFactoryCMV2.prefab") == null)
                BuildPrefab(false);
        }

        [MenuItem("Tools/Bastion/Build War Factory CMV2 Prefab")]
        public static void BuildFromMenu() => BuildPrefab(true);

        public static void BuildPrefab(bool selectPrefab)
        {
            EnsureFolder(Prefabs);
            EnsureFolder(Materials);
            Material baseMat = CreateBaseMaterial();
            Material teamMat = CreateTeamMaterial();
            Material emissiveMat = CreateEmissiveMaterial();
            Material glassMat = CreateGlassMaterial();
            Material smokeMat = CreateSmokeMaterial();

            GameObject root = new GameObject("Bastion_WarFactoryCMV2");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject staticRoot = NewChild("StaticRenderers", lod0.transform, Vector3.zero);
            GameObject activeRoot = NewChild("ActivePieces", lod0.transform, Vector3.zero);

            GameObject baseArmor = InstantiateModel("BaseArmor", Meshes + "/" + Slug + "_BaseArmor.obj", staticRoot.transform, Vector3.zero, baseMat);
            GameObject team = InstantiateModel("TeamColorPanels", Meshes + "/" + Slug + "_TeamColorPanels.obj", staticRoot.transform, Vector3.zero, teamMat);
            GameObject emiss = InstantiateModel("EmissiveParts", Meshes + "/" + Slug + "_Emissive.obj", staticRoot.transform, Vector3.zero, emissiveMat);
            GameObject glass = InstantiateModel("GlassLights", Meshes + "/" + Slug + "_GlassLights.obj", staticRoot.transform, Vector3.zero, glassMat);
            GameObject smokeFx = InstantiateModel("SmokeStackFXMarkers", Meshes + "/" + Slug + "_SmokeStackFX.obj", staticRoot.transform, Vector3.zero, smokeMat);

            GameObject door = InstantiateModel("AssemblyDoor", Meshes + "/" + Slug + "_AssemblyDoor.obj", activeRoot.transform, new Vector3(0f, 1.02f, -4.86f), baseMat);
            GameObject conveyor = InstantiateModel("AssemblyConveyor", Meshes + "/" + Slug + "_Conveyor.obj", activeRoot.transform, new Vector3(0f, 0.78f, -0.58f), baseMat);
            GameObject trolley = InstantiateModel("GantryTrolley", Meshes + "/" + Slug + "_GantryTrolley.obj", activeRoot.transform, new Vector3(0f, 3.92f, 0.80f), baseMat);

            GameObject fanA = InstantiateModel("RoofFan_A", Meshes + "/" + Slug + "_RoofFan.obj", activeRoot.transform, new Vector3(-1.95f, 5.88f, 3.05f), baseMat);
            GameObject fanB = InstantiateModel("RoofFan_B", Meshes + "/" + Slug + "_RoofFan.obj", activeRoot.transform, new Vector3(1.95f, 5.88f, 3.05f), baseMat);
            GameObject fanC = InstantiateModel("RoofFan_C", Meshes + "/" + Slug + "_RoofFan.obj", activeRoot.transform, new Vector3(0.0f, 5.88f, -2.35f), baseMat);

            GameObject armA = InstantiateModel("RobotArm_FrontLeft", Meshes + "/" + Slug + "_RobotArmA.obj", activeRoot.transform, new Vector3(-2.25f, 0.83f, -1.45f), baseMat);
            armA.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            GameObject armB = InstantiateModel("RobotArm_FrontRight", Meshes + "/" + Slug + "_RobotArmB.obj", activeRoot.transform, new Vector3(2.25f, 0.83f, -1.45f), baseMat);
            armB.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
            GameObject armC = InstantiateModel("RobotArm_RearLeft", Meshes + "/" + Slug + "_RobotArmA.obj", activeRoot.transform, new Vector3(-2.15f, 0.83f, 1.25f), baseMat);
            armC.transform.localRotation = Quaternion.Euler(0f, 5f, 0f);
            GameObject armD = InstantiateModel("RobotArm_RearRight", Meshes + "/" + Slug + "_RobotArmB.obj", activeRoot.transform, new Vector3(2.15f, 0.83f, 1.25f), baseMat);
            armD.transform.localRotation = Quaternion.Euler(0f, -5f, 0f);

            GameObject beaconA = InstantiateModel("Beacon_FrontLeft", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(-7.25f, 3.92f, -5.40f), emissiveMat);
            GameObject beaconB = InstantiateModel("Beacon_RoofLeft", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(-4.75f, 6.62f, -3.25f), emissiveMat);
            GameObject beaconC = InstantiateModel("Beacon_RoofRight", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(4.85f, 6.37f, -3.00f), emissiveMat);
            GameObject beaconD = InstantiateModel("Beacon_Stack", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(5.50f, 7.05f, 4.00f), emissiveMat);

            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1_BaseArmor", Meshes + "/" + Slug + "_LOD1_BaseArmor.obj", lod1.transform, Vector3.zero, baseMat);
            GameObject lod1Team = InstantiateModel("LOD1_TeamColorPanels", Meshes + "/" + Slug + "_LOD1_TeamColorPanels.obj", lod1.transform, Vector3.zero, teamMat);
            InstantiateModel("LOD1_Emissive", Meshes + "/" + Slug + "_LOD1_Emissive.obj", lod1.transform, Vector3.zero, emissiveMat);
            InstantiateModel("LOD1_GlassLights", Meshes + "/" + Slug + "_LOD1_GlassLights.obj", lod1.transform, Vector3.zero, glassMat);

            GameObject lod2 = NewChild("LOD2", root.transform, Vector3.zero);
            InstantiateModel("LOD2_BaseArmor", Meshes + "/" + Slug + "_LOD2_BaseArmor.obj", lod2.transform, Vector3.zero, baseMat);
            GameObject lod2Team = InstantiateModel("LOD2_TeamColorPanels", Meshes + "/" + Slug + "_LOD2_TeamColorPanels.obj", lod2.transform, Vector3.zero, teamMat);

            Transform spawn = NewChild("SpawnPoint", root.transform, new Vector3(0f, 0.95f, -8.45f)).transform;
            Transform rally = NewChild("RallyPoint", root.transform, new Vector3(0f, 0.95f, -10.25f)).transform;
            Transform assembly = NewChild("VehicleAssemblySocket", root.transform, new Vector3(0f, 1.55f, -0.55f)).transform;
            Transform interior = NewChild("InteriorPoint", root.transform, new Vector3(0f, 1.80f, 0.35f)).transform;
            Transform doorSocket = NewChild("DoorSocket", root.transform, new Vector3(0f, 2.20f, -4.85f)).transform;
            Transform conveyorSocket = NewChild("ConveyorSocket", root.transform, new Vector3(0f, 1.05f, -0.55f)).transform;
            Transform smokeA = NewChild("SmokeSocket_MainStack", root.transform, new Vector3(6.95f, 5.40f, 4.98f)).transform;
            Transform smokeB = NewChild("SmokeSocket_MidStack", root.transform, new Vector3(5.65f, 4.40f, 5.35f)).transform;
            Transform smokeC = NewChild("SmokeSocket_OuterStack", root.transform, new Vector3(7.85f, 4.00f, 2.85f)).transform;
            Transform smokeD = NewChild("SmokeSocket_ServiceVent", root.transform, new Vector3(-7.20f, 3.30f, 4.25f)).transform;
            Transform lightA = NewChild("LightSocket_LeftBay", root.transform, new Vector3(-5.55f, 2.55f, -4.90f)).transform;
            Transform lightB = NewChild("LightSocket_RightBay", root.transform, new Vector3(5.55f, 2.55f, -4.90f)).transform;
            Transform lightC = NewChild("LightSocket_Interior", root.transform, new Vector3(0f, 3.30f, -1.00f)).transform;

            var metadata = root.AddComponent<BastionWarFactoryCMV2.BastionWarFactoryCMV2>();
            metadata.spawnPoint = spawn;
            metadata.rallyPoint = rally;
            metadata.vehicleAssemblySocket = assembly;
            metadata.interiorPoint = interior;
            metadata.doorSocket = doorSocket;
            metadata.conveyorSocket = conveyorSocket;
            metadata.smokeSockets = new Transform[] { smokeA, smokeB, smokeC, smokeD };
            metadata.lightSockets = new Transform[] { lightA, lightB, lightC };
            metadata.robotArmSockets = new Transform[] { armA.transform, armB.transform, armC.transform, armD.transform };

            var active = root.AddComponent<BastionWarFactoryCMV2.BastionWarFactoryActiveAnimator>();
            active.door = door.transform;
            active.conveyor = conveyor.transform;
            active.gantryTrolley = trolley.transform;
            active.roofFans = new Transform[] { fanA.transform, fanB.transform, fanC.transform };
            active.robotArms = new Transform[] { armA.transform, armB.transform, armC.transform, armD.transform };
            active.beacons = new Transform[] { beaconA.transform, beaconB.transform, beaconC.transform, beaconD.transform };
            active.smokeSockets = new Transform[] { smokeA, smokeB, smokeC, smokeD };

            var teamColor = root.AddComponent<BastionWarFactoryCMV2.BastionTeamColor>();
            List<Renderer> teamRenderers = new List<Renderer>();
            teamRenderers.AddRange(team.GetComponentsInChildren<Renderer>());
            teamRenderers.AddRange(lod1Team.GetComponentsInChildren<Renderer>());
            teamRenderers.AddRange(lod2Team.GetComponentsInChildren<Renderer>());
            teamColor.teamColorRenderers = teamRenderers.ToArray();
            teamColor.teamColor = Color.white;

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 3.15f, -0.15f);
            col.size = new Vector3(17.4f, 6.6f, 14.8f);

            ConfigureLODGroup(root, lod0, lod1, lod2);

            string prefabPath = Prefabs + "/Bastion_WarFactoryCMV2.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (selectPrefab)
            {
                Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            Debug.Log("Bastion War Factory CMV2 prefab created at " + prefabPath);
        }

        private static GameObject InstantiateModel(string name, string path, Transform parent, Vector3 localPosition, Material material)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError("Missing model asset: " + path);
                GameObject missing = new GameObject(name + "_MISSING");
                missing.transform.SetParent(parent, false);
                missing.transform.localPosition = localPosition;
                return missing;
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            return instance;
        }

        private static GameObject NewChild(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        private static void ConfigureLODGroup(GameObject root, GameObject lod0, GameObject lod1, GameObject lod2)
        {
            LODGroup group = root.AddComponent<LODGroup>();
            LOD[] lods = new LOD[3];
            lods[0] = new LOD(0.60f, lod0.GetComponentsInChildren<Renderer>());
            lods[1] = new LOD(0.25f, lod1.GetComponentsInChildren<Renderer>());
            lods[2] = new LOD(0.08f, lod2.GetComponentsInChildren<Renderer>());
            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        private static Material CreateBaseMaterial()
        {
            Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_BaseAtlas");
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_WarFactoryCMV2_Atlas.png");
            mat.SetTexture("_BaseMap", atlas);
            mat.SetTexture("_MainTex", atlas);
            mat.SetFloat("_Smoothness", 0.24f);
            mat.SetFloat("_Metallic", 0.03f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateEmissiveMaterial()
        {
            Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_EmissiveAtlas");
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_WarFactoryCMV2_Atlas.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_WarFactoryCMV2_Emission.png");
            mat.SetTexture("_BaseMap", atlas);
            mat.SetTexture("_MainTex", atlas);
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", Color.white * 1.2f);
            mat.SetFloat("_Smoothness", 0.36f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateTeamMaterial()
        {
            Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_TeamColor");
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", 0.34f);
            mat.SetFloat("_Metallic", 0.02f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateGlassMaterial()
        {
            Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_GlassLights");
            mat.SetColor("_BaseColor", new Color(0.12f, 0.55f, 0.64f, 0.86f));
            mat.SetColor("_Color", new Color(0.12f, 0.55f, 0.64f, 0.86f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.10f, 0.85f, 1.0f) * 0.7f);
            mat.SetFloat("_Smoothness", 0.55f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateSmokeMaterial()
        {
            Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_SmokeFXMarkers");
            mat.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.55f, 0.35f));
            mat.SetColor("_Color", new Color(0.55f, 0.55f, 0.55f, 0.35f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateMaterial(string name)
        {
            EnsureFolder(Materials);
            string path = Materials + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(FindUsableShader());
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static Shader FindUsableShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
