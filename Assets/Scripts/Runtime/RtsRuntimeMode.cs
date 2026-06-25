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

            RtsRuntimeMode commandLineMode;
            if (TryGetCommandLineMode(out commandLineMode))
            {
                return commandLineMode;
            }

            RtsRuntimeMode environmentMode;
            if (TryParseMode(Environment.GetEnvironmentVariable(ForceModeEnvironmentVariable), out environmentMode))
            {
                return environmentMode;
            }

            return IsXrRuntimeActive() ? RtsRuntimeMode.QuestVr : RtsRuntimeMode.Desktop;
        }

        public static bool IsXrRuntimeActive()
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            if (settings != null && settings.Manager != null && settings.Manager.activeLoader != null)
            {
                return true;
            }

            return XRSettings.enabled && XRSettings.isDeviceActive;
        }

        private static bool TryGetCommandLineMode(out RtsRuntimeMode mode)
        {
            string[] arguments = Environment.GetCommandLineArgs();
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
