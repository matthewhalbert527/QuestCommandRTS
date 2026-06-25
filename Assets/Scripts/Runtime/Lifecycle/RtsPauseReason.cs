using System;

namespace QuestCommandRTS
{
    [Flags]
    public enum RtsPauseReason
    {
        None = 0,
        User = 1 << 0,
        ApplicationPause = 1 << 1,
        InputFocusLost = 1 << 2,
        HmdUnmounted = 1 << 3,
        Saving = 1 << 4,
        Loading = 1 << 5,
        MatchEnded = 1 << 6
    }
}
