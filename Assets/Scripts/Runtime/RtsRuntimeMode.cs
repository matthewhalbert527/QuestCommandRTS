using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace QuestCommandRTS
{
    public enum RtsRuntimeMode
    {
        Desktop,
        QuestVr
    }

    public static class RtsRuntimeModeResolver
    {
        public const string ForceModeCommandLineArgument = "-questRtsMode";
        public const string ForceModeEnvironmentVariable = "QUEST_RTS_FORCE_MODE";

        private static RtsRuntimeMode? forcedMode;

        public static bool HasForcedMode => forcedMode.HasValue;

        public static void ForceModeForTests(RtsRuntimeMode? mode)
        {
            forcedMode = mode;
        }

        public static RtsRuntimeMode Resolve()
        {
            if (forcedMode.HasValue)
            {
                return forcedMode.Value;
            }

            XRGeneralSettings settings = XRGeneralSettings.Instance;
            bool hasOpenXrLoader = HasOpenXrLoader(settings);
            return ResolveFromState(
                Environment.GetCommandLineArgs(),
                Environment.GetEnvironmentVariable(ForceModeEnvironmentVariable),
                XRSettings.enabled,
                XRSettings.isDeviceActive,
                hasOpenXrLoader,
                Application.platform);
        }

        public static bool IsXrRuntimeActive()
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            bool hasOpenXrLoader = HasOpenXrLoader(settings);
            return IsXrRuntimeActiveForState(XRSettings.enabled, XRSettings.isDeviceActive, hasOpenXrLoader, Application.platform);
        }

        internal static bool IsXrRuntimeActiveForState(bool xrSettingsEnabled, bool xrDeviceActive, bool hasOpenXrLoader, RuntimePlatform platform)
        {
            if (xrDeviceActive)
            {
                return true;
            }

            if (!xrSettingsEnabled || !hasOpenXrLoader)
            {
                return false;
            }

            return platform == RuntimePlatform.Android;
        }

#if UNITY_EDITOR
        public static bool EvaluateXrRuntimeStateForTests(bool xrSettingsEnabled, bool xrDeviceActive, bool hasOpenXrLoader, RuntimePlatform platform)
        {
            return IsXrRuntimeActiveForState(xrSettingsEnabled, xrDeviceActive, hasOpenXrLoader, platform);
        }

        public static RtsRuntimeMode ResolveFromStateForTests(string[] arguments, string environmentMode, bool xrSettingsEnabled, bool xrDeviceActive, bool hasOpenXrLoader, RuntimePlatform platform)
        {
            return ResolveFromState(arguments, environmentMode, xrSettingsEnabled, xrDeviceActive, hasOpenXrLoader, platform);
        }
#endif

        private static RtsRuntimeMode ResolveFromState(string[] arguments, string environmentValue, bool xrSettingsEnabled, bool xrDeviceActive, bool hasOpenXrLoader, RuntimePlatform platform)
        {
            RtsRuntimeMode commandLineMode;
            if (TryGetCommandLineMode(arguments, out commandLineMode))
            {
                return commandLineMode;
            }

            RtsRuntimeMode environmentMode;
            if (TryParseMode(environmentValue, out environmentMode))
            {
                return environmentMode;
            }

            return IsXrRuntimeActiveForState(xrSettingsEnabled, xrDeviceActive, hasOpenXrLoader, platform) ? RtsRuntimeMode.QuestVr : RtsRuntimeMode.Desktop;
        }

        private static bool HasOpenXrLoader(XRGeneralSettings settings)
        {
            XRLoader loader = settings != null && settings.Manager != null ? settings.Manager.activeLoader : null;
            return loader != null && loader.GetType().FullName == "UnityEngine.XR.OpenXR.OpenXRLoader";
        }

        private static bool TryGetCommandLineMode(string[] arguments, out RtsRuntimeMode mode)
        {
            if (arguments == null)
            {
                mode = RtsRuntimeMode.Desktop;
                return false;
            }

            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], ForceModeCommandLineArgument, StringComparison.OrdinalIgnoreCase) &&
                    TryParseMode(arguments[i + 1], out mode))
                {
                    return true;
                }
            }

            mode = RtsRuntimeMode.Desktop;
            return false;
        }

        private static bool TryParseMode(string value, out RtsRuntimeMode mode)
        {
            if (string.Equals(value, "QuestVr", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Quest", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "VR", StringComparison.OrdinalIgnoreCase))
            {
                mode = RtsRuntimeMode.QuestVr;
                return true;
            }

            if (string.Equals(value, "Desktop", StringComparison.OrdinalIgnoreCase))
            {
                mode = RtsRuntimeMode.Desktop;
                return true;
            }

            mode = RtsRuntimeMode.Desktop;
            return false;
        }
    }
}
