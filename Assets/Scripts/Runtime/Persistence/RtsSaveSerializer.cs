using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace QuestCommandRTS
{
    public static class RtsSaveSerializer
    {
        public const int CurrentSchemaVersion = 1;

        public static string Serialize(string slotId, RtsMatchSaveData data)
        {
            data.schemaVersion = CurrentSchemaVersion;
            data.savedUtc = DateTime.UtcNow.ToString("o");
            string payload = JsonUtility.ToJson(data, true);

            RtsSaveEnvelope envelope = new RtsSaveEnvelope
            {
                schemaVersion = CurrentSchemaVersion,
                gameVersion = Application.version,
                createdUtc = data.savedUtc,
                slotId = slotId,
                payloadJson = payload,
                checksum = ComputeChecksum(payload)
            };

            return JsonUtility.ToJson(envelope, true);
        }

        public static bool TryDeserialize(string json, out RtsMatchSaveData data, out string error)
        {
            data = null;
            error = string.Empty;

            if (string.IsNullOrEmpty(json))
            {
                error = "Save file is empty.";
                return false;
            }

            RtsSaveEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<RtsSaveEnvelope>(json);
            }
            catch (Exception exception)
            {
                error = "Save envelope could not be parsed: " + exception.Message;
                return false;
            }

            if (envelope == null || string.IsNullOrEmpty(envelope.payloadJson))
            {
                error = "Save envelope is missing payload.";
                return false;
            }

            if (envelope.schemaVersion > CurrentSchemaVersion)
            {
                error = "Save schema " + envelope.schemaVersion + " is newer than this build supports.";
                return false;
            }

            string checksum = ComputeChecksum(envelope.payloadJson);
            if (!string.Equals(checksum, envelope.checksum, StringComparison.Ordinal))
            {
                error = "Save checksum mismatch.";
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<RtsMatchSaveData>(envelope.payloadJson);
            }
            catch (Exception exception)
            {
                error = "Save payload could not be parsed: " + exception.Message;
                return false;
            }

            if (data == null)
            {
                error = "Save payload is empty.";
                return false;
            }

            return RtsSaveMigration.TryMigrate(data, out error);
        }

        private static string ComputeChecksum(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
