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
            Add(results, "Quest input present", input != null, "QuestRtsInputController should translate controller state into shared dispatcher calls.");
            Add(results, "Quest world HUD present", worldHud != null && HasWorldSpaceCanvas("Quest World Status"), "Quest mode should expose a world-space status panel.");
            Add(results, "Quest tactical map present", tacticalMap != null && HasWorldSpaceCanvas("Quest Tactical Map"), "Quest mode should expose the battle map as world-space headset UI.");
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
            else
            {
                Add(results, "Quest settings present", false, "QuestTabletopSettings component is missing.");
            }

            if (rig != null)
            {
                Add(results, "Head tracking node", HasTrackedPose(rig.Head), "XR Head should have QuestTrackedNodePose.");
                Add(results, "Left controller node", HasTrackedPose(rig.LeftController), "Left Controller should have QuestTrackedNodePose.");
                Add(results, "Right controller node", HasTrackedPose(rig.RightController), "Right Controller should have QuestTrackedNodePose.");
                Add(results, "Pointer visuals", HasDescendant(rig.RigRoot, "Right Controller RTS Ray") && HasDescendant(rig.RigRoot, "RTS Pointer Reticle"), "Right controller ray and reticle objects should be created once.");
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

        private static bool HasDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return false;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == objectName)
                {
                    return true;
                }
            }

            return false;
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
