# Quest XR Setup

## Required Editor

Use Unity 2022.3.62f3. The package manifest is pinned for this editor line.

## Installed Packages

The repository declares XR Management 4.5.4, OpenXR 1.15.1, XR Interaction Toolkit 3.2.2, and Input System 1.18.0. Let Unity resolve package dependencies normally when the project opens.

## Android Modules

Install these modules for Unity 2022.3.62f3 through Unity Hub:

- Android Build Support
- Android SDK and NDK Tools
- OpenJDK

The repository does not include Android SDK, NDK, JDK, APK, AAB, or generated build folders.

## Headset Preparation

At a high level:

1. Enable Developer Mode for the headset through the Meta mobile app or Meta developer dashboard flow.
2. Connect the headset by USB for Android builds, or use Meta Quest Link for PC OpenXR testing.
3. Accept the USB debugging prompt in the headset when building or deploying.
4. Confirm the Meta desktop app is configured as the active OpenXR runtime for Quest Link testing.

## Automated Project Settings

Run `Tools > Quest RTS > Apply Recommended Quest Settings` to apply:

- Android package id: `com.matthewhalbert.questcommandrts`
- Android minimum API level 29
- Android target API automatic/highest installed
- IL2CPP scripting backend
- ARM64 target architecture
- Vulkan as the first Android graphics API
- Android multithreaded rendering
- Active Input Handling: Both
- OpenXR loader for Android
- OpenXR loader for Standalone for Quest Link testing
- OpenXR Single Pass Instanced render mode for Android and Standalone
- Android Meta Quest Support feature
- Android Oculus Touch Controller Profile
- Standalone Oculus Touch Controller Profile

Then run `Tools > Quest RTS > Validate XR Setup`. The validator checks both project settings and whether this Unity editor install supports `BuildTarget.Android`. If it reports that Android Build Support is missing, install Android Build Support, SDK and NDK Tools, and OpenJDK through Unity Hub before attempting a Quest device build.

Run `Tools > Quest RTS > Validate Generated Quest Runtime` for a local generated-runtime smoke report. It forces Quest mode in the editor, verifies the generated tabletop rig, XR head camera path, Quest input/controller objects, pointer visuals, world-space HUD, tactical map, command console, and confirms the desktop camera/input/HUD path is absent. This does not replace physical headset testing.

If Android Build Support is not installed or the editor has not switched to Android yet, Unity might not create Android OpenXR package settings in the local session. In that case, install the Android modules, switch the build target to Android, rerun the apply command, and verify Android Single Pass Instanced, Meta Quest Support, and Oculus Touch in Project Settings.

## Manual OpenXR Checks

The apply command configures the expected loader, render mode, and controller/profile features through Unity package APIs. Still inspect the Unity UI after package updates or Unity upgrades.

In Project Settings, verify:

- XR Plug-in Management is installed and enabled.
- Android uses the OpenXR loader.
- Standalone uses the OpenXR loader when testing through Meta Quest Link.
- OpenXR features include Meta Quest Support.
- OpenXR interaction profiles include Oculus Touch Controller Profile.
- Stereo rendering shows Single Pass Instanced.

If the validator reports a failure, rerun `Tools > Quest RTS > Apply Recommended Quest Settings`, inspect the Unity UI directly, and correct remaining issues before device testing.

## Quest Link Smoke Test

1. Start the Meta desktop app and enable Quest Link.
2. Confirm OpenXR runtime is active for Meta Quest Link.
3. Open `Assets/Scenes/Battlefield.unity`.
4. Press Play.
5. Force Quest mode for editor testing without a live XR device if needed with `QUEST_RTS_FORCE_MODE=QuestVr` or `-questRtsMode QuestVr`.
6. Confirm the headset pose moves the view without snapping to the desktop command camera.

## Android Development Build

1. Switch platform to Android.
2. Run `Tools > Quest RTS > Apply Recommended Quest Settings`.
3. Verify the manual OpenXR checks above.
4. Enable Development Build and Script Debugging if needed.
5. Build and Run to the connected headset.

## Expected Controller Mapping

- Right controller ray: hover target or terrain.
- X/left primary button: open or close the left-wrist Quest build menu.
- Right trigger: select, or activate a Quest command-console control when the ray is over the console.
- Left trigger held + right trigger: additive select on a friendly entity, or area-select nearby friendly units when aimed at terrain.
- A/right primary button: context command.
- Left trigger held + A/right primary button: attack-move selected units.
- A/right primary button while placing: confirm the current valid placement.
- B/right secondary button while placing: cancel placement.
- B/right secondary button while not placing: clear selection.
- Left trigger held + B/right secondary button: stop selected units.

## Quest Command Console Flow

Open the left-wrist build menu with X/left primary. The menu follows the left controller and uses large holographic selection tiles for structure construction and unit production. Aim at the wrist menu with the right ray and use the right trigger to tap a tile.

- Build tab: choose a structure tile. The tile shows cost, power effect, availability, and any disabled reason. After choosing, aim at the battlefield with the right ray. A confirms a valid preview; B cancels.
- Produce tab: select a Command Center, Barracks, Refinery, or War Factory first. The tab shows trainable unit tiles, cost, build time, queue state, active progress, and a Cancel Production control for queued or active units.
- Selected tab: inspect health, selected counts, queue/rally status, and use Repair or Sell when a player structure is eligible. Rally points are set by selecting a production structure and pressing A on terrain.
- System tab: pause/resume, save the manual slot, load the manual slot when one exists, and start a new match.

