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
            if (!store.TryRead(slotId, out string json, out error))
            {
                return false;
            }

            return RtsSaveSerializer.TryDeserialize(json, out data, out error);
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
    }
}
