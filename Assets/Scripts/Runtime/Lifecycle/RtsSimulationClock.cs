using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsSimulationClock
    {
        public float SimulationTime { get; private set; }
        public float DeltaTime { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        public RtsPauseReason PauseReasons { get; private set; }
        public bool IsPaused => PauseReasons != RtsPauseReason.None;

        public void Tick(float unscaledDeltaTime)
        {
            if (IsPaused)
            {
                DeltaTime = 0f;
                return;
            }

            DeltaTime = Mathf.Max(0f, unscaledDeltaTime) * TimeScale;
            SimulationTime += DeltaTime;
        }

        public void SetTimeScale(float timeScale)
        {
            TimeScale = Mathf.Clamp(timeScale, 0.5f, 2f);
        }

        public void SetPaused(RtsPauseReason reason, bool paused)
        {
            if (reason == RtsPauseReason.None)
            {
                return;
            }

            if (paused)
            {
                PauseReasons |= reason;
                DeltaTime = 0f;
                return;
            }

            PauseReasons &= ~reason;
        }

        public void SetSimulationTime(float time)
        {
            SimulationTime = Mathf.Max(0f, time);
            DeltaTime = 0f;
        }
    }
}
