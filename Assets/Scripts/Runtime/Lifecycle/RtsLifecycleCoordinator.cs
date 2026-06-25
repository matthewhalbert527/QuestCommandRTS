using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsLifecycleCoordinator : MonoBehaviour
    {
        public const string PeriodicAutosaveSlot = "periodic-autosave";

        public bool HasInputFocus { get; private set; } = true;
        public bool IsApplicationPaused { get; private set; }
        public bool AcceptsInput => !IsApplicationPaused && HasInputFocus && !IsSavingOrLoading;
        public bool IsSavingOrLoading { get; private set; }

        private RtsGame game;
        private float nextPeriodicAutosaveTime;

        public void Initialize(RtsGame owner)
        {
            game = owner;
            ScheduleNextPeriodicAutosave(Time.unscaledTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SetApplicationPaused(pauseStatus);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetInputFocus(hasFocus);
        }

        private void Update()
        {
            EvaluatePeriodicAutosave(Time.unscaledTime);
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
                ScheduleNextPeriodicAutosave(Time.unscaledTime);
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
                ScheduleNextPeriodicAutosave(Time.unscaledTime);
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

        public void ScheduleNextPeriodicAutosaveForTests(float now)
        {
            ScheduleNextPeriodicAutosave(now);
        }

        public bool EvaluatePeriodicAutosaveForTests(float now)
        {
            return EvaluatePeriodicAutosave(now);
        }
#endif

        private bool EvaluatePeriodicAutosave(float now)
        {
            if (now < nextPeriodicAutosaveTime)
            {
                return false;
            }

            if (!CanPeriodicAutosave())
            {
                ScheduleNextPeriodicAutosave(now);
                return false;
            }

            bool saved = TryAutosave(PeriodicAutosaveSlot);
            ScheduleNextPeriodicAutosave(now);
            return saved;
        }

        private bool CanPeriodicAutosave()
        {
            if (game == null || game.SaveService == null || game.IsMatchOver || IsApplicationPaused || !HasInputFocus || IsSavingOrLoading || game.SaveService.IsBusy)
            {
                return false;
            }

            if (game.ProfileSettings != null && !game.ProfileSettings.Data.periodicAutosaveEnabled)
            {
                return false;
            }

            return game.Clock == null || !game.Clock.IsPaused;
        }

        private void ScheduleNextPeriodicAutosave(float now)
        {
            nextPeriodicAutosaveTime = now + GetPeriodicAutosaveIntervalSeconds();
        }

        private float GetPeriodicAutosaveIntervalSeconds()
        {
            if (game == null || game.ProfileSettings == null || game.ProfileSettings.Data == null)
            {
                return 180f;
            }

            game.ProfileSettings.Data.Normalize();
            return game.ProfileSettings.Data.periodicAutosaveIntervalSeconds;
        }

        private bool TryAutosave(string slot)
        {
            if (game == null || game.SaveService == null || game.IsMatchOver)
            {
                return false;
            }

            bool saved = game.SaveService.TryWriteSlot(slot, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("Autosave failed: " + error);
            }

            return saved;
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
