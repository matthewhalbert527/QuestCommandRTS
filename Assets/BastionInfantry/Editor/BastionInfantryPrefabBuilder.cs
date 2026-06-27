using System;
using System.Collections.Generic;
using System.IO;
using QuestCommandRTS;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BastionInfantry.Editor
{
    internal static class BastionInfantryPrefabBuilder
    {
        private const string Root = "Assets/BastionInfantry";
        private const string Meshes = Root + "/Meshes";
        private const string Textures = Root + "/Textures";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string MaterialPath = Materials + "/Bastion_Infantry_Palette.mat";
        private const string DetailTexturePrefix = Textures + "/Bastion_Infantry_Detail_";
        private const string DetailMaterialPrefix = Materials + "/Bastion_Infantry_Detail_";

        private sealed class RoleConfig
        {
            public readonly string Slug;
            public readonly string DisplayName;
            public readonly BastionInfantryRole Role;
            public readonly BastionWeaponDelivery Delivery;
            public readonly Vector3 Muzzle;
            public readonly float MaxHealth;
            public readonly float MoveSpeed;
            public readonly float TurnSpeed;
            public readonly float AttackRange;
            public readonly float Cooldown;
            public readonly float Damage;
            public readonly float SplashRadius;
            public readonly float ProjectileSpeed;
            public readonly float UtilityPower;

            public RoleConfig(string slug, string displayName, BastionInfantryRole role, BastionWeaponDelivery delivery,
                Vector3 muzzle, float maxHealth, float moveSpeed, float turnSpeed, float attackRange,
                float cooldown, float damage, float splashRadius, float projectileSpeed, float utilityPower)
            {
                Slug = slug;
                DisplayName = displayName;
                Role = role;
                Delivery = delivery;
                Muzzle = muzzle;
                MaxHealth = maxHealth;
                MoveSpeed = moveSpeed;
                TurnSpeed = turnSpeed;
                AttackRange = attackRange;
                Cooldown = cooldown;
                Damage = damage;
                SplashRadius = splashRadius;
                ProjectileSpeed = projectileSpeed;
                UtilityPower = utilityPower;
            }
        }

        private sealed class DetailMaterialSet
        {
            public Material Plate;
            public Material Canvas;
            public Material Webbing;
            public Material Metal;
            public Material Dark;
            public Material Hazard;
            public Material Cyan;
            public Material Amber;
        }

        private static readonly RoleConfig[] Roles =
        {
            new RoleConfig("Gunner", "Gunner", BastionInfantryRole.Gunner, BastionWeaponDelivery.Hitscan, new Vector3(0.000f, -0.010f, 1.050f), 100.00f, 3.60f, 420.00f, 19.00f, 0.120f, 8.00f, 0.00f, 0.00f, 0.00f),
            new RoleConfig("Grenadier", "Grenadier", BastionInfantryRole.Grenadier, BastionWeaponDelivery.LobbedProjectile, new Vector3(0.000f, 0.000f, 1.000f), 105.00f, 3.25f, 380.00f, 15.00f, 1.550f, 52.00f, 2.80f, 16.00f, 0.00f),
            new RoleConfig("RocketSoldier", "Rocket Soldier", BastionInfantryRole.RocketSoldier, BastionWeaponDelivery.Rocket, new Vector3(0.070f, 0.130f, 1.100f), 115.00f, 2.95f, 340.00f, 27.00f, 2.850f, 120.00f, 1.60f, 28.00f, 0.00f),
            new RoleConfig("FlameTrooper", "Flame Trooper", BastionInfantryRole.FlameTrooper, BastionWeaponDelivery.FlameStream, new Vector3(0.000f, -0.030f, 1.050f), 145.00f, 3.00f, 360.00f, 8.50f, 0.080f, 6.00f, 1.25f, 0.00f, 0.00f),
            new RoleConfig("Engineer", "Engineer", BastionInfantryRole.Engineer, BastionWeaponDelivery.RepairTool, new Vector3(0.000f, -0.040f, 0.820f), 90.00f, 3.45f, 400.00f, 5.50f, 0.200f, 5.00f, 0.00f, 0.00f, 38.00f)
        };

        [InitializeOnLoadMethod]
        private static void QueueAutomaticBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetsReady() && AnyPrefabMissing())
                {
                    BuildAll(false);
                }
            };
        }

        [MenuItem("Tools/Bastion Infantry/Create or Rebuild All Soldier Prefabs")]
        private static void BuildFromMenu()
        {
            BuildAll(true);
        }

        private static bool AnyPrefabMissing()
        {
            foreach (RoleConfig role in Roles)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(role)) == null)
                {
                    return true;
                }
            }
            return false;
        }

        private static string PrefabPath(RoleConfig role)
        {
            return Prefabs + "/Bastion_" + role.Slug + ".prefab";
        }

        private static string ModelPath(RoleConfig role, string part)
        {
            return Meshes + "/Bastion_" + role.Slug + "_" + part + ".obj";
        }

        private static bool AssetsReady()
        {
            string[] parts = { "Pelvis", "LeftLeg", "RightLeg", "Torso", "Head", "Arms", "Weapon", "Gear", "LOD1", "LOD2" };
            foreach (RoleConfig role in Roles)
            {
                foreach (string part in parts)
                {
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(role, part)) == null)
                    {
                        return false;
                    }
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Infantry_Palette.png") != null;
        }

        private static void BuildAll(bool selectResult)
        {
            if (!AssetsReady())
            {
                Debug.LogWarning("Bastion Infantry: model imports are not ready. Re-run the prefab command after Unity finishes importing.");
                return;
            }

            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            Material material = CreateOrUpdateMaterial();
            DetailMaterialSet detailMaterials = CreateOrUpdateDetailMaterials();
            GameObject lastPrefab = null;

            foreach (RoleConfig role in Roles)
            {
                BuildRole(role, material, detailMaterials);
                lastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(role));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (selectResult && lastPrefab != null)
            {
                Selection.activeObject = lastPrefab;
                EditorGUIUtility.PingObject(lastPrefab);
            }

            Debug.Log("Bastion Infantry: created five prefabs in " + Prefabs);
        }

        private static void BuildRole(RoleConfig role, Material material, DetailMaterialSet detailMaterials)
        {
            GameObject root = new GameObject("Bastion_" + role.Slug);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject lod0Rig = NewChild("LOD0_Rig", root.transform, Vector3.zero);
            GameObject pelvisPivot = NewChild("PelvisPivot", lod0Rig.transform, new Vector3(0f, 1.02f, 0f));
            GameObject pelvisModel = InstantiateModel("PelvisModel", ModelPath(role, "Pelvis"), pelvisPivot.transform, Vector3.zero, material);

            GameObject leftLegPivot = NewChild("LeftLegPivot", pelvisPivot.transform, new Vector3(-0.16f, -0.02f, 0f));
            GameObject leftLegModel = InstantiateModel("LeftLegModel", ModelPath(role, "LeftLeg"), leftLegPivot.transform, Vector3.zero, material);
            GameObject rightLegPivot = NewChild("RightLegPivot", pelvisPivot.transform, new Vector3(0.16f, -0.02f, 0f));
            GameObject rightLegModel = InstantiateModel("RightLegModel", ModelPath(role, "RightLeg"), rightLegPivot.transform, Vector3.zero, material);

            GameObject upperBodyYaw = NewChild("UpperBodyYaw", pelvisPivot.transform, new Vector3(0f, 0.25f, 0f));
            GameObject torsoModel = InstantiateModel("TorsoModel", ModelPath(role, "Torso"), upperBodyYaw.transform, Vector3.zero, material);
            GameObject gearModel = InstantiateModel("GearModel", ModelPath(role, "Gear"), upperBodyYaw.transform, Vector3.zero, material);

            GameObject headYaw = NewChild("HeadYaw", upperBodyYaw.transform, new Vector3(0f, 0.43f, 0.015f));
            GameObject headModel = InstantiateModel("HeadModel", ModelPath(role, "Head"), headYaw.transform, Vector3.zero, material);

            GameObject weaponPitch = NewChild("WeaponPitch", upperBodyYaw.transform, new Vector3(0f, 0.10f, 0.03f));
            GameObject armsModel = InstantiateModel("ArmsModel", ModelPath(role, "Arms"), weaponPitch.transform, Vector3.zero, material);
            GameObject weaponRecoil = NewChild("WeaponRecoil", weaponPitch.transform, Vector3.zero);
            GameObject weaponModel = InstantiateModel("WeaponModel", ModelPath(role, "Weapon"), weaponRecoil.transform, Vector3.zero, material);
            Transform muzzle = NewAnchor(role.Role == BastionInfantryRole.Engineer ? "ToolTip" : "Muzzle", weaponRecoil.transform, role.Muzzle);
            AddProfessionalDetailKit(role, lod0Rig.transform, detailMaterials);

            Transform healthBarAnchor = NewAnchor("HealthBarAnchor", root.transform, new Vector3(0f, 2.08f, 0f));
            Transform selectionAnchor = NewAnchor("SelectionAnchor", root.transform, new Vector3(0f, 0.02f, 0f));
            Transform aimCenter = NewAnchor("AimCenter", root.transform, new Vector3(0f, 1.35f, 0f));

            GameObject lod1 = InstantiateModel("LOD1", ModelPath(role, "LOD1"), root.transform, Vector3.zero, material);
            GameObject lod2 = InstantiateModel("LOD2", ModelPath(role, "LOD2"), root.transform, Vector3.zero, material);
            ConfigureLODGroup(root, lod0Rig, lod1, lod2);

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.92f, 0f);
            collider.height = 1.84f;
            collider.radius = role.Role == BastionInfantryRole.FlameTrooper ? 0.32f : 0.28f;
            collider.direction = 1;

            BastionInfantryProceduralAnimator procedural = root.AddComponent<BastionInfantryProceduralAnimator>();
            SerializedObject proceduralSerialized = new SerializedObject(procedural);
            proceduralSerialized.FindProperty("pelvisPivot").objectReferenceValue = pelvisPivot.transform;
            proceduralSerialized.FindProperty("leftLegPivot").objectReferenceValue = leftLegPivot.transform;
            proceduralSerialized.FindProperty("rightLegPivot").objectReferenceValue = rightLegPivot.transform;
            proceduralSerialized.FindProperty("weaponRecoil").objectReferenceValue = weaponRecoil.transform;
            proceduralSerialized.FindProperty("headYaw").objectReferenceValue = headYaw.transform;
            proceduralSerialized.ApplyModifiedPropertiesWithoutUndo();

            BastionInfantryUnit unit = root.AddComponent<BastionInfantryUnit>();
            SerializedObject serialized = new SerializedObject(unit);
            serialized.FindProperty("role").enumValueIndex = (int)role.Role;
            serialized.FindProperty("weaponDelivery").enumValueIndex = (int)role.Delivery;
            serialized.FindProperty("upperBodyYaw").objectReferenceValue = upperBodyYaw.transform;
            serialized.FindProperty("weaponPitch").objectReferenceValue = weaponPitch.transform;
            serialized.FindProperty("weaponRecoil").objectReferenceValue = weaponRecoil.transform;
            serialized.FindProperty("muzzle").objectReferenceValue = muzzle;
            serialized.FindProperty("healthBarAnchor").objectReferenceValue = healthBarAnchor;
            serialized.FindProperty("selectionAnchor").objectReferenceValue = selectionAnchor;
            serialized.FindProperty("maxHealth").floatValue = role.MaxHealth;
            serialized.FindProperty("moveSpeed").floatValue = role.MoveSpeed;
            serialized.FindProperty("turnSpeed").floatValue = role.TurnSpeed;
            serialized.FindProperty("attackRange").floatValue = role.AttackRange;
            serialized.FindProperty("actionCooldown").floatValue = role.Cooldown;
            serialized.FindProperty("damage").floatValue = role.Damage;
            serialized.FindProperty("splashRadius").floatValue = role.SplashRadius;
            serialized.FindProperty("projectileSpeed").floatValue = role.ProjectileSpeed;
            serialized.FindProperty("utilityPower").floatValue = role.UtilityPower;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(role));
            UnityEngine.Object.DestroyImmediate(root);
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

        private static void AddProfessionalDetailKit(RoleConfig role, Transform lod0Rig, DetailMaterialSet materials)
        {
            GameObject kit = NewChild("LOD0_ProfessionalDetailKit", lod0Rig, Vector3.zero);
            kit.AddComponent<RtsPreserveImportedMaterial>();

            AddBox(kit.transform, "Layered Chest Upper Plate", new Vector3(0f, 1.47f, 0.165f), new Vector3(0.42f, 0.055f, 0.035f), materials.Plate);
            AddBox(kit.transform, "Layered Chest Lower Plate", new Vector3(0f, 1.31f, 0.185f), new Vector3(0.36f, 0.055f, 0.035f), materials.Plate);
            AddBox(kit.transform, "Dark Chest Recess", new Vector3(0f, 1.39f, 0.202f), new Vector3(0.28f, 0.15f, 0.022f), materials.Dark);
            AddBox(kit.transform, "Collar Neck Seal", new Vector3(0f, 1.58f, 0.075f), new Vector3(0.34f, 0.05f, 0.12f), materials.Webbing);
            AddBox(kit.transform, "Belt Webbing", new Vector3(0f, 1.10f, 0.14f), new Vector3(0.42f, 0.04f, 0.06f), materials.Webbing);
            AddBox(kit.transform, "Belt Buckle Worn Metal", new Vector3(0f, 1.105f, 0.195f), new Vector3(0.095f, 0.05f, 0.025f), materials.Metal);

            AddBox(kit.transform, "Left Shoulder Micro Armor", new Vector3(-0.35f, 1.48f, 0.08f), new Vector3(0.13f, 0.055f, 0.09f), materials.Plate);
            AddBox(kit.transform, "Right Shoulder Micro Armor", new Vector3(0.35f, 1.48f, 0.08f), new Vector3(0.13f, 0.055f, 0.09f), materials.Plate);
            AddBox(kit.transform, "Left Forearm Wear Strip", new Vector3(-0.41f, 1.25f, 0.28f), new Vector3(0.055f, 0.12f, 0.035f), materials.Metal);
            AddBox(kit.transform, "Right Forearm Wear Strip", new Vector3(0.41f, 1.25f, 0.28f), new Vector3(0.055f, 0.12f, 0.035f), materials.Metal);

            AddBox(kit.transform, "Helmet Visor Lower Frame", new Vector3(0f, 1.82f, 0.255f), new Vector3(0.23f, 0.018f, 0.018f), materials.Dark);
            AddBox(kit.transform, "Helmet Side Plate Left", new Vector3(-0.17f, 1.84f, 0.10f), new Vector3(0.045f, 0.09f, 0.045f), materials.Plate);
            AddBox(kit.transform, "Helmet Side Plate Right", new Vector3(0.17f, 1.84f, 0.10f), new Vector3(0.045f, 0.09f, 0.045f), materials.Plate);
            AddBox(kit.transform, "Helmet Cyan Optic Slit", new Vector3(0f, 1.825f, 0.282f), new Vector3(0.15f, 0.025f, 0.014f), materials.Cyan);
            AddCylinder(kit.transform, "Micro Radio Whip", new Vector3(-0.26f, 1.92f, -0.08f), new Vector3(0.01f, 0.26f, 0.01f), Quaternion.identity, materials.Dark);

            AddBox(kit.transform, "Left Thigh Utility Pouch", new Vector3(-0.22f, 0.88f, 0.10f), new Vector3(0.085f, 0.14f, 0.055f), materials.Canvas);
            AddBox(kit.transform, "Right Thigh Utility Pouch", new Vector3(0.22f, 0.88f, 0.10f), new Vector3(0.085f, 0.14f, 0.055f), materials.Canvas);
            AddBox(kit.transform, "Left Knee Weathered Plate", new Vector3(-0.21f, 0.64f, 0.13f), new Vector3(0.12f, 0.07f, 0.04f), materials.Plate);
            AddBox(kit.transform, "Right Knee Weathered Plate", new Vector3(0.21f, 0.64f, 0.13f), new Vector3(0.12f, 0.07f, 0.04f), materials.Plate);
            AddBox(kit.transform, "Left Boot Worn Toe", new Vector3(-0.22f, 0.27f, 0.19f), new Vector3(0.16f, 0.035f, 0.055f), materials.Metal);
            AddBox(kit.transform, "Right Boot Worn Toe", new Vector3(0.22f, 0.27f, 0.19f), new Vector3(0.16f, 0.035f, 0.055f), materials.Metal);

            switch (role.Role)
            {
                case BastionInfantryRole.RocketSoldier:
                    AddCylinder(kit.transform, "Rocket Tube Rear Soot Sleeve", new Vector3(0.25f, 1.48f, 0.20f), new Vector3(0.07f, 0.09f, 0.07f), Quaternion.Euler(90f, 0f, 0f), materials.Dark);
                    AddBox(kit.transform, "Rocket Tube Clamp Front", new Vector3(0.25f, 1.55f, 0.74f), new Vector3(0.09f, 0.035f, 0.045f), materials.Metal);
                    AddBox(kit.transform, "Rocket Warning Plate", new Vector3(0.25f, 1.58f, 0.92f), new Vector3(0.08f, 0.024f, 0.08f), materials.Hazard);
                    break;
                case BastionInfantryRole.Grenadier:
                    AddBox(kit.transform, "Grenadier Bandolier Dark Strap", new Vector3(-0.10f, 1.43f, 0.222f), new Vector3(0.045f, 0.34f, 0.02f), materials.Webbing);
                    for (int i = 0; i < 4; i++)
                    {
                        AddCylinder(kit.transform, "Grenadier Micro Grenade " + i, new Vector3(-0.18f + i * 0.12f, 1.40f, 0.245f), new Vector3(0.028f, 0.035f, 0.028f), Quaternion.identity, materials.Metal);
                    }
                    break;
                case BastionInfantryRole.FlameTrooper:
                    AddBox(kit.transform, "Flame Apron Scorched Plate", new Vector3(0f, 1.22f, 0.218f), new Vector3(0.32f, 0.22f, 0.03f), materials.Metal);
                    AddCylinder(kit.transform, "Left Fuel Tank Band", new Vector3(-0.13f, 1.45f, -0.32f), new Vector3(0.062f, 0.022f, 0.062f), Quaternion.identity, materials.Hazard);
                    AddCylinder(kit.transform, "Right Fuel Tank Band", new Vector3(0.13f, 1.45f, -0.32f), new Vector3(0.062f, 0.022f, 0.062f), Quaternion.identity, materials.Hazard);
                    AddBox(kit.transform, "Flame Nozzle Heat Shield", new Vector3(0.35f, 1.32f, 0.78f), new Vector3(0.11f, 0.045f, 0.13f), materials.Metal);
                    break;
                case BastionInfantryRole.Engineer:
                    AddBox(kit.transform, "Engineer Tool Case Face", new Vector3(-0.34f, 1.05f, 0.10f), new Vector3(0.16f, 0.20f, 0.045f), materials.Canvas);
                    AddBox(kit.transform, "Engineer Tool Case Metal Latch", new Vector3(-0.34f, 1.08f, 0.132f), new Vector3(0.07f, 0.026f, 0.018f), materials.Metal);
                    AddBox(kit.transform, "Engineer Tablet Lit Screen", new Vector3(0.24f, 1.17f, 0.285f), new Vector3(0.13f, 0.075f, 0.018f), materials.Cyan);
                    AddBox(kit.transform, "Engineer Helmet Work Light", new Vector3(0f, 1.99f, 0.235f), new Vector3(0.08f, 0.035f, 0.018f), materials.Cyan);
                    break;
                default:
                    AddBox(kit.transform, "Gunner Rifle Receiver Wear", new Vector3(0.30f, 1.43f, 0.50f), new Vector3(0.045f, 0.025f, 0.20f), materials.Metal);
                    AddBox(kit.transform, "Gunner Spare Magazine", new Vector3(-0.10f, 1.12f, 0.18f), new Vector3(0.075f, 0.16f, 0.045f), materials.Dark);
                    break;
            }
        }

        private static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = localScale;
            AssignDetailMaterial(box, material);
            return box;
        }

        private static GameObject AddCylinder(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = localRotation;
            cylinder.transform.localScale = localScale;
            AssignDetailMaterial(cylinder, material);
            return cylinder;
        }

        private static void AssignDetailMaterial(GameObject detail, Material material)
        {
            Collider collider = detail.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = detail.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void ConfigureLODGroup(GameObject root, GameObject lod0Root, GameObject lod1Root, GameObject lod2Root)
        {
            Renderer[] lod0 = lod0Root.GetComponentsInChildren<Renderer>(true);
            Renderer[] lod1 = lod1Root.GetComponentsInChildren<Renderer>(true);
            Renderer[] lod2 = lod2Root.GetComponentsInChildren<Renderer>(true);
            LODGroup group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = false;
            LOD high = new LOD(0.30f, lod0);
            high.fadeTransitionWidth = 0.07f;
            LOD medium = new LOD(0.105f, lod1);
            medium.fadeTransitionWidth = 0.08f;
            LOD low = new LOD(0.028f, lod2);
            low.fadeTransitionWidth = 0.10f;
            LOD culled = new LOD(0.006f, new Renderer[0]);
            group.SetLODs(new[] { high, medium, low, culled });
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
                throw new InvalidOperationException("Bastion Infantry: neither URP/Lit nor Standard shader could be found.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = "Bastion_Infantry_Palette" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Infantry_Palette.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_Infantry_Emission.png");
            SetTextureIfPresent(material, "_BaseMap", palette);
            SetTextureIfPresent(material, "_MainTex", palette);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Metallic", 0.10f);
            SetFloatIfPresent(material, "_Smoothness", 0.34f);
            SetFloatIfPresent(material, "_Glossiness", 0.34f);

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

        private static DetailMaterialSet CreateOrUpdateDetailMaterials()
        {
            EnsureFolder(Textures);
            EnsureFolder(Materials);

            return new DetailMaterialSet
            {
                Plate = CreateOrUpdateDetailMaterial("Plate", new Color(0.62f, 0.61f, 0.52f), new Color(0.18f, 0.17f, 0.12f), 211, 0.08f, 0.26f, false),
                Canvas = CreateOrUpdateDetailMaterial("Canvas", new Color(0.34f, 0.31f, 0.23f), new Color(0.11f, 0.08f, 0.045f), 223, 0.03f, 0.18f, false),
                Webbing = CreateOrUpdateDetailMaterial("Webbing", new Color(0.075f, 0.08f, 0.065f), new Color(0.025f, 0.025f, 0.018f), 227, 0.04f, 0.2f, false),
                Metal = CreateOrUpdateDetailMaterial("Metal", new Color(0.40f, 0.40f, 0.36f), new Color(0.10f, 0.10f, 0.085f), 229, 0.34f, 0.36f, false),
                Dark = CreateOrUpdateDetailMaterial("Dark", new Color(0.055f, 0.06f, 0.055f), new Color(0.015f, 0.016f, 0.014f), 233, 0.08f, 0.24f, false),
                Hazard = CreateOrUpdateDetailMaterial("Hazard", new Color(0.82f, 0.38f, 0.08f), new Color(0.05f, 0.045f, 0.035f), 239, 0.08f, 0.28f, false),
                Cyan = CreateOrUpdateDetailMaterial("Cyan", new Color(0.08f, 0.42f, 0.48f), new Color(0.02f, 0.10f, 0.12f), 241, 0.02f, 0.45f, true),
                Amber = CreateOrUpdateDetailMaterial("Amber", new Color(0.95f, 0.48f, 0.08f), new Color(0.18f, 0.06f, 0.02f), 251, 0.02f, 0.42f, true)
            };
        }

        private static Material CreateOrUpdateDetailMaterial(string name, Color baseColor, Color grimeColor, int seed, float metallic, float smoothness, bool emissive)
        {
            string path = DetailMaterialPrefix + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                throw new InvalidOperationException("Bastion Infantry: neither URP/Lit nor Standard shader could be found.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = "Bastion_Infantry_Detail_" + name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D texture = CreateOrUpdateDetailTexture(name, baseColor, grimeColor, seed);
            SetTextureIfPresent(material, "_BaseMap", texture);
            SetTextureIfPresent(material, "_MainTex", texture);
            SetColorIfPresent(material, "_BaseColor", baseColor);
            SetColorIfPresent(material, "_Color", baseColor);
            SetFloatIfPresent(material, "_Metallic", metallic);
            SetFloatIfPresent(material, "_Smoothness", smoothness);
            SetFloatIfPresent(material, "_Glossiness", smoothness);

            if (emissive)
            {
                SetTextureIfPresent(material, "_EmissionMap", texture);
                SetColorIfPresent(material, "_EmissionColor", baseColor * 1.4f);
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                SetColorIfPresent(material, "_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreateOrUpdateDetailTexture(string name, Color baseColor, Color grimeColor, int seed)
        {
            string path = DetailTexturePrefix + name + ".asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(128, 128, TextureFormat.RGBA32, true) { name = "Bastion_Infantry_Detail_" + name };
                AssetDatabase.CreateAsset(texture, path);
            }

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float coarse = Mathf.PerlinNoise((x + seed * 13) * 0.06f, (y + seed * 7) * 0.07f);
                    float fine = Mathf.PerlinNoise((x + seed * 3) * 0.32f, (y + seed * 5) * 0.29f);
                    float shade = 0.84f + coarse * 0.22f - fine * 0.06f;
                    Color color = baseColor * shade;
                    color.a = 1f;

                    bool scratch = ((x * 17 + y * 31 + seed * 11) % 149) < 2;
                    if (scratch)
                    {
                        color = Color.Lerp(color, Color.white, 0.12f);
                    }

                    if (coarse > 0.70f)
                    {
                        color = Color.Lerp(color, grimeColor, (coarse - 0.70f) * 0.7f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true, false);
            EditorUtility.SetDirty(texture);
            return texture;
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
