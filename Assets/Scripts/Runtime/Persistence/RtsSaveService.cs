namespace QuestCommandRTS
{
    public sealed class RtsSaveService
    {
        private readonly RtsGame game;
        private readonly RtsSaveFileStore store;

        public bool IsBusy { get; private set; }

        public RtsSaveService(RtsGame owner, RtsSaveFileStore fileStore)
        {
            game = owner;
            store = fileStore;
        }

        public RtsMatchSaveData Capture()
        {
            return game.CaptureSaveData();
        }

        public bool HasSlot(string slotId)
        {
            return store.HasSlot(slotId);
        }

        public System.Collections.Generic.List<string> ListSlots()
        {
            return store.ListSlots();
        }

        public bool TryGetSlotMetadata(string slotId, out RtsSaveMetadata metadata, out string error)
        {
            metadata = null;
            if (TryReadAndDeserializeMetadata(slotId, false, out metadata, out error))
            {
                return true;
            }

            string primaryError = error;
            if (TryReadAndDeserializeMetadata(slotId, true, out metadata, out string backupError))
            {
                metadata.readFromBackup = true;
                error = string.Empty;
                return true;
            }

            error = primaryError + " Backup metadata unavailable: " + backupError;
            return false;
        }

        public System.Collections.Generic.List<RtsSaveMetadata> ListSlotMetadata()
        {
            System.Collections.Generic.List<RtsSaveMetadata> metadata = new System.Collections.Generic.List<RtsSaveMetadata>();
            System.Collections.Generic.List<string> slots = ListSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                if (TryGetSlotMetadata(slots[i], out RtsSaveMetadata item, out _))
                {
                    metadata.Add(item);
                }
            }

            return metadata;
        }

        public bool TryWriteSlot(string slotId, out string error)
        {
            error = string.Empty;
            if (IsBusy)
            {
                error = "Save system is busy.";
                return false;
            }

            IsBusy = true;
            game.Lifecycle?.BeginSaving();
            try
            {
                string json = RtsSaveSerializer.Serialize(slotId, Capture());
                return store.TryWrite(slotId, json, out error);
            }
            finally
            {
                game.Lifecycle?.EndSaving();
                IsBusy = false;
            }
        }

        public bool TryReadSlot(string slotId, out RtsMatchSaveData data, out string error)
        {
            data = null;
            if (TryReadAndDeserializePrimary(slotId, out data, out error))
            {
                return true;
            }

            string primaryError = error;
            if (!store.TryReadBackup(slotId, out string backupJson, out string backupReadError))
            {
                error = primaryError + " Backup unavailable: " + backupReadError;
                return false;
            }

            if (RtsSaveSerializer.TryDeserialize(backupJson, out data, out string backupDeserializeError))
            {
                error = string.Empty;
                return true;
            }

            error = primaryError + " Backup unreadable: " + backupDeserializeError;
            return false;
        }

        public bool TryLoadSlot(string slotId, out string error)
        {
            error = string.Empty;
            if (IsBusy)
            {
                error = "Save system is busy.";
                return false;
            }

            IsBusy = true;
            game.Lifecycle?.BeginLoading();
            try
            {
                if (!TryReadSlot(slotId, out RtsMatchSaveData data, out error))
                {
                    return false;
                }

                return game.RestoreSaveData(data, out error);
            }
            finally
            {
                game.Lifecycle?.EndLoading();
                IsBusy = false;
            }
        }

        private bool TryReadAndDeserializePrimary(string slotId, out RtsMatchSaveData data, out string error)
        {
            data = null;
            if (!store.TryRead(slotId, out string json, out error))
            {
                return false;
            }

            return RtsSaveSerializer.TryDeserialize(json, out data, out error);
        }

        private bool TryReadAndDeserializeMetadata(string slotId, bool backup, out RtsSaveMetadata metadata, out string error)
        {
            metadata = null;
            string json;
            bool read = backup ? store.TryReadBackup(slotId, out json, out error) : store.TryRead(slotId, out json, out error);
            if (!read)
            {
                return false;
            }

            return RtsSaveSerializer.TryReadMetadata(json, out metadata, out error);
        }
    }
}
