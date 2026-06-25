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

Then run `Tools > Quest RTS > Validate XR Setup`.

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
5. Force Quest mode for editor testing if needed with `QUEST_RTS_FORCE_MODE=QuestVr` or `-questRtsMode QuestVr`.
6. Confirm the headset pose moves the view without snapping to the desktop command camera.

## Android Development Build

1. Switch platform to Android.
2. Run `Tools > Quest RTS > Apply Recommended Quest Settings`.
3. Verify the manual OpenXR checks above.
4. Enable Development Build and Script Debugging if needed.
5. Build and Run to the connected headset.

## Expected Controller Mapping

- Right controller ray: hover target or terrain.
- Right trigger: select.
- Left trigger held + right trigger: additive select.
- A/right primary button: context command.
- B/right secondary button: cancel placement or clear selection.

## Manual Smoke Test Checklist

- HMD pose tracking works without camera snapping.
- Both controllers track.
- Battlefield appears about 1.75 to 1.8 meters wide.
- Right ray and reticle align with the controller.
- Trigger selects a friendly unit or structure.
- Left trigger modifier allows additive selection.
- A moves selected units to terrain.
- A attacks a visible enemy.
- A sends a selected harvester to a resource.
- A sets a rally point when only a production building is selected.
- B clears selection or cancels placement.
- Desktop overlay HUD and OnGUI minimap are absent in Quest mode.
- World-space status panel is readable and does not cover the board.
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

### Desktop Camera Incorrectly Appearing

- Confirm runtime mode is `QuestVr`.
- Check that `QUEST_RTS_FORCE_MODE` or `-questRtsMode` is not forcing Desktop.
- In Quest mode, no `Command Camera`, `RtsInputController`, or `RtsHud` should be created.

### Input System Actions Not Responding

- This milestone reads OpenXR controller state through Unity XR input feature usages.
- Active Input Handling should still be Both because the project supports legacy desktop input and package-based XR tooling.
- Verify the controller profile and OpenXR loader before debugging actions.

### Vulkan Or Android Build Errors

- Reinstall Android Build Support, SDK, NDK, and OpenJDK for Unity 2022.3.62f3.
- Let Unity install the recommended SDK/NDK versions.
- Keep ARM64 and IL2CPP enabled for Quest.
- If Vulkan fails on local hardware, validate desktop play first and then test on device with the documented Android settings.
