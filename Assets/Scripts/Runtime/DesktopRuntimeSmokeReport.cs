using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestCommandRTS
{
    public readonly struct DesktopRuntimeSmokeItem
    {
        public readonly string Label;
        public readonly bool Passed;
        public readonly bool Manual;
        public readonly string Detail;

        public DesktopRuntimeSmokeItem(string label, bool passed, bool manual, string detail)
        {
            Label = label;
            Passed = passed;
            Manual = manual;
            Detail = detail;
        }
    }

    public static class DesktopRuntimeSmokeReport
    {
        public static List<DesktopRuntimeSmokeItem> Build(RtsGame game)
        {
            List<DesktopRuntimeSmokeItem> results = new List<DesktopRuntimeSmokeItem>();

            if (game == null)
            {
                Add(results, "Runtime instance", false, "No RtsGame instance was provided.");
                return results;
            }

            RtsInputController input = game.GetComponent<RtsInputController>();
            RtsHud hud = game.GetComponent<RtsHud>();

            Add(results, "Runtime mode", game.RuntimeMode == RtsRuntimeMode.Desktop, game.RuntimeMode.ToString());
            Add(results, "Command camera present", game.CommandCamera != null, "Desktop mode should create or use a command camera.");
            Add(results, "View camera uses command camera", game.CommandCamera != null && game.GetViewCameraTransform() == game.CommandCamera.transform, "Desktop view camera should resolve to the command camera.");
            Add(results, "Desktop input present", input != null, "Desktop mode should install RtsInputController.");
            Add(results, "Desktop input uses shared dispatcher", input != null && input.SharedDispatcher == game.CommandDispatcher, "Desktop input should reference RtsGame.CommandDispatcher.");
            Add(results, "Desktop HUD present", hud != null && HasScreenSpaceHud(), "Desktop mode should install the Screen Space Overlay RTS HUD.");
            Add(results, "Desktop production progress meter", HasHudChild("Production Progress Fill") && HasHudChild("Production Progress Text"), "Desktop HUD should show active unit build progress.");
            Add(results, "Desktop sidebar minimap", HasHudChild("Sidebar Minimap") && HasHudChild("Sidebar Minimap Plot"), "Desktop HUD should place a large minimap at the top of the command sidebar.");
            Add(results, "Desktop command sidebar tabs", HasHudChild("Buildings Tab") && HasHudChild("Units Tab") && HasHudChild("Commands Tab"), "Desktop HUD should group build, unit, and command tiles into C&C-style tabs.");
            Add(results, "Desktop sidebar collapse", HasHudChild("Sidebar Collapse Button"), "Desktop HUD should allow the command sidebar to shrink.");
            Add(results, "Desktop event system present", HasDesktopEventSystem(), "Desktop HUD should create an EventSystem for UI interaction.");
            Add(results, "Quest rig absent", game.QuestRig == null && game.GetComponent<QuestRtsInputController>() == null, "Desktop mode should not install the Quest rig or Quest input controller.");
            Add(results, "Quest world HUD absent", game.GetComponent<QuestWorldHud>() == null, "Desktop mode should not install QuestWorldHud.");
            Add(results, "Quest tactical map absent", game.GetComponent<QuestTacticalMap>() == null, "Desktop mode should not install QuestTacticalMap.");
            Add(results, "Quest command console absent", game.GetComponent<QuestCommandConsole>() == null, "Desktop mode should not install QuestCommandConsole.");
            Add(results, "Command dispatcher present", game.CommandDispatcher != null, "Both runtime modes should have the shared command dispatcher.");
            Add(results, "Build manager present", game.BuildManager != null, "Desktop build and placement controls require BuildManager.");
            Add(results, "Initial entities spawned", game.Entities.Count > 0 && game.ResourceNodes.Count > 0, "Generated match should include entities and resource nodes.");
            Add(results, "Initial selection present", game.Selection.Count > 0, "Generated match should start with a usable player selection.");
            AddManual(results, "Desktop control regression", "Camera pan/zoom, drag selection, control groups, right-click commands, build hotkeys, and production still need a manual playthrough.");
            return results;
        }

        private static bool HasScreenSpaceHud()
        {
            GameObject hudObject = GameObject.Find("RTS HUD");
            if (hudObject == null)
            {
                return false;
            }

            Canvas canvas = hudObject.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return false;
            }

            return hudObject.GetComponent<GraphicRaycaster>() != null;
        }

        private static bool HasHudChild(string childName)
        {
            GameObject hudObject = GameObject.Find("RTS HUD");
            if (hudObject == null)
            {
                return false;
            }

            Transform[] children = hudObject.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDesktopEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = Object.FindObjectOfType<EventSystem>();
            }

            return eventSystem != null && eventSystem.GetComponent<StandaloneInputModule>() != null;
        }

        private static void Add(List<DesktopRuntimeSmokeItem> results, string label, bool passed, string detail)
        {
            results.Add(new DesktopRuntimeSmokeItem(label, passed, false, detail));
        }

        private static void AddManual(List<DesktopRuntimeSmokeItem> results, string label, string detail)
        {
            results.Add(new DesktopRuntimeSmokeItem(label, false, true, detail));
        }
    }
}
