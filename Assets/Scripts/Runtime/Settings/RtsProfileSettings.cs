using System;
using System.IO;
using UnityEngine;

namespace QuestCommandRTS
{
    [Serializable]
    public sealed class RtsProfileSettingsData
    {
        public const int CurrentSchemaVersion = 1;
        public const float DefaultPointerLength = 3.2f;
        public const float RoomSizedPointerLength = 5.5f;
        public const float DefaultTabletopScale = 1f;
        public const float MinTabletopScale = 0.75f;
        public const float MaxTabletopScale = 2.25f;
        public const float RoomSizedTabletopScale = MaxTabletopScale;

        public int schemaVersion = CurrentSchemaVersion;
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float effectsVolume = 0.9f;
        public bool hapticsEnabled = true;
        public bool leftHandedMode;
        public float pointerLength = DefaultPointerLength;
        public float tabletopScale = DefaultTabletopScale;
        public float tabletopHeight = 0.82f;
        public float uiScale = 1f;
        public bool highContrast;
        public bool reducedFlashing;
        public string qualityPreset = "Balanced";
        public bool periodicAutosaveEnabled = true;
        public float periodicAutosaveIntervalSeconds = 180f;

        public void Normalize()
        {
            schemaVersion = CurrentSchemaVersion;
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            effectsVolume = Mathf.Clamp01(effectsVolume);
            pointerLength = NormalizePositiveRange(pointerLength, DefaultPointerLength, 0.25f, 8f);
            tabletopScale = NormalizePositiveRange(tabletopScale, DefaultTabletopScale, MinTabletopScale, MaxTabletopScale);
            tabletopHeight = NormalizePositiveRange(tabletopHeight, 0.82f, 0.35f, 1.25f);
            uiScale = NormalizePositiveRange(uiScale, 1f, 0.75f, 1.35f);
            periodicAutosaveIntervalSeconds = NormalizePositiveRange(periodicAutosaveIntervalSeconds, 180f, 30f, 900f);
            qualityPreset = NormalizeQualityPreset(qualityPreset);
        }

        private static float NormalizePositiveRange(float value, float defaultValue, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return defaultValue;
            }

            return Mathf.Clamp(value, min, max);
        }

        private static string NormalizeQualityPreset(string value)
        {
            if (string.Equals(value, "Performance", StringComparison.OrdinalIgnoreCase))
            {
                return "Performance";
            }

            if (string.Equals(value, "Quality", StringComparison.OrdinalIgnoreCase))
            {
                return "Quality";
            }

            return "Balanced";
        }
    }

    public sealed class RtsProfileSettings
    {
        private readonly string path;

        public RtsProfileSettingsData Data { get; private set; } = new RtsProfileSettingsData();

        public RtsProfileSettings(string path)
        {
            this.path = path;
        }

        public static RtsProfileSettings CreateDefault()
        {
            return new RtsProfileSettings(Path.Combine(Application.persistentDataPath, "profile-settings.json"));
        }

        public bool TryLoad(out string error)
        {
            error = string.Empty;
            if (!File.Exists(path))
            {
                Data = new RtsProfileSettingsData();
                Data.Normalize();
                return true;
            }

            try
            {
                RtsProfileSettingsData loaded = JsonUtility.FromJson<RtsProfileSettingsData>(File.ReadAllText(path));
                if (loaded == null)
                {
                    error = "Profile settings file did not contain valid settings data.";
                    Data = new RtsProfileSettingsData();
                    Data.Normalize();
                    return false;
                }

                if (loaded.schemaVersion > RtsProfileSettingsData.CurrentSchemaVersion)
                {
                    error = "Profile settings schema " + loaded.schemaVersion + " is newer than this build supports.";
                    Data = new RtsProfileSettingsData();
                    Data.Normalize();
                    return false;
                }

                Data = loaded;
                Data.Normalize();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Data = new RtsProfileSettingsData();
                Data.Normalize();
                return false;
            }
        }

        public bool TrySave(out string error)
        {
            error = string.Empty;
            try
            {
                Data.Normalize();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = path + ".tmp";
                string backupPath = path + ".bak";
                File.WriteAllText(tempPath, JsonUtility.ToJson(Data, true));
                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, true);
                    File.Delete(path);
                }

                File.Move(tempPath, path);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
