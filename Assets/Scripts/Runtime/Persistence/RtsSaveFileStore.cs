using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsSaveFileStore
    {
        private readonly string rootPath;

        public RtsSaveFileStore(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public static RtsSaveFileStore CreateDefault()
        {
            return new RtsSaveFileStore(Path.Combine(Application.persistentDataPath, "Saves"));
        }

        public string GetSlotPath(string slotId)
        {
            return Path.Combine(rootPath, SanitizeSlot(slotId) + ".json");
        }

        public bool HasSlot(string slotId)
        {
            return File.Exists(GetSlotPath(slotId));
        }

        public bool TryWrite(string slotId, string contents, out string error)
        {
            error = string.Empty;

            try
            {
                Directory.CreateDirectory(rootPath);
                string path = GetSlotPath(slotId);
                string tempPath = path + ".tmp";
                string backupPath = path + ".bak";

                File.WriteAllText(tempPath, contents);
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

        public bool TryRead(string slotId, out string contents, out string error)
        {
            contents = string.Empty;
            error = string.Empty;

            try
            {
                string path = GetSlotPath(slotId);
                if (!File.Exists(path))
                {
                    error = "Save slot does not exist: " + slotId;
                    return false;
                }

                contents = File.ReadAllText(path);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public List<string> ListSlots()
        {
            List<string> slots = new List<string>();
            if (!Directory.Exists(rootPath))
            {
                return slots;
            }

            string[] files = Directory.GetFiles(rootPath, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                slots.Add(Path.GetFileNameWithoutExtension(files[i]));
            }

            return slots;
        }

        private static string SanitizeSlot(string slotId)
        {
            string value = string.IsNullOrEmpty(slotId) ? "autosave" : slotId;
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                value = value.Replace(invalid[i], '_');
            }

            return value;
        }
    }
}
