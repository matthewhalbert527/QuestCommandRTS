using System;
using System.IO;
using UnityEngine;

namespace QuestCommandRTS
{
    [Serializable]
    public sealed class RtsProfileSettingsData
    {
        public int schemaVersion = 1;
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float effectsVolume = 0.9f;
        public bool hapticsEnabled = true;
        public bool leftHandedMode;
        public float pointerLength = 80f;
        public float tabletopScale = 1f;
        public float tabletopHeight = 0f;
        public float uiScale = 1f;
        public bool highContrast;
        public bool reducedFlashing;
        public string qualityPreset = "Balanced";
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
                return true;
            }

            try
            {
                Data = JsonUtility.FromJson<RtsProfileSettingsData>(File.ReadAllText(path));
                if (Data == null || Data.schemaVersion > 1)
                {
                    Data = new RtsProfileSettingsData();
                }

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Data = new RtsProfileSettingsData();
                return false;
            }
        }

        public bool TrySave(out string error)
        {
            error = string.Empty;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(Data, true));
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
