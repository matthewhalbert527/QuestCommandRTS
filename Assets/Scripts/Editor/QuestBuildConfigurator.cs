#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace QuestCommandRTS.Editor
{
    public static class RtsEditorTools
    {
        private const string ScenePath = "Assets/Scenes/Battlefield.unity";

        [MenuItem("Command RTS/Open Battlefield Scene")]
        public static void OpenBattlefieldScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    public static class QuestXrProjectValidator
    {
        private const string AndroidPackageId = "com.matthewhalbert.questcommandrts";
        private const string ManifestPath = "Packages/manifest.json";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string XrGeneralSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        private const string OpenXrLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string MetaQuestFeatureId = "com.unity.openxr.feature.metaquest";
        private const string OculusTouchFeatureId = "com.unity.openxr.feature.input.oculustouch";

        [MenuItem("Tools/Quest RTS/Apply Recommended Quest Settings")]
        public static void ApplyRecommendedQuestSettings()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidPackageId);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)0;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            EnsureActiveInputHandlingBoth();
            ConfigureOpenXrForBuildTarget(BuildTargetGroup.Standalone);
            ConfigureOpenXrForBuildTarget(BuildTargetGroup.Android);
            SetOpenXrFeature(BuildTargetGroup.Android, MetaQuestFeatureId, true);
            SetOpenXrFeature(BuildTargetGroup.Android, OculusTouchFeatureId, true);
            SetOpenXrFeature(BuildTargetGroup.Standalone, OculusTouchFeatureId, true);

            AssetDatabase.SaveAssets();
            Debug.Log("Quest RTS recommended Android/OpenXR player settings applied. Run Tools/Quest RTS/Validate XR Setup before headset testing.");
        }

        [MenuItem("Tools/Quest RTS/Validate XR Setup")]
        public static void ValidateXrSetup()
        {
            List<ValidationItem> results = BuildValidationReport();
            bool hasFailure = false;

            for (int i = 0; i < results.Count; i++)
            {
                ValidationItem item = results[i];
                string message = item.Label + ": " + item.Detail;
                if (item.Passed)
                {
                    Debug.Log("[Quest RTS XR] PASS - " + message);
                }
                else if (item.WarningOnly)
                {
                    Debug.LogWarning("[Quest RTS XR] MANUAL - " + message);
                }
                else
                {
                    hasFailure = true;
                    Debug.LogError("[Quest RTS XR] FAIL - " + message);
                }
            }

            if (hasFailure)
            {
                Debug.LogError("[Quest RTS XR] Validation found blocking setup issues. Use Tools/Quest RTS/Apply Recommended Quest Settings and then verify docs/QUEST_XR_SETUP.md.");
            }
            else
            {
                Debug.Log("[Quest RTS XR] Validation completed. Manual OpenXR headset checks are still listed in docs/QUEST_XR_SETUP.md.");
            }
        }

        internal static List<ValidationItem> BuildValidationReport()
        {
            List<ValidationItem> results = new List<ValidationItem>();
            string manifest = File.Exists(ManifestPath) ? File.ReadAllText(ManifestPath) : string.Empty;
            string projectSettings = File.Exists(ProjectSettingsPath) ? File.ReadAllText(ProjectSettingsPath) : string.Empty;

            Add(results, "Unity version", Application.unityVersion.StartsWith("2022.3.62f3"), Application.unityVersion);
            Add(results, "XR Management package", manifest.Contains("\"com.unity.xr.management\": \"4.5.4\""), "manifest should include com.unity.xr.management 4.5.4");
            Add(results, "OpenXR package", manifest.Contains("\"com.unity.xr.openxr\": \"1.15.1\""), "manifest should include com.unity.xr.openxr 1.15.1");
            Add(results, "XR Interaction Toolkit package", manifest.Contains("\"com.unity.xr.interaction.toolkit\": \"3.2.2\""), "manifest should include com.unity.xr.interaction.toolkit 3.2.2");
            Add(results, "Input System package", manifest.Contains("\"com.unity.inputsystem\": \"1.18.0\""), "manifest should include com.unity.inputsystem 1.18.0");
            results.Add(CreateResolvedPackageValidationItem("Resolved XR Management package", "com.unity.xr.management", "4.5.4"));
            results.Add(CreateResolvedPackageValidationItem("Resolved OpenXR package", "com.unity.xr.openxr", "1.15.1"));
            results.Add(CreateResolvedPackageValidationItem("Resolved XR Interaction Toolkit package", "com.unity.xr.interaction.toolkit", "3.2.2"));
            results.Add(CreateResolvedPackageValidationItem("Resolved Input System package", "com.unity.inputsystem", "1.18.0"));
            results.Add(CreateForbiddenXrPackagesValidationItem(manifest));
            results.Add(CreateAndroidBuildSupportValidationItem(IsAndroidBuildTargetSupported()));
            Add(results, "Android min API", PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel29, PlayerSettings.Android.minSdkVersion.ToString());
            Add(results, "Android target API", (int)PlayerSettings.Android.targetSdkVersion == 0, "Automatic/highest installed target expected");
            Add(results, "Android scripting backend", PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android).ToString());
            Add(results, "Android architecture", PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures.ToString());
            Add(results, "Android package id", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) == AndroidPackageId, PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
            Add(results, "Active input handling", projectSettings.Contains("activeInputHandler: 2"), "ProjectSettings should contain activeInputHandler: 2 (Both)");
            Add(results, "Android multithreaded rendering", PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android), PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android).ToString());
            Add(results, "Android graphics API", HasPrimaryGraphicsApi(GraphicsDeviceType.Vulkan), "Vulkan should be first Android graphics API");
            Add(results, "Standalone OpenXR loader", IsOpenXrLoaderAssigned(BuildTargetGroup.Standalone), "Standalone should have OpenXR assigned for Quest Link testing");
            Add(results, "Android OpenXR loader", IsOpenXrLoaderAssigned(BuildTargetGroup.Android), "Android should have OpenXR assigned");
            Add(results, "Standalone Single Pass Instanced", IsSinglePassInstanced(BuildTargetGroup.Standalone), "OpenXR render mode should be Single Pass Instanced");
            if (OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android) != null)
            {
                Add(results, "Android Single Pass Instanced", IsSinglePassInstanced(BuildTargetGroup.Android), "OpenXR render mode should be Single Pass Instanced");
                Add(results, "Android Meta Quest Support", IsOpenXrFeatureEnabled(BuildTargetGroup.Android, MetaQuestFeatureId), "Meta Quest Support feature should be enabled");
                Add(results, "Android Oculus Touch profile", IsOpenXrFeatureEnabled(BuildTargetGroup.Android, OculusTouchFeatureId), "Oculus Touch Controller Profile should be enabled");
            }
            else
            {
                AddManual(results, "Android OpenXR package settings", "Unity did not create Android OpenXR package settings in this editor session. Install Android Build Support, switch to Android, rerun Apply Recommended Quest Settings, and verify Single Pass Instanced, Meta Quest Support, and Oculus Touch.");
            }

            Add(results, "Standalone Oculus Touch profile", IsOpenXrFeatureEnabled(BuildTargetGroup.Standalone, OculusTouchFeatureId), "Oculus Touch Controller Profile should be enabled for Quest Link");

            AddManual(results, "Headset setup", "Developer mode, USB debugging, and Quest Link/device build testing require manual headset verification.");
            return results;
        }

        internal static ValidationItem CreateAndroidBuildSupportValidationItem(bool buildTargetSupported)
        {
            return new ValidationItem(
                "Android Build Support",
                buildTargetSupported,
                false,
                buildTargetSupported
                    ? "BuildTarget.Android is supported by this Unity editor install."
                    : "Android Build Support is not installed for this Unity editor. Install Android Build Support, SDK and NDK Tools, and OpenJDK for Unity 2022.3.62f3 in Unity Hub before Quest device builds.");
        }

        internal static ValidationItem CreateForbiddenXrPackagesValidationItem(string manifest)
        {
            string text = manifest ?? string.Empty;
            bool hasForbiddenPackage =
                text.Contains("\"com.meta.xr") ||
                text.Contains("\"com.unity.xr.meta-openxr\"");

            return new ValidationItem(
                "Forbidden XR packages absent",
                !hasForbiddenPackage,
                false,
                hasForbiddenPackage
                    ? "Remove Meta XR SDK/All-in-One, Meta Avatars, passthrough/hand-tracking packages, or com.unity.xr.meta-openxr packages for this milestone."
                    : "Manifest uses Unity XR Management, OpenXR, XR Interaction Toolkit, and Input System without forbidden Meta XR SDK packages.");
        }

        internal static ValidationItem CreateResolvedPackageValidationItem(string label, string packageName, string expectedVersion)
        {
            UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            string resolvedVersion = info != null ? info.version : "missing";
            return new ValidationItem(
                label,
                info != null && resolvedVersion == expectedVersion,
                false,
                packageName + " resolved version " + resolvedVersion + ", expected " + expectedVersion + ".");
        }

        private static bool IsAndroidBuildTargetSupported()
        {
            return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
        }

        private static bool HasPrimaryGraphicsApi(GraphicsDeviceType expected)
        {
            GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            return apis != null && apis.Length > 0 && apis[0] == expected;
        }

        private static void ConfigureOpenXrForBuildTarget(BuildTargetGroup group)
        {
            XRManagerSettings managerSettings = GetOrCreateManagerSettings(group);
            if (managerSettings != null)
            {
                XRPackageMetadataStore.AssignLoader(managerSettings, OpenXrLoaderTypeName, group);
            }

            OpenXRSettings openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (openXrSettings != null)
            {
                openXrSettings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                EditorUtility.SetDirty(openXrSettings);
            }
        }

        private static XRManagerSettings GetOrCreateManagerSettings(BuildTargetGroup group)
        {
            XRGeneralSettingsPerBuildTarget buildTargetSettings = GetOrCreateXrBuildTargetSettings();
            if (!buildTargetSettings.HasSettingsForBuildTarget(group))
            {
                buildTargetSettings.CreateDefaultSettingsForBuildTarget(group);
            }

            if (!buildTargetSettings.HasManagerSettingsForBuildTarget(group))
            {
                buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(group);
            }

            return buildTargetSettings.ManagerSettingsForBuildTarget(group);
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateXrBuildTargetSettings()
        {
            XRGeneralSettingsPerBuildTarget buildTargetSettings;
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings) && buildTargetSettings != null)
            {
                return buildTargetSettings;
            }

            string[] assets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (assets.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if (buildTargetSettings != null)
                {
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
                    return buildTargetSettings;
                }
            }

            EnsureFolder("Assets/XR");
            buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(buildTargetSettings, XrGeneralSettingsPath);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            AssetDatabase.SaveAssets();
            return buildTargetSettings;
        }

        private static bool IsOpenXrLoaderAssigned(BuildTargetGroup group)
        {
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            XRManagerSettings manager = settings != null ? settings.Manager : null;
            if (manager == null)
            {
                return false;
            }

            IReadOnlyList<XRLoader> loaders = manager.activeLoaders;
            for (int i = 0; i < loaders.Count; i++)
            {
                XRLoader loader = loaders[i];
                if (loader != null && loader.GetType().FullName == OpenXrLoaderTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSinglePassInstanced(BuildTargetGroup group)
        {
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            return settings != null && settings.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced;
        }

        private static bool SetOpenXrFeature(BuildTargetGroup group, string featureId, bool enabled)
        {
            FeatureHelpers.RefreshFeatures(group);
            OpenXRFeature feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(group, featureId);
            if (feature == null)
            {
                return false;
            }

            feature.enabled = enabled;
            EditorUtility.SetDirty(feature);
            return true;
        }

        private static bool IsOpenXrFeatureEnabled(BuildTargetGroup group, string featureId)
        {
            FeatureHelpers.RefreshFeatures(group);
            OpenXRFeature feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(group, featureId);
            return feature != null && feature.enabled;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
            string name = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureActiveInputHandlingBoth()
        {
            if (!File.Exists(ProjectSettingsPath))
            {
                return;
            }

            string text = File.ReadAllText(ProjectSettingsPath);
            string updated = text.Replace("activeInputHandler: 0", "activeInputHandler: 2").Replace("activeInputHandler: 1", "activeInputHandler: 2");
            if (updated != text)
            {
                File.WriteAllText(ProjectSettingsPath, updated);
                AssetDatabase.Refresh();
            }
        }

        private static void Add(List<ValidationItem> results, string label, bool passed, string detail)
        {
            results.Add(new ValidationItem(label, passed, false, detail));
        }

        private static void AddManual(List<ValidationItem> results, string label, string detail)
        {
            results.Add(new ValidationItem(label, false, true, detail));
        }

        internal struct ValidationItem
        {
            public readonly string Label;
            public readonly bool Passed;
            public readonly bool WarningOnly;
            public readonly string Detail;

            public ValidationItem(string label, bool passed, bool warningOnly, string detail)
            {
                Label = label;
                Passed = passed;
                WarningOnly = warningOnly;
                Detail = detail;
            }
        }
    }
}
#endif
