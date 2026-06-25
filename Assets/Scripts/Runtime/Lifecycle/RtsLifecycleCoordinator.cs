using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsLifecycleCoordinator : MonoBehaviour
    {
        public bool HasInputFocus { get; private set; } = true;
        public bool IsApplicationPaused { get; private set; }
        public bool AcceptsInput => !IsApplicationPaused && HasInputFocus && !IsSavingOrLoading;
        public bool IsSavingOrLoading { get; private set; }

        private RtsGame game;

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SetApplicationPaused(pauseStatus);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetInputFocus(hasFocus);
        }

        public void SetUserPaused(bool paused)
        {
            SetPauseReason(RtsPauseReason.User, paused);
        }

        public void BeginSaving()
        {
            IsSavingOrLoading = true;
            SetPauseReason(RtsPauseReason.Saving, true);
        }

        public void EndSaving()
        {
            SetPauseReason(RtsPauseReason.Saving, false);
            IsSavingOrLoading = false;
        }

        public void BeginLoading()
        {
            IsSavingOrLoading = true;
            SetPauseReason(RtsPauseReason.Loading, true);
        }

        public void EndLoading()
        {
            SetPauseReason(RtsPauseReason.Loading, false);
            IsSavingOrLoading = false;
        }

        public void SetMatchEnded(bool ended)
        {
            SetPauseReason(RtsPauseReason.MatchEnded, ended);
        }

        public void SetApplicationPaused(bool paused)
        {
            if (IsApplicationPaused == paused)
            {
                return;
            }

            IsApplicationPaused = paused;
            SetPauseReason(RtsPauseReason.ApplicationPause, paused);
            SetPlacementSuspended(paused || !HasInputFocus);
            if (paused)
            {
                TryAutosave("autosave");
            }
        }

        public void SetInputFocus(bool hasFocus)
        {
            if (HasInputFocus == hasFocus)
            {
                return;
            }

            HasInputFocus = hasFocus;
            SetPauseReason(RtsPauseReason.InputFocusLost, !hasFocus);
            SetPlacementSuspended(IsApplicationPaused || !hasFocus);
            if (!hasFocus)
            {
                TryAutosave("focus-autosave");
            }
        }

#if UNITY_EDITOR
        public void SetApplicationPausedForTests(bool paused)
        {
            SetApplicationPaused(paused);
        }

        public void SetInputFocusForTests(bool hasFocus)
        {
            SetInputFocus(hasFocus);
        }
#endif

        private void TryAutosave(string slot)
        {
            if (game == null || game.SaveService == null || game.IsMatchOver)
            {
                return;
            }

            game.SaveService.TryWriteSlot(slot, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("Autosave failed: " + error);
            }
        }

        private void SetPauseReason(RtsPauseReason reason, bool paused)
        {
            if (game != null && game.Clock != null)
            {
                game.Clock.SetPaused(reason, paused);
            }
        }

        private void SetPlacementSuspended(bool suspended)
        {
            if (game != null && game.BuildManager != null)
            {
                game.BuildManager.SetPlacementSuspended(suspended);
            }
        }
    }
}
