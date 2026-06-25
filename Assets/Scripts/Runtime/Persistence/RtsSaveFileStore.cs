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

        public string GetBackupSlotPath(string slotId)
        {
            return GetSlotPath(slotId) + ".bak";
        }

        public bool HasSlot(string slotId)
        {
            return File.Exists(GetSlotPath(slotId)) || File.Exists(GetBackupSlotPath(slotId));
        }

        public bool TryWrite(string slotId, string contents, out string error)
        {
            error = string.Empty;

            try
            {
                Directory.CreateDirectory(rootPath);
                string path = GetSlotPath(slotId);
                string tempPath = path + ".tmp";
                string backupPath = GetBackupSlotPath(slotId);

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
            return TryReadPath(GetSlotPath(slotId), "Save slot does not exist: " + slotId, out contents, out error);
        }

        public bool TryReadBackup(string slotId, out string contents, out string error)
        {
            return TryReadPath(GetBackupSlotPath(slotId), "Save backup does not exist: " + slotId, out contents, out error);
        }

        private static bool TryReadPath(string path, string missingError, out string contents, out string error)
        {
            contents = string.Empty;
            error = string.Empty;

            try
            {
                if (!File.Exists(path))
                {
                    error = missingError;
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
            HashSet<string> seen = new HashSet<string>();
            if (!Directory.Exists(rootPath))
            {
                return slots;
            }

            string[] files = Directory.GetFiles(rootPath, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                AddSlot(slots, seen, Path.GetFileNameWithoutExtension(files[i]));
            }

            string[] backups = Directory.GetFiles(rootPath, "*.json.bak");
            for (int i = 0; i < backups.Length; i++)
            {
                string name = Path.GetFileName(backups[i]);
                if (name.EndsWith(".json.bak", StringComparison.Ordinal))
                {
                    AddSlot(slots, seen, name.Substring(0, name.Length - ".json.bak".Length));
                }
            }

            slots.Sort(StringComparer.Ordinal);
            return slots;
        }

        private static void AddSlot(List<string> slots, HashSet<string> seen, string slotId)
        {
            if (seen.Add(slotId))
            {
                slots.Add(slotId);
            }
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
