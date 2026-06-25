using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuestCommandRTS
{
    public readonly struct QuestRuntimeSmokeItem
    {
        public readonly string Label;
        public readonly bool Passed;
        public readonly bool Manual;
        public readonly string Detail;

        public QuestRuntimeSmokeItem(string label, bool passed, bool manual, string detail)
        {
            Label = label;
            Passed = passed;
            Manual = manual;
            Detail = detail;
        }
    }

    public static class QuestRuntimeSmokeReport
    {
        public static List<QuestRuntimeSmokeItem> Build(RtsGame game)
        {
            List<QuestRuntimeSmokeItem> results = new List<QuestRuntimeSmokeItem>();

            if (game == null)
            {
                Add(results, "Runtime instance", false, "No RtsGame instance was provided.");
                return results;
            }

            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();
            QuestTabletopRig rig = game.QuestRig;
            QuestRtsInputController input = game.GetComponent<QuestRtsInputController>();
            QuestWorldHud worldHud = game.GetComponent<QuestWorldHud>();
            QuestTacticalMap tacticalMap = game.GetComponent<QuestTacticalMap>();
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();

            Add(results, "Runtime mode", game.RuntimeMode == RtsRuntimeMode.QuestVr, game.RuntimeMode.ToString());
            Add(results, "Desktop command camera absent", game.CommandCamera == null && GameObject.Find("Command Camera") == null, "Quest mode must not create the desktop Command Camera.");
            Add(results, "Desktop input absent", game.GetComponent<RtsInputController>() == null, "Quest mode must not install RtsInputController.");
            Add(results, "Desktop HUD absent", game.GetComponent<RtsHud>() == null, "Quest mode must not install the Screen Space Overlay desktop HUD.");
            Add(results, "Quest rig present", rig != null && rig.RigRoot != null && rig.HeadCamera != null, "QuestTabletopRig should own the tabletop root and XR head camera.");
            Add(results, "Quest settings present", settings != null, "QuestTabletopSettings should own tabletop scale, height, ray, reticle, clip-plane, and world-space UI values.");
            Add(results, "Quest input present", input != null, "QuestRtsInputController should translate controller state into shared dispatcher calls.");
            Add(results, "Quest world HUD present", worldHud != null && HasWorldSpaceCanvas("Quest World Status"), "Quest mode should expose a world-space status panel.");
            Add(results, "Quest world HUD control hints", HasWorldHudControlHints(), "World-space status panel should show trigger, A/B, and X command-console hints.");
            Add(results, "Quest tactical map present", tacticalMap != null && HasWorldSpaceCanvas("Quest Tactical Map"), "Quest mode should expose the battle map as world-space headset UI.");
            string tacticalMapDetail;
            Add(results, "Quest tactical map non-interactive", HasNonInteractiveTacticalMap(out tacticalMapDetail), tacticalMapDetail);
            Add(results, "Quest command console present", console != null && console.PanelRect != null, "Quest command console should exist under the tabletop rig.");
            Add(results, "View camera uses XR head", rig != null && rig.HeadCamera != null && game.GetViewCameraTransform() == rig.HeadCamera.transform, "Game view camera should resolve to the Quest head camera.");

            if (settings != null)
            {
                float minSimulationUnitsPerMeter = 126f / RtsProfileSettingsData.MaxTabletopScale;
                float maxSimulationUnitsPerMeter = 126f / RtsProfileSettingsData.MinTabletopScale;
                bool scaleValid = settings.SimulationUnitsPerMeter >= minSimulationUnitsPerMeter - 0.01f && settings.SimulationUnitsPerMeter <= maxSimulationUnitsPerMeter + 0.01f;
                Add(results, "Tabletop scale", scaleValid, "SimulationUnitsPerMeter=" + settings.SimulationUnitsPerMeter.ToString("0.##") + ", supported=" + minSimulationUnitsPerMeter.ToString("0.##") + "-" + maxSimulationUnitsPerMeter.ToString("0.##"));

                float minBoardWidthMeters = RtsBalance.MapHalfSize * 2f / maxSimulationUnitsPerMeter;
                float maxBoardWidthMeters = RtsBalance.MapHalfSize * 2f / minSimulationUnitsPerMeter;
                bool boardWidthValid = settings.BattlefieldWidthMeters >= minBoardWidthMeters - 0.01f && settings.BattlefieldWidthMeters <= maxBoardWidthMeters + 0.01f;
                Add(results, "Board physical width", boardWidthValid, "Supported " + minBoardWidthMeters.ToString("0.##") + "m to " + maxBoardWidthMeters.ToString("0.##") + "m, actual " + settings.BattlefieldWidthMeters.ToString("0.##") + "m.");

                bool tabletopHeightValid = Mathf.Abs(settings.GetRigRootPosition().y + settings.BoardHeightSimulationUnits) <= 0.01f;
                Add(results, "Tabletop height offset", tabletopHeightValid, "BoardHeightMeters=" + settings.BoardHeightMeters.ToString("0.##"));

                if (rig != null && rig.RigRoot != null)
                {
                    bool rigScaleValid = Vector3.Distance(rig.RigRoot.localScale, Vector3.one * settings.SimulationUnitsPerMeter) <= 0.01f;
                    Add(results, "Rig scale applied", rigScaleValid, "RigRoot.localScale=" + rig.RigRoot.localScale);
                }
            }
            if (rig != null)
            {
                Add(results, "Head tracking node", HasTrackedPose(rig.Head), "XR Head should have QuestTrackedNodePose.");
                Add(results, "Left controller node", HasTrackedPose(rig.LeftController), "Left Controller should have QuestTrackedNodePose.");
                Add(results, "Right controller node", HasTrackedPose(rig.RightController), "Right Controller should have QuestTrackedNodePose.");
                string pointerDetail;
                Add(results, "Pointer visuals", HasPointerVisuals(rig, settings, out pointerDetail), pointerDetail);
            }

            AddManual(results, "Physical headset verification", "HMD pose, controller tracking, ray alignment, comfort, and performance still require Quest Link or device testing.");
            return results;
        }

        private static bool HasTrackedPose(Transform target)
        {
            return target != null && target.GetComponent<QuestTrackedNodePose>() != null;
        }

        private static bool HasWorldSpaceCanvas(string objectName)
        {
            GameObject canvasObject = GameObject.Find(objectName);
            if (canvasObject == null)
            {
                return false;
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            return canvas != null && canvas.renderMode == RenderMode.WorldSpace;
        }

        private static bool HasWorldHudControlHints()
        {
            GameObject canvasObject = GameObject.Find("Quest World Status");
            if (canvasObject == null)
            {
                return false;
            }

            Text[] textBlocks = canvasObject.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < textBlocks.Length; i++)
            {
                Text text = textBlocks[i];
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                string value = text.text;
                if (value.Contains("Trigger: Select") &&
                    value.Contains("A: Command") &&
                    value.Contains("B: Cancel/Clear") &&
                    value.Contains("X: Command Console"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasNonInteractiveTacticalMap(out string detail)
        {
            GameObject canvasObject = GameObject.Find("Quest Tactical Map");
            if (canvasObject == null)
            {
                detail = "Quest Tactical Map object is missing.";
                return false;
            }

            GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            Graphic[] graphics = canvasObject.GetComponentsInChildren<Graphic>(true);
            int raycastTargets = 0;
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic != null && graphic.raycastTarget)
                {
                    raycastTargets++;
                }
            }

            bool raycasterDisabled = raycaster != null && !raycaster.enabled;
            detail = "raycaster=" + (raycasterDisabled ? "disabled" : "enabled-or-missing") +
                ", raycastTargets=" + raycastTargets +
                ", graphics=" + graphics.Length;
            return raycasterDisabled && raycastTargets == 0;
        }

        private static bool HasPointerVisuals(QuestTabletopRig rig, QuestTabletopSettings settings, out string detail)
        {
            if (rig == null || rig.RigRoot == null || settings == null)
            {
                detail = "Quest rig, rig root, and settings are required before pointer visuals can be checked.";
                return false;
            }

            Transform lineTransform = FindDescendant(rig.RigRoot, "Right Controller RTS Ray");
            Transform reticleTransform = FindDescendant(rig.RigRoot, "RTS Pointer Reticle");
            LineRenderer line = lineTransform != null ? lineTransform.GetComponent<LineRenderer>() : null;
            Renderer reticleRenderer = reticleTransform != null ? reticleTransform.GetComponent<Renderer>() : null;
            Collider reticleCollider = reticleTransform != null ? reticleTransform.GetComponent<Collider>() : null;

            bool lineValid = line != null &&
                line.useWorldSpace &&
                line.positionCount == 2 &&
                line.sharedMaterial != null &&
                Mathf.Abs(line.widthMultiplier - settings.RayWidthSimulationUnits) <= 0.001f;
            bool reticleValid = reticleTransform != null &&
                reticleRenderer != null &&
                reticleRenderer.sharedMaterial != null &&
                reticleCollider == null &&
                Vector3.Distance(reticleTransform.localScale, Vector3.one * settings.ReticleSizeMeters) <= 0.001f;

            detail = "line=" + (lineValid ? "ok" : "invalid") +
                ", reticle=" + (reticleValid ? "ok" : "invalid") +
                ", expectedWidth=" + settings.RayWidthSimulationUnits.ToString("0.###") +
                ", expectedReticleMeters=" + settings.ReticleSizeMeters.ToString("0.###");
            return lineValid && reticleValid;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == objectName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static void Add(List<QuestRuntimeSmokeItem> results, string label, bool passed, string detail)
        {
            results.Add(new QuestRuntimeSmokeItem(label, passed, false, detail));
        }

        private static void AddManual(List<QuestRuntimeSmokeItem> results, string label, string detail)
        {
            results.Add(new QuestRuntimeSmokeItem(label, false, true, detail));
        }
    }
}
