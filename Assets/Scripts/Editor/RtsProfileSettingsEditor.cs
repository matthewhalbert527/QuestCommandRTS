#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class RtsProfileSettingsEditor
    {
        private const string ProfileFileName = "profile-settings.json";

        [MenuItem("Command RTS/Profile/Use Default Quest Tabletop Scale")]
        public static void UseDefaultQuestTabletopScale()
        {
            WriteQuestTabletopProfile(RtsProfileSettingsData.DefaultTabletopScale, RtsProfileSettingsData.DefaultPointerLength, "default");
        }

        [MenuItem("Command RTS/Profile/Use Room-Sized Quest Tabletop Scale")]
        public static void UseRoomSizedQuestTabletopScale()
        {
            WriteQuestTabletopProfile(RtsProfileSettingsData.RoomSizedTabletopScale, RtsProfileSettingsData.RoomSizedPointerLength, "room-sized");
        }

        private static void WriteQuestTabletopProfile(float tabletopScale, float pointerLength, string label)
        {
            string path = Path.Combine(Application.persistentDataPath, ProfileFileName);
            RtsProfileSettings settings = new RtsProfileSettings(path);
            if (!settings.TryLoad(out string loadError))
            {
                Debug.LogWarning("[Command RTS Profile] Starting from defaults because the existing profile could not be loaded: " + loadError);
            }

            settings.Data.tabletopScale = tabletopScale;
            settings.Data.pointerLength = pointerLength;

            if (!settings.TrySave(out string saveError))
            {
                Debug.LogError("[Command RTS Profile] Could not save " + label + " Quest tabletop profile: " + saveError);
                return;
            }

            float simulationUnitsPerMeter = 126f / settings.Data.tabletopScale;
            float boardWidthMeters = RtsBalance.MapHalfSize * 2f / simulationUnitsPerMeter;
            Debug.Log("[Command RTS Profile] Saved " + label + " Quest tabletop profile to " + path + " with board width " + boardWidthMeters.ToString("0.00") + "m.");
        }
    }
}
#endif
