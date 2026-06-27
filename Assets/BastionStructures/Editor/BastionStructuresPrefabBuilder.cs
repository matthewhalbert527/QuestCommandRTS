using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BastionStructures.Editor
{
    internal static class BastionStructuresPrefabBuilder
    {
        private const string Root = "Assets/BastionStructures";
        private const string Meshes = Root + "/Meshes";
        private const string Textures = Root + "/Textures";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string MaterialPath = Materials + "/Bastion_Structures_Palette.mat";

        private static readonly string[] RequiredModels =
        {
            Meshes + "/Bastion_Barracks_Base.obj",
            Meshes + "/Bastion_Barracks_LOD1.obj",
            Meshes + "/Bastion_WarFactory_Base.obj",
            Meshes + "/Bastion_WarFactory_FactoryDoor.obj",
            Meshes + "/Bastion_WarFactory_LOD1.obj",
            Meshes + "/Bastion_Refinery_Base.obj",
            Meshes + "/Bastion_Refinery_UnloaderSpin.obj",
            Meshes + "/Bastion_Refinery_LOD1.obj",
            Meshes + "/Bastion_Harvester_Base.obj",
            Meshes + "/Bastion_Harvester_CollectorSpin.obj",
            Meshes + "/Bastion_Harvester_LOD1.obj",
            Meshes + "/Bastion_PowerPlant_Base.obj",
            Meshes + "/Bastion_PowerPlant_TurbineSpin.obj",
            Meshes + "/Bastion_PowerPlant_LOD1.obj",
            Meshes + "/Bastion_LargePowerPlant_Base.obj",
            Meshes + "/Bastion_LargePowerPlant_TurbineSpinLeft.obj",
            Meshes + "/Bastion_LargePowerPlant_TurbineSpinRight.obj",
            Meshes + "/Bastion_LargePowerPlant_LOD1.obj",
            Meshes + "/Bastion_CommunicationsCenter_Base.obj",
            Meshes + "/Bastion_CommunicationsCenter_DishYaw.obj",
            Meshes + "/Bastion_CommunicationsCenter_DishPitch.obj",
            Meshes + "/Bastion_CommunicationsCenter_LOD1.obj",
            Meshes + "/Bastion_RepairBay_Base.obj",
            Meshes + "/Bastion_RepairBay_ServiceArmLeft.obj",
            Meshes + "/Bastion_RepairBay_ServiceArmRight.obj",
            Meshes + "/Bastion_RepairBay_LOD1.obj",
            Meshes + "/Bastion_MASH_Base.obj",
            Meshes + "/Bastion_MASH_BeaconSpin.obj",
            Meshes + "/Bastion_MASH_LOD1.obj",
            Meshes + "/Bastion_TechCenter_Base.obj",
            Meshes + "/Bastion_TechCenter_EnergyRingSpin.obj",
            Meshes + "/Bastion_TechCenter_LOD1.obj",
            Meshes + "/Bastion_Turret_Base.obj",
            Meshes + "/Bastion_Turret_WeaponYaw.obj",
            Meshes + "/Bastion_Turret_WeaponPitch.obj",
            Meshes + "/Bastion_Turret_LOD1.obj",
            Meshes + "/Bastion_GunTower_Base.obj",
            Meshes + "/Bastion_GunTower_WeaponYaw.obj",
            Meshes + "/Bastion_GunTower_WeaponPitch.obj",
            Meshes + "/Bastion_GunTower_LOD1.obj",
            Meshes + "/Bastion_AdvancedGunTower_Base.obj",
            Meshes + "/Bastion_AdvancedGunTower_WeaponYaw.obj",
            Meshes + "/Bastion_AdvancedGunTower_WeaponPitch.obj",
            Meshes + "/Bastion_AdvancedGunTower_LOD1.obj"
        };

        [InitializeOnLoadMethod]
        private static void QueueAutomaticBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetsReady() && !AllPrefabsExist()) BuildAll(false);
            };
        }

        [MenuItem("Tools/Bastion Base Structures/Create or Rebuild All Prefabs")]
        private static void BuildFromMenu()
        {
            BuildAll(true);
        }

        private static bool AssetsReady()
        {
            foreach (string path in RequiredModels)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return false;
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Structures_Palette.png") != null;
        }

        private static bool AllPrefabsExist()
        {
            string[] prefabs =
            {
                "Bastion_Barracks.prefab", "Bastion_WarFactory.prefab", "Bastion_Refinery.prefab", "Bastion_Harvester.prefab", "Bastion_PowerPlant.prefab", "Bastion_LargePowerPlant.prefab", "Bastion_CommunicationsCenter.prefab", "Bastion_RepairBay.prefab", "Bastion_MASH.prefab", "Bastion_TechCenter.prefab", "Bastion_Turret.prefab", "Bastion_GunTower.prefab", "Bastion_AdvancedGunTower.prefab"
            };
            foreach (string prefab in prefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + prefab) == null) return false;
            }
            return true;
        }

        private static void BuildAll(bool selectFolder)
        {
            if (!AssetsReady())
            {
                Debug.LogWarning("Bastion Base Structures: model imports are not ready. Run the build command after Unity finishes importing.");
                return;
            }

            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            Material material = CreateOrUpdateMaterial();
            var roots = new List<GameObject>
            {
                Build_Bastion_Barracks(material),
                Build_Bastion_WarFactory(material),
                Build_Bastion_Refinery(material),
                Build_Bastion_Harvester(material),
                Build_Bastion_PowerPlant(material),
                Build_Bastion_LargePowerPlant(material),
                Build_Bastion_CommunicationsCenter(material),
                Build_Bastion_RepairBay(material),
                Build_Bastion_MASH(material),
                Build_Bastion_TechCenter(material),
                Build_Bastion_Turret(material),
                Build_Bastion_GunTower(material),
                Build_Bastion_AdvancedGunTower(material)
            };

            foreach (GameObject root in roots)
            {
                string path = Prefabs + "/" + root.name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (selectFolder)
            {
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Prefabs);
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
            Debug.Log("Bastion Base Structures: created 13 prefabs in " + Prefabs + ".");
        }

        private static GameObject Build_Bastion_Barracks(Material material)
        {
            GameObject root = NewRoot("Bastion_Barracks");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_Barracks_Base.obj", Base.transform, Vector3.zero, material);
            GameObject SpawnPoint = NewChild("SpawnPoint", root.transform, new Vector3(0.00000f, 0.15000f, 3.25000f));
            GameObject RallyPoint = NewChild("RallyPoint", root.transform, new Vector3(0.00000f, 0.15000f, 6.00000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_Barracks_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Barracks", BastionStructureRole.InfantryProduction, new Vector2(6.20000f, 5.20000f), 1200.00000f, 0, 10, SpawnPoint.transform, RallyPoint.transform, null, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.77500f, 0f);
            collider.size = new Vector3(6.20000f, 3.55000f, 5.20000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_WarFactory(Material material)
        {
            GameObject root = NewRoot("Bastion_WarFactory");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_WarFactory_Base.obj", Base.transform, Vector3.zero, material);
            GameObject FactoryDoor = NewChild("FactoryDoor", lod0.transform, new Vector3(0.00000f, 0.28000f, 3.02000f));
            InstantiateModel("FactoryDoorModel", Meshes + "/Bastion_WarFactory_FactoryDoor.obj", FactoryDoor.transform, Vector3.zero, material);
            GameObject SpawnPoint = NewChild("SpawnPoint", root.transform, new Vector3(0.00000f, 0.20000f, 4.45000f));
            GameObject RallyPoint = NewChild("RallyPoint", root.transform, new Vector3(0.00000f, 0.20000f, 8.80000f));
            GameObject FactoryInteriorPoint = NewChild("FactoryInteriorPoint", root.transform, new Vector3(0.00000f, 0.20000f, 1.25000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_WarFactory_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion War Factory", BastionStructureRole.VehicleProduction, new Vector2(10.20000f, 9.00000f), 2200.00000f, 0, 30, SpawnPoint.transform, RallyPoint.transform, null, null);
            BastionDoorController door = root.AddComponent<BastionDoorController>();
            door.Configure(FactoryDoor.transform, new Vector3(0.00000f, 3.25000f, 0.00000f), 3.25f, false);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.62500f, 0f);
            collider.size = new Vector3(10.20000f, 5.25000f, 9.00000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_Refinery(Material material)
        {
            GameObject root = NewRoot("Bastion_Refinery");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_Refinery_Base.obj", Base.transform, Vector3.zero, material);
            GameObject UnloaderSpin = NewChild("UnloaderSpin", lod0.transform, new Vector3(2.60000f, 1.35000f, 2.92000f));
            InstantiateModel("UnloaderSpinModel", Meshes + "/Bastion_Refinery_UnloaderSpin.obj", UnloaderSpin.transform, Vector3.zero, material);
            BastionSpin spin_UnloaderSpin = UnloaderSpin.AddComponent<BastionSpin>();
            spin_UnloaderSpin.Configure(UnloaderSpin.transform, new Vector3(1.00000f, 0.00000f, 0.00000f), 22.00000f, true);
            GameObject DockPoint = NewChild("DockPoint", root.transform, new Vector3(2.60000f, 0.18000f, 4.05000f));
            GameObject UnloadPoint = NewChild("UnloadPoint", root.transform, new Vector3(2.60000f, 1.20000f, 2.65000f));
            GameObject FXSocket = NewChild("FXSocket", root.transform, new Vector3(-2.55000f, 4.85000f, -0.75000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_Refinery_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Refinery", BastionStructureRole.Economy, new Vector2(9.20000f, 8.00000f), 2000.00000f, 0, 20, null, null, DockPoint.transform, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.42500f, 0f);
            collider.size = new Vector3(9.20000f, 4.85000f, 8.00000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_Harvester(Material material)
        {
            GameObject root = NewRoot("Bastion_Harvester");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_Harvester_Base.obj", Base.transform, Vector3.zero, material);
            GameObject CollectorSpin = NewChild("CollectorSpin", lod0.transform, new Vector3(0.00000f, 0.72000f, 2.78000f));
            InstantiateModel("CollectorSpinModel", Meshes + "/Bastion_Harvester_CollectorSpin.obj", CollectorSpin.transform, Vector3.zero, material);
            BastionSpin spin_CollectorSpin = CollectorSpin.AddComponent<BastionSpin>();
            spin_CollectorSpin.Configure(CollectorSpin.transform, new Vector3(1.00000f, 0.00000f, 0.00000f), 75.00000f, true);
            GameObject CargoPoint = NewChild("CargoPoint", root.transform, new Vector3(0.00000f, 2.25000f, -1.45000f));
            GameObject UnloadPoint = NewChild("UnloadPoint", root.transform, new Vector3(0.00000f, 1.55000f, -2.65000f));
            GameObject PathOrigin = NewChild("PathOrigin", root.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_Harvester_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Harvester", BastionStructureRole.Vehicle, new Vector2(4.10000f, 6.20000f), 1400.00000f, 0, 0, null, null, null, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.50000f, 0f);
            collider.size = new Vector3(4.10000f, 3.00000f, 6.20000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_PowerPlant(Material material)
        {
            GameObject root = NewRoot("Bastion_PowerPlant");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_PowerPlant_Base.obj", Base.transform, Vector3.zero, material);
            GameObject TurbineSpin = NewChild("TurbineSpin", lod0.transform, new Vector3(0.00000f, 3.02000f, -0.45000f));
            InstantiateModel("TurbineSpinModel", Meshes + "/Bastion_PowerPlant_TurbineSpin.obj", TurbineSpin.transform, Vector3.zero, material);
            BastionSpin spin_TurbineSpin = TurbineSpin.AddComponent<BastionSpin>();
            spin_TurbineSpin.Configure(TurbineSpin.transform, new Vector3(0.00000f, 1.00000f, 0.00000f), 45.00000f, true);
            GameObject FXSocket = NewChild("FXSocket", root.transform, new Vector3(-1.45000f, 3.65000f, -0.95000f));
            GameObject PowerCoreSocket = NewChild("PowerCoreSocket", root.transform, new Vector3(0.00000f, 3.02000f, -0.45000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_PowerPlant_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Power Plant", BastionStructureRole.Power, new Vector2(5.60000f, 5.00000f), 1000.00000f, 100, 0, null, null, null, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.17500f, 0f);
            collider.size = new Vector3(5.60000f, 4.35000f, 5.00000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_LargePowerPlant(Material material)
        {
            GameObject root = NewRoot("Bastion_LargePowerPlant");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_LargePowerPlant_Base.obj", Base.transform, Vector3.zero, material);
            GameObject TurbineSpinLeft = NewChild("TurbineSpinLeft", lod0.transform, new Vector3(-1.75000f, 3.55000f, -0.85000f));
            InstantiateModel("TurbineSpinLeftModel", Meshes + "/Bastion_LargePowerPlant_TurbineSpinLeft.obj", TurbineSpinLeft.transform, Vector3.zero, material);
            BastionSpin spin_TurbineSpinLeft = TurbineSpinLeft.AddComponent<BastionSpin>();
            spin_TurbineSpinLeft.Configure(TurbineSpinLeft.transform, new Vector3(0.00000f, 1.00000f, 0.00000f), 38.00000f, true);
            GameObject TurbineSpinRight = NewChild("TurbineSpinRight", lod0.transform, new Vector3(1.75000f, 3.55000f, -0.85000f));
            InstantiateModel("TurbineSpinRightModel", Meshes + "/Bastion_LargePowerPlant_TurbineSpinRight.obj", TurbineSpinRight.transform, Vector3.zero, material);
            BastionSpin spin_TurbineSpinRight = TurbineSpinRight.AddComponent<BastionSpin>();
            spin_TurbineSpinRight.Configure(TurbineSpinRight.transform, new Vector3(0.00000f, 1.00000f, 0.00000f), -38.00000f, true);
            GameObject FXSocketLeft = NewChild("FXSocketLeft", root.transform, new Vector3(-1.75000f, 5.48000f, -0.85000f));
            GameObject FXSocketRight = NewChild("FXSocketRight", root.transform, new Vector3(1.75000f, 5.48000f, -0.85000f));
            GameObject PowerCoreSocket = NewChild("PowerCoreSocket", root.transform, new Vector3(0.00000f, 4.20000f, -1.65000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_LargePowerPlant_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Large Power Plant", BastionStructureRole.Power, new Vector2(8.20000f, 7.20000f), 1800.00000f, 250, 0, null, null, null, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 3.20000f, 0f);
            collider.size = new Vector3(8.20000f, 6.40000f, 7.20000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_CommunicationsCenter(Material material)
        {
            GameObject root = NewRoot("Bastion_CommunicationsCenter");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_CommunicationsCenter_Base.obj", Base.transform, Vector3.zero, material);
            GameObject DishYaw = NewChild("DishYaw", lod0.transform, new Vector3(0.00000f, 3.80000f, -0.35000f));
            InstantiateModel("DishYawModel", Meshes + "/Bastion_CommunicationsCenter_DishYaw.obj", DishYaw.transform, Vector3.zero, material);
            BastionSpin spin_DishYaw = DishYaw.AddComponent<BastionSpin>();
            spin_DishYaw.Configure(DishYaw.transform, new Vector3(0.00000f, 1.00000f, 0.00000f), 12.00000f, true);
            GameObject DishPitch = NewChild("DishPitch", DishYaw.transform, new Vector3(0.00000f, 1.20000f, 0.00000f));
            InstantiateModel("DishPitchModel", Meshes + "/Bastion_CommunicationsCenter_DishPitch.obj", DishPitch.transform, Vector3.zero, material);
            GameObject SignalOrigin = NewChild("SignalOrigin", DishPitch.transform, new Vector3(0.00000f, 5.00000f, -0.20000f));
            GameObject FXSocket = NewChild("FXSocket", root.transform, new Vector3(1.85000f, 5.65000f, -1.35000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_CommunicationsCenter_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Communications Center", BastionStructureRole.Support, new Vector2(6.80000f, 6.60000f), 1200.00000f, 0, 40, null, null, null, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 3.27500f, 0f);
            collider.size = new Vector3(6.80000f, 6.55000f, 6.60000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_RepairBay(Material material)
        {
            GameObject root = NewRoot("Bastion_RepairBay");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_RepairBay_Base.obj", Base.transform, Vector3.zero, material);
            GameObject ServiceArmLeft = NewChild("ServiceArmLeft", lod0.transform, new Vector3(-2.30000f, 2.60000f, 0.25000f));
            InstantiateModel("ServiceArmLeftModel", Meshes + "/Bastion_RepairBay_ServiceArmLeft.obj", ServiceArmLeft.transform, Vector3.zero, material);
            GameObject ServiceArmRight = NewChild("ServiceArmRight", lod0.transform, new Vector3(2.30000f, 2.60000f, 0.25000f));
            InstantiateModel("ServiceArmRightModel", Meshes + "/Bastion_RepairBay_ServiceArmRight.obj", ServiceArmRight.transform, Vector3.zero, material);
            GameObject ServicePoint = NewChild("ServicePoint", root.transform, new Vector3(0.00000f, 0.45000f, 0.35000f));
            GameObject VehicleEntryPoint = NewChild("VehicleEntryPoint", root.transform, new Vector3(0.00000f, 0.22000f, 3.90000f));
            GameObject FXSocketLeft = NewChild("FXSocketLeft", ServiceArmLeft.transform, new Vector3(0.12000f, -0.25000f, 0.65000f));
            GameObject FXSocketRight = NewChild("FXSocketRight", ServiceArmRight.transform, new Vector3(-0.12000f, -0.25000f, 0.65000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_RepairBay_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Repair Bay", BastionStructureRole.Support, new Vector2(8.20000f, 7.20000f), 1600.00000f, 0, 35, null, null, null, ServicePoint.transform);
            BastionRepairBayController repair = root.AddComponent<BastionRepairBayController>();
            repair.Configure(ServiceArmLeft.transform, ServiceArmRight.transform, 0f, 18f, 55f);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.05000f, 0f);
            collider.size = new Vector3(8.20000f, 4.10000f, 7.20000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_MASH(Material material)
        {
            GameObject root = NewRoot("Bastion_MASH");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_MASH_Base.obj", Base.transform, Vector3.zero, material);
            GameObject BeaconSpin = NewChild("BeaconSpin", lod0.transform, new Vector3(0.00000f, 3.52000f, -0.45000f));
            InstantiateModel("BeaconSpinModel", Meshes + "/Bastion_MASH_BeaconSpin.obj", BeaconSpin.transform, Vector3.zero, material);
            BastionSpin spin_BeaconSpin = BeaconSpin.AddComponent<BastionSpin>();
            spin_BeaconSpin.Configure(BeaconSpin.transform, new Vector3(0.00000f, 1.00000f, 0.00000f), 90.00000f, true);
            GameObject HealPoint = NewChild("HealPoint", root.transform, new Vector3(0.00000f, 0.18000f, 3.25000f));
            GameObject InfantryExitPoint = NewChild("InfantryExitPoint", root.transform, new Vector3(0.00000f, 0.18000f, 3.75000f));
            GameObject MedicalFXSocket = NewChild("MedicalFXSocket", root.transform, new Vector3(0.00000f, 3.70000f, -0.45000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_MASH_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion MASH", BastionStructureRole.Medical, new Vector2(7.20000f, 6.20000f), 1100.00000f, 0, 20, null, null, null, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.97500f, 0f);
            collider.size = new Vector3(7.20000f, 3.95000f, 6.20000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_TechCenter(Material material)
        {
            GameObject root = NewRoot("Bastion_TechCenter");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_TechCenter_Base.obj", Base.transform, Vector3.zero, material);
            GameObject EnergyRingSpin = NewChild("EnergyRingSpin", lod0.transform, new Vector3(0.00000f, 5.00000f, -0.75000f));
            InstantiateModel("EnergyRingSpinModel", Meshes + "/Bastion_TechCenter_EnergyRingSpin.obj", EnergyRingSpin.transform, Vector3.zero, material);
            BastionSpin spin_EnergyRingSpin = EnergyRingSpin.AddComponent<BastionSpin>();
            spin_EnergyRingSpin.Configure(EnergyRingSpin.transform, new Vector3(0.00000f, 1.00000f, 0.00000f), 24.00000f, true);
            GameObject ResearchFXSocket = NewChild("ResearchFXSocket", root.transform, new Vector3(0.00000f, 5.00000f, -0.75000f));
            GameObject AntennaSocket = NewChild("AntennaSocket", root.transform, new Vector3(0.00000f, 6.00000f, -0.75000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_TechCenter_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Tech Center", BastionStructureRole.Technology, new Vector2(8.20000f, 8.00000f), 2000.00000f, 0, 75, null, null, null, null);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 3.00000f, 0f);
            collider.size = new Vector3(8.20000f, 6.00000f, 8.00000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_Turret(Material material)
        {
            GameObject root = NewRoot("Bastion_Turret");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_Turret_Base.obj", Base.transform, Vector3.zero, material);
            GameObject WeaponYaw = NewChild("WeaponYaw", lod0.transform, new Vector3(0.00000f, 1.10000f, 0.00000f));
            InstantiateModel("WeaponYawModel", Meshes + "/Bastion_Turret_WeaponYaw.obj", WeaponYaw.transform, Vector3.zero, material);
            GameObject WeaponPitch = NewChild("WeaponPitch", WeaponYaw.transform, new Vector3(0.00000f, 0.45000f, 0.62000f));
            InstantiateModel("WeaponPitchModel", Meshes + "/Bastion_Turret_WeaponPitch.obj", WeaponPitch.transform, Vector3.zero, material);
            GameObject Muzzle_01 = NewChild("Muzzle_01", WeaponPitch.transform, new Vector3(0.00000f, 0.00000f, 2.98000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_Turret_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Turret", BastionStructureRole.Defense, new Vector2(3.40000f, 3.40000f), 900.00000f, 0, 10, null, null, null, null);
            BastionDefenseController defense = root.AddComponent<BastionDefenseController>();
            defense.Configure(WeaponYaw.transform, WeaponPitch.transform, new Transform[] { Muzzle_01.transform }, 95.00000f, 70.00000f, -8.00000f, 38.00000f);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.22500f, 0f);
            collider.size = new Vector3(3.40000f, 2.45000f, 3.40000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_GunTower(Material material)
        {
            GameObject root = NewRoot("Bastion_GunTower");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_GunTower_Base.obj", Base.transform, Vector3.zero, material);
            GameObject WeaponYaw = NewChild("WeaponYaw", lod0.transform, new Vector3(0.00000f, 4.55000f, 0.00000f));
            InstantiateModel("WeaponYawModel", Meshes + "/Bastion_GunTower_WeaponYaw.obj", WeaponYaw.transform, Vector3.zero, material);
            GameObject WeaponPitch = NewChild("WeaponPitch", WeaponYaw.transform, new Vector3(0.00000f, 0.42000f, 0.72000f));
            InstantiateModel("WeaponPitchModel", Meshes + "/Bastion_GunTower_WeaponPitch.obj", WeaponPitch.transform, Vector3.zero, material);
            GameObject Muzzle_01 = NewChild("Muzzle_01", WeaponPitch.transform, new Vector3(-0.38000f, 0.00000f, 2.70000f));
            GameObject Muzzle_02 = NewChild("Muzzle_02", WeaponPitch.transform, new Vector3(0.38000f, 0.00000f, 2.70000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_GunTower_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Gun Tower", BastionStructureRole.Defense, new Vector2(4.00000f, 4.00000f), 1300.00000f, 0, 20, null, null, null, null);
            BastionDefenseController defense = root.AddComponent<BastionDefenseController>();
            defense.Configure(WeaponYaw.transform, WeaponPitch.transform, new Transform[] { Muzzle_01.transform, Muzzle_02.transform }, 105.00000f, 78.00000f, -10.00000f, 48.00000f);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.82500f, 0f);
            collider.size = new Vector3(4.00000f, 5.65000f, 4.00000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }
        private static GameObject Build_Bastion_AdvancedGunTower(Material material)
        {
            GameObject root = NewRoot("Bastion_AdvancedGunTower");
            GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
            GameObject Base = NewChild("Base", lod0.transform, new Vector3(0.00000f, 0.00000f, 0.00000f));
            InstantiateModel("BaseModel", Meshes + "/Bastion_AdvancedGunTower_Base.obj", Base.transform, Vector3.zero, material);
            GameObject WeaponYaw = NewChild("WeaponYaw", lod0.transform, new Vector3(0.00000f, 5.82000f, 0.00000f));
            InstantiateModel("WeaponYawModel", Meshes + "/Bastion_AdvancedGunTower_WeaponYaw.obj", WeaponYaw.transform, Vector3.zero, material);
            GameObject WeaponPitch = NewChild("WeaponPitch", WeaponYaw.transform, new Vector3(0.00000f, 0.55000f, 0.82000f));
            InstantiateModel("WeaponPitchModel", Meshes + "/Bastion_AdvancedGunTower_WeaponPitch.obj", WeaponPitch.transform, Vector3.zero, material);
            GameObject Muzzle_01 = NewChild("Muzzle_01", WeaponPitch.transform, new Vector3(-1.02000f, -0.22000f, 1.52000f));
            GameObject Muzzle_02 = NewChild("Muzzle_02", WeaponPitch.transform, new Vector3(-0.54000f, -0.22000f, 1.52000f));
            GameObject Muzzle_03 = NewChild("Muzzle_03", WeaponPitch.transform, new Vector3(-1.02000f, 0.22000f, 1.52000f));
            GameObject Muzzle_04 = NewChild("Muzzle_04", WeaponPitch.transform, new Vector3(-0.54000f, 0.22000f, 1.52000f));
            GameObject Muzzle_05 = NewChild("Muzzle_05", WeaponPitch.transform, new Vector3(0.54000f, -0.22000f, 1.52000f));
            GameObject Muzzle_06 = NewChild("Muzzle_06", WeaponPitch.transform, new Vector3(1.02000f, -0.22000f, 1.52000f));
            GameObject Muzzle_07 = NewChild("Muzzle_07", WeaponPitch.transform, new Vector3(0.54000f, 0.22000f, 1.52000f));
            GameObject Muzzle_08 = NewChild("Muzzle_08", WeaponPitch.transform, new Vector3(1.02000f, 0.22000f, 1.52000f));
            GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
            InstantiateModel("LOD1Model", Meshes + "/Bastion_AdvancedGunTower_LOD1.obj", lod1.transform, Vector3.zero, material);
            BastionStructure metadata = root.AddComponent<BastionStructure>();
            metadata.Configure("Bastion Advanced Gun Tower", BastionStructureRole.Defense, new Vector2(4.80000f, 4.80000f), 1800.00000f, 0, 40, null, null, null, null);
            BastionDefenseController defense = root.AddComponent<BastionDefenseController>();
            defense.Configure(WeaponYaw.transform, WeaponPitch.transform, new Transform[] { Muzzle_01.transform, Muzzle_02.transform, Muzzle_03.transform, Muzzle_04.transform, Muzzle_05.transform, Muzzle_06.transform, Muzzle_07.transform, Muzzle_08.transform }, 78.00000f, 62.00000f, -5.00000f, 58.00000f);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 3.62500f, 0f);
            collider.size = new Vector3(4.80000f, 7.25000f, 4.80000f);
            ConfigureLODGroup(root, lod0, lod1);
            return root;
        }

        private static GameObject NewRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
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

        private static GameObject InstantiateModel(string name, string path, Transform parent, Vector3 localPosition, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Missing model: " + path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return instance;
        }

        private static void ConfigureLODGroup(GameObject root, GameObject lod0, GameObject lod1)
        {
            LODGroup group = root.AddComponent<LODGroup>();
            Renderer[] lod0Renderers = lod0.GetComponentsInChildren<Renderer>(true);
            Renderer[] lod1Renderers = lod1.GetComponentsInChildren<Renderer>(true);
            group.SetLODs(new[]
            {
                new LOD(0.34f, lod0Renderers),
                new LOD(0.09f, lod1Renderers),
                new LOD(0.01f, Array.Empty<Renderer>())
            });
            group.fadeMode = LODFadeMode.None;
            group.RecalculateBounds();
        }

        private static Material CreateOrUpdateMaterial()
        {
            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Structures_Palette.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Structures_Emission.png");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = "Bastion_Structures_Palette" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            SetTextureIfPresent(material, "_BaseMap", palette);
            SetTextureIfPresent(material, "_MainTex", palette);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Metallic", 0.12f);
            SetFloatIfPresent(material, "_Smoothness", 0.26f);
            SetFloatIfPresent(material, "_Glossiness", 0.26f);
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
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