## Quest Tactical Map

The tactical map is a non-interactive world-space battle-view panel next to the tabletop. It uses pooled UI pips for resource fields and visible forces, keeps fogged enemies hidden, and has its `GraphicRaycaster` disabled so the right ray continues to select terrain and activate command-console controls normally.

## Manual Smoke Test Checklist

- HMD pose tracking works without camera snapping.
- Both controllers track.
- Battlefield appears about 1.75 to 1.8 meters wide.
- Battlefield appears at tabletop height, not floor height.
- Right ray and reticle align with the controller.
- Trigger selects a friendly unit or structure.
- Left trigger modifier allows additive selection and terrain-centered area selection for nearby friendly units.
- X opens and closes the left-wrist Quest build menu.
- Right trigger activates wrist-menu controls without selecting through the panel.
- Build tab starts placement for every available player structure.
- Placement preview follows the right ray, snaps to valid points, and shows valid/invalid state.
- A confirms valid placement and spends credits once.
- B cancels placement without spending credits.
- Produce tab queues every supported unit from an eligible selected structure.
- Cancel Production refunds the last queued unit, or the active unit when no pending queue item exists.
- Selected tab can repair or sell eligible player structures.
- A moves selected units to terrain.
- Left trigger + A issues attack-move to terrain.
- A attacks a visible enemy.
- A sends a selected harvester to a resource.
- A sets a rally point when only a production building is selected.
- B clears selection or cancels placement.
- Left trigger + B stops selected units.
- System tab can pause/resume, save, load, and start a new match without enabling desktop HUD.
- Desktop overlay HUD and OnGUI minimap are absent in Quest mode.
- World-space status panel is readable and does not cover the board.
- Left-wrist build menu follows the left controller and keeps structure/unit selection tiles readable.
- World-space tactical map is readable, updates resource/unit pips, and does not capture pointer input.
- No GameObject named `Command Camera` is created in Quest mode.
- Basic play has no recurring exceptions.

## Troubleshooting

### Black Screen

- Confirm XR Plug-in Management has OpenXR enabled for the active build target.
- Confirm the Meta app is the active OpenXR runtime for Quest Link.
- Check that the scene is `Assets/Scenes/Battlefield.unity` and that runtime bootstrap has not been disabled.
- For Android, confirm Vulkan is supported and Android modules are installed.

### No Controller Tracking

- Verify Oculus Touch Controller Profile is enabled in OpenXR settings.
- Confirm the headset is not in hand-tracking-only mode.
- Reconnect Quest Link or restart the Android build after changing OpenXR profiles.

### OpenXR Loader Not Active

- Open Project Settings > XR Plug-in Management.
- Enable OpenXR for Standalone or Android as appropriate.
- Restart Play Mode after changing loaders.

### Board Appears At The Wrong Scale

- Inspect the `QuestTabletopSettings` component created at runtime.
- Default `SimulationUnitsPerMeter` is 126.
- The 224-unit board should appear roughly 1.78 meters wide.
- For a room-sized board, use `Command RTS > Profile > Use Room-Sized Quest Tabletop Scale`; it writes a local profile with `tabletopScale` 2.25, making the same battlefield roughly 4.0 meters wide with a longer pointer ray. Use `Command RTS > Profile > Use Default Quest Tabletop Scale` to restore the default.
- Default `BoardHeightMeters` is 0.82, which offsets the rig root so the battlefield plane sits at physical tabletop height above the tracking origin.
- Forced Quest mode without live headset tracking seeds fallback head and controller poses from `QuestTabletopSettings`; live XR tracking should replace those poses as soon as device position and rotation features are available.
- If the board appears on the floor, verify the project includes the `BoardHeightMeters` setting and that `QuestTabletopRig` uses `QuestTabletopSettings.GetRigRootPosition()`.

### Desktop Camera Incorrectly Appearing

- Confirm runtime mode is `QuestVr`.
- Check that `QUEST_RTS_FORCE_MODE` or `-questRtsMode` is not forcing Desktop.
- In Quest mode, no `Command Camera`, `RtsInputController`, or `RtsHud` should be created.

### Input System Actions Not Responding

- This milestone reads OpenXR controller state through Unity XR input feature usages.
- Active Input Handling should still be Both because the project supports legacy desktop input and package-based XR tooling.
- Verify the controller profile and OpenXR loader before debugging actions.

### Console Does Not Open Or Clicks Through

- Confirm the left controller is tracked and X/left primary is available through the Oculus Touch profile.
- In Quest mode, a `Quest Command Console` object should exist under the tabletop rig.
- The right trigger activates console controls only while the right ray intersects the panel. If the ray misses the panel, right trigger returns to battlefield selection.
- If battlefield selection happens while pointing at the panel, verify the app is running the latest scripts and that `QuestCommandConsole.TryHandlePointer` is returning capture for the panel ray.

### Vulkan Or Android Build Errors

- Reinstall Android Build Support, SDK, NDK, and OpenJDK for Unity 2022.3.62f3.
- Let Unity install the recommended SDK/NDK versions.
- Keep ARM64 and IL2CPP enabled for Quest.
- If Vulkan fails on local hardware, validate desktop play first and then test on device with the documented Android settings.
