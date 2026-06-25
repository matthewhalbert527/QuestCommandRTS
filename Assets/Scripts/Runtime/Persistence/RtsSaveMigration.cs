namespace QuestCommandRTS
{
    public static class RtsSaveMigration
    {
        public static bool TryMigrate(RtsMatchSaveData data, out string error)
        {
            error = string.Empty;
            if (data == null)
            {
                error = "Save payload is empty.";
                return false;
            }

            if (data.schemaVersion > RtsSaveSerializer.CurrentSchemaVersion)
            {
                error = "Save schema " + data.schemaVersion + " is newer than this build supports.";
                return false;
            }

            if (data.schemaVersion <= 0)
            {
                data.schemaVersion = 1;
            }

            return true;
        }
    }
}
