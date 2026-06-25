using UnityEngine;

namespace QuestCommandRTS
{
    [DisallowMultipleComponent]
    public sealed class QuestTabletopSettings : MonoBehaviour
    {
        public float SimulationUnitsPerMeter = 126f;
        public float BoardHeightMeters = 0.82f;
        // Tracking-origin anchor in simulation units. The rig root is shifted down by BoardHeightMeters so the battlefield sits at tabletop height.
        public Vector3 InitialRigPosition = new Vector3(0f, 0f, -160f);
        public float InitialYawDegrees;
        public Vector3 FallbackHeadLocalPositionMeters = new Vector3(0f, 1.55f, 0f);
        public Vector3 FallbackHeadEulerDegrees = new Vector3(14f, 0f, 0f);
        public Vector3 FallbackLeftControllerLocalPositionMeters = new Vector3(-0.24f, 1.08f, 0.28f);
        public Vector3 FallbackRightControllerLocalPositionMeters = new Vector3(0.24f, 1.08f, 0.28f);
        public Vector3 FallbackControllerEulerDegrees = new Vector3(15f, 0f, 0f);
        public float RayLengthMeters = 3.2f;
        public float RayWidthMeters = 0.006f;
        public float ReticleSizeMeters = 0.035f;
        public float CameraNearClipMeters = 0.02f;
        public float CameraFarClipMeters = 6f;
        public Vector3 StatusPanelLocalPositionMeters = new Vector3(-0.72f, 1.15f, 0.48f);
        public Vector2 StatusPanelSizeMeters = new Vector2(0.58f, 0.22f);
        public Vector3 TacticalMapLocalPositionMeters = new Vector3(0.68f, 1.12f, 0.46f);
        public Vector2 TacticalMapSizeMeters = new Vector2(0.34f, 0.34f);
        public Vector3 CommandConsoleLocalPositionMeters = new Vector3(-0.78f, 0.96f, 0.12f);
        public Vector2 CommandConsoleSizeMeters = new Vector2(0.74f, 0.52f);

        public float BoardHeightSimulationUnits => BoardHeightMeters * SimulationUnitsPerMeter;
        public float BattlefieldWidthMeters => RtsBalance.MapHalfSize * 2f / SimulationUnitsPerMeter;
        public float RayLengthSimulationUnits => RayLengthMeters * SimulationUnitsPerMeter;
        public float RayWidthSimulationUnits => RayWidthMeters * SimulationUnitsPerMeter;
        public float ReticleSizeSimulationUnits => ReticleSizeMeters * SimulationUnitsPerMeter;
        public float CameraNearClipSimulationUnits => CameraNearClipMeters * SimulationUnitsPerMeter;
        public float CameraFarClipSimulationUnits => CameraFarClipMeters * SimulationUnitsPerMeter;

        public void ApplyProfile(RtsProfileSettingsData profile)
        {
            if (profile == null)
            {
                ClampValues();
                return;
            }

            profile.Normalize();
            SimulationUnitsPerMeter = 126f / profile.tabletopScale;
            BoardHeightMeters = profile.tabletopHeight;
            RayLengthMeters = profile.pointerLength;
            StatusPanelSizeMeters = new Vector2(0.58f, 0.22f) * profile.uiScale;
            TacticalMapSizeMeters = new Vector2(0.34f, 0.34f) * profile.uiScale;
            CommandConsoleSizeMeters = new Vector2(0.74f, 0.52f) * profile.uiScale;
            ClampValues();
        }

        public Vector3 GetRigRootPosition()
        {
            return InitialRigPosition - Vector3.up * BoardHeightSimulationUnits;
        }

        private void OnValidate()
        {
            ClampValues();
        }

        private void ClampValues()
        {
            SimulationUnitsPerMeter = Mathf.Clamp(SimulationUnitsPerMeter, 50f, 240f);
            BoardHeightMeters = Mathf.Clamp(BoardHeightMeters, 0.35f, 1.25f);
            FallbackHeadLocalPositionMeters.y = Mathf.Clamp(FallbackHeadLocalPositionMeters.y, 0.9f, 2.2f);
            FallbackLeftControllerLocalPositionMeters.y = Mathf.Clamp(FallbackLeftControllerLocalPositionMeters.y, 0.6f, 1.7f);
            FallbackRightControllerLocalPositionMeters.y = Mathf.Clamp(FallbackRightControllerLocalPositionMeters.y, 0.6f, 1.7f);
            RayLengthMeters = Mathf.Clamp(RayLengthMeters, 0.25f, 8f);
            RayWidthMeters = Mathf.Clamp(RayWidthMeters, 0.001f, 0.03f);
            ReticleSizeMeters = Mathf.Clamp(ReticleSizeMeters, 0.01f, 0.12f);
            CameraNearClipMeters = Mathf.Clamp(CameraNearClipMeters, 0.005f, 0.2f);
            CameraFarClipMeters = Mathf.Clamp(CameraFarClipMeters, 2f, 20f);
        }
    }
}
