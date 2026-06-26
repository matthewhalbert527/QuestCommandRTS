#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuestCommandRTS.Editor
{
    public sealed class RtsQuestEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            RtsRuntimeModeResolver.ForceModeForTests(null);

            GameObject[] objects = Object.FindObjectsOfType<GameObject>();
            for (int i = objects.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(objects[i]);
            }
        }

        [Test]
        public void RuntimeModeResolverUsesDesktopWhenXrIsNotActive()
        {
            RtsRuntimeModeResolver.ForceModeForTests(null);
            Assert.AreEqual(RtsRuntimeMode.Desktop, RtsRuntimeModeResolver.Resolve());

            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.QuestVr);
            Assert.AreEqual(RtsRuntimeMode.QuestVr, RtsRuntimeModeResolver.Resolve());
        }

        [Test]
        public void RuntimeModeResolverRequiresEnabledOrActiveXrState()
        {
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(false, false, false, RuntimePlatform.WindowsEditor));
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(false, false, true, RuntimePlatform.WindowsEditor));
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(true, false, false, RuntimePlatform.WindowsEditor));
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(true, false, false, RuntimePlatform.Android));
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(true, false, true, RuntimePlatform.WindowsEditor));
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(true, false, true, RuntimePlatform.WindowsPlayer));
            Assert.IsTrue(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(false, true, false, RuntimePlatform.WindowsEditor));
            Assert.IsTrue(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(true, false, true, RuntimePlatform.Android));
        }

        [Test]
        public void RuntimeModeResolverHonorsOverridesBeforeAutomaticXrState()
        {
            Assert.AreEqual(
                RtsRuntimeMode.QuestVr,
                RtsRuntimeModeResolver.ResolveFromStateForTests(
                    new[] { "QuestCommandRTS.exe", "-questRtsMode", "QuestVr" },
                    "Desktop",
                    false,
                    false,
                    false,
                    RuntimePlatform.WindowsPlayer));

            Assert.AreEqual(
                RtsRuntimeMode.Desktop,
                RtsRuntimeModeResolver.ResolveFromStateForTests(
                    new[] { "QuestCommandRTS.exe" },
                    "Desktop",
                    true,
                    true,
                    true,
                    RuntimePlatform.Android));

            Assert.AreEqual(
                RtsRuntimeMode.QuestVr,
                RtsRuntimeModeResolver.ResolveFromStateForTests(
                    new[] { "QuestCommandRTS.exe" },
                    "VR",
                    false,
                    false,
                    false,
                    RuntimePlatform.WindowsEditor));

            Assert.AreEqual(
                RtsRuntimeMode.QuestVr,
                RtsRuntimeModeResolver.ResolveFromStateForTests(
                    new[] { "QuestCommandRTS.exe", "-questRtsMode", "DefinitelyNotAMode" },
                    "DefinitelyNotAMode",
                    true,
                    false,
                    true,
                    RuntimePlatform.Android));
        }

        [Test]
        public void QuestDevicePollingBacksOffWhenDevicesAreMissing()
        {
            Assert.AreEqual(0.5f, QuestTrackedNodePose.DeviceRefreshIntervalSeconds, 0.001f);
            Assert.AreEqual(0.5f, QuestRtsInputController.DeviceRefreshIntervalSeconds, 0.001f);

            Assert.IsFalse(QuestTrackedNodePose.ShouldRefreshDeviceForTests(false, 0.24f, 0.5f));
            Assert.IsTrue(QuestTrackedNodePose.ShouldRefreshDeviceForTests(false, 0.5f, 0.5f));
            Assert.IsFalse(QuestTrackedNodePose.ShouldRefreshDeviceForTests(true, 10f, 0.5f));

            Assert.IsFalse(QuestRtsInputController.ShouldRefreshDeviceForTests(false, 0.24f, 0.5f));
            Assert.IsTrue(QuestRtsInputController.ShouldRefreshDeviceForTests(false, 0.5f, 0.5f));
            Assert.IsFalse(QuestRtsInputController.ShouldRefreshDeviceForTests(true, 10f, 0.5f));
        }

        [Test]
        public void DesktopInitializationCreatesDesktopCameraInputAndHud()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            Assert.AreEqual(RtsRuntimeMode.Desktop, game.RuntimeMode);
            Assert.IsNotNull(game.CommandCamera);
            Assert.IsNotNull(game.GetComponent<RtsInputController>());
            Assert.IsNotNull(game.GetComponent<RtsHud>());
            Assert.IsNull(game.GetComponent<QuestRtsInputController>());
            Assert.IsNull(game.QuestRig);
            Assert.AreSame(game.CommandCamera.transform, game.GetViewCameraTransform());
        }

        [Test]
        public void DesktopHudMainAndPauseMenusControlUserPause()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsHud hud = game.GetComponent<RtsHud>();
            Assert.IsNotNull(hud);

            hud.ShowMainMenuForTests();
            Assert.IsTrue(hud.IsMainMenuVisibleForTests);
            Assert.IsTrue(game.IsUserPaused);

            hud.StartSkirmishFromMainMenuForTests();
            Assert.IsFalse(hud.IsMainMenuVisibleForTests);
            Assert.IsFalse(game.IsUserPaused);

            game.SetUserPaused(true);
            hud.RefreshMenuPanelsForTests();
            Assert.IsTrue(hud.IsPauseMenuVisibleForTests);
        }

        [Test]
        public void DesktopHudSkirmishOptionsApplyToNewMatch()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsHud hud = game.GetComponent<RtsHud>();
            Assert.IsNotNull(hud);

            hud.ShowMainMenuForTests();
            hud.CycleSkirmishDifficultyForTests();
            hud.CycleSkirmishCreditsForTests();
            hud.CycleSkirmishPeaceTimeForTests();
            hud.CycleSkirmishGameSpeedForTests();
            hud.CycleSkirmishFogForTests();
            hud.CycleSkirmishStartingForcesForTests();

            hud.StartSkirmishFromMainMenuForTests();

            Assert.AreEqual(RtsAiDifficulty.Veteran, game.SkirmishOptions.difficulty);
            Assert.AreEqual(RtsStartingCreditsPreset.High, game.SkirmishOptions.startingCredits);
            Assert.AreEqual(RtsPeaceTimePreset.FiveMinutes, game.SkirmishOptions.peaceTime);
            Assert.AreEqual(RtsGameSpeedPreset.Fast, game.SkirmishOptions.gameSpeed);
            Assert.AreEqual(RtsFogPreset.Revealed, game.SkirmishOptions.fog);
            Assert.AreEqual(RtsStartingForcesPreset.ScoutTeam, game.SkirmishOptions.startingForces);
            Assert.AreEqual(10000, game.Resources.Credits);
            Assert.AreEqual(3400, game.EnemyDirector.EnemyCreditsForTests);
            Assert.AreEqual(1.2f, game.Clock.TimeScale, 0.001f);
            Assert.IsFalse(game.FogOfWar.IsEnabled);
            Assert.AreEqual(2, CountLivingPlayerUnits(game));
        }

        [Test]
        public void DesktopInitializationParentsGeneratedCameraLightAndEventSystemUnderRuntimeRoot()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            GameObject sunObject = GameObject.Find("Sun");
            GameObject eventSystemObject = GameObject.Find("EventSystem");
            Assert.IsNotNull(sunObject);
            Assert.IsNotNull(eventSystemObject);
            Light sun = sunObject.GetComponent<Light>();
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();

            Assert.IsNotNull(game.CommandCamera);
            Assert.IsTrue(game.CommandCamera.transform.IsChildOf(game.transform), "Generated desktop camera should be cleaned up with the runtime root.");
            Assert.IsNotNull(sun);
            Assert.IsTrue(sun.transform.IsChildOf(game.transform), "Generated scene light should be cleaned up with the runtime root.");
            Assert.IsNotNull(eventSystem);
            Assert.IsNotNull(eventSystemObject.GetComponent<StandaloneInputModule>());
            Assert.IsTrue(eventSystem.transform.IsChildOf(game.transform), "Generated desktop EventSystem should be cleaned up with the runtime root.");
        }

        [Test]
        public void ForcedQuestInitializationSkipsDesktopCameraInputAndHud()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);

            Assert.AreEqual(RtsRuntimeMode.QuestVr, game.RuntimeMode);
            Assert.IsNull(game.CommandCamera);
            Assert.IsNull(GameObject.Find("Command Camera"));
            Assert.IsNull(game.GetComponent<RtsInputController>());
            Assert.IsNull(game.GetComponent<RtsHud>());
            Assert.IsNotNull(game.GetComponent<QuestRtsInputController>());
            Assert.IsNotNull(game.GetComponent<QuestWorldHud>());
            Assert.IsNotNull(game.GetComponent<QuestTacticalMap>());
            Assert.IsNotNull(game.GetComponent<QuestCommandConsole>());
            Assert.IsNotNull(game.QuestRig);
            Assert.IsNotNull(game.QuestRig.TacticalMap);
            Assert.AreSame(game.QuestRig.HeadCamera.transform, game.GetViewCameraTransform());
        }

        [Test]
        public void QuestInitializationParentsGeneratedLightUnderRuntimeRootWithoutCommandCamera()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            GameObject sunObject = GameObject.Find("Sun");
            Assert.IsNotNull(sunObject);
            Light sun = sunObject.GetComponent<Light>();

            Assert.IsNull(game.CommandCamera);
            Assert.IsNull(GameObject.Find("Command Camera"));
            Assert.IsNotNull(sun);
            Assert.IsTrue(sun.transform.IsChildOf(game.transform), "Generated scene light should be cleaned up with the Quest runtime root.");
        }

        [Test]
        public void QuestWorldStatusPanelUsesWorldSpaceAndRequiredHints()
        {
            CreateInitializedGame(RtsRuntimeMode.QuestVr);

            GameObject statusObject = GameObject.Find("Quest World Status");
            Assert.IsNotNull(statusObject);

            Canvas canvas = statusObject.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);

            GraphicRaycaster raycaster = statusObject.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(raycaster);
            Assert.IsFalse(raycaster.enabled);

            Text text = statusObject.GetComponentInChildren<Text>();
            Assert.IsNotNull(text);
            StringAssert.Contains("Credits ", text.text);
            StringAssert.Contains("Power ", text.text);
            StringAssert.Contains("Selected ", text.text);
            StringAssert.Contains("Trigger: Select", text.text);
            StringAssert.Contains("Left Trigger + Trigger: Add/Area", text.text);
            StringAssert.Contains("A: Command", text.text);
            StringAssert.Contains("B: Cancel/Clear", text.text);
            StringAssert.Contains("X: Command Console", text.text);
        }

        [Test]
        public void QuestWorldStatusPanelRefreshesOnlyWhenDisplayedStateChanges()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestWorldHud hud = game.GetComponent<QuestWorldHud>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(hud.StatusTextForTests);

            hud.RefreshForTests(false);
            string initial = hud.StatusTextForTests.text;
            hud.RefreshForTests(false);
            Assert.AreSame(initial, hud.StatusTextForTests.text);

            Assert.IsTrue(game.Resources.TrySpend(100));
            hud.RefreshForTests(false);
            string afterCredits = hud.StatusTextForTests.text;
            Assert.AreNotSame(initial, afterCredits);
            StringAssert.Contains("Credits 6100", afterCredits);

            game.ClearSelection();
            hud.RefreshForTests(false);
            string afterSelection = hud.StatusTextForTests.text;
            Assert.AreNotSame(afterCredits, afterSelection);
            StringAssert.Contains("Selected 0", afterSelection);

            game.SetUserPaused(true);
            hud.RefreshForTests(false);
            string afterPause = hud.StatusTextForTests.text;
            Assert.AreNotSame(afterSelection, afterPause);
            StringAssert.Contains("Paused", afterPause);

            hud.RefreshForTests(false);
            Assert.AreSame(afterPause, hud.StatusTextForTests.text);
        }

        [Test]
        public void QuestTacticalMapUsesWorldSpacePooledPipsAndNoPointerCapture()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestTacticalMap tacticalMap = game.GetComponent<QuestTacticalMap>();
            Assert.IsNotNull(tacticalMap);

            GameObject mapObject = GameObject.Find("Quest Tactical Map");
            Assert.IsNotNull(mapObject);

            Canvas canvas = mapObject.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);

            GraphicRaycaster raycaster = mapObject.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(raycaster);
            Assert.IsFalse(raycaster.enabled);

            Assert.IsNotNull(tacticalMap.PanelRectForTests);
            Assert.IsNotNull(tacticalMap.MapRectForTests);
            Assert.AreEqual(QuestTacticalMap.MaxEntityPips, tacticalMap.EntityPipCapacityForTests);
            Assert.AreEqual(QuestTacticalMap.MaxResourcePips, tacticalMap.ResourcePipCapacityForTests);
            Assert.Greater(tacticalMap.VisibleEntityPipCountForTests, 0);
            Assert.Greater(tacticalMap.VisibleResourcePipCountForTests, 0);
            AssertPanelImage("Tactical Map Plot Area", 0.8f);

            Text header = FindRectTransform("Tactical Map Header").GetComponent<Text>();
            Assert.IsNotNull(header);
            StringAssert.Contains("TACTICAL MAP", header.text);
        }

        [Test]
        public void QuestTacticalMapRefreshHidesDepletedResources()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestTacticalMap tacticalMap = game.GetComponent<QuestTacticalMap>();
            Assert.IsNotNull(tacticalMap);

            tacticalMap.RefreshForTests(true);
            int beforeDepletion = tacticalMap.VisibleResourcePipCountForTests;
            Assert.Greater(beforeDepletion, 0);

            ResourceNode node = game.ResourceNodes[0];
            node.Harvest(node.Amount);

            tacticalMap.RefreshForTests(true);

            Assert.AreEqual(beforeDepletion - 1, tacticalMap.VisibleResourcePipCountForTests);
        }

        [Test]
        public void QuestCommandConsoleBuildsLayeredHolographicPanelFrame()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);
            Assert.IsNotNull(game.QuestRig);
            Assert.IsNotNull(game.QuestRig.LeftController);

            console.SetOpen(true);

            Assert.IsNotNull(console.WristAnchor);
            Assert.AreEqual("Left Wrist Build Menu Anchor", console.WristAnchor.name);
            Assert.IsTrue(console.WristAnchor.IsChildOf(game.QuestRig.LeftController), "Quest command console should be anchored to the left wrist/controller.");
            Assert.IsTrue(console.PanelRect.transform.IsChildOf(console.WristAnchor), "Panel should follow the left wrist anchor.");

            AssertPanelImage("Console Top Glow", 0.7f);
            AssertPanelImage("Console Left Glow", 0.5f);
            AssertPanelImage("Console Header Underline", 0.4f);
            AssertPanelImage("Console Tab Rail", 0.5f);
            AssertPanelImage("Console Content Backplate", 0.4f);

            Text header = FindRectTransform("Console Header").GetComponent<Text>();
            Assert.IsNotNull(header);
            StringAssert.Contains("RTS COMMAND", header.text);
        }

        [Test]
        public void QuestCommandConsoleUsesLargeSelectionTilesForBuildAndProduction()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            console.SetOpen(true);

            Image commandCenterIcon = AssertPanelImage("Build Row 0 Icon", 0.6f);
            Image powerIcon = AssertPanelImage("Build Row 1 Icon", 0.6f);
            Assert.AreNotEqual(commandCenterIcon.color, powerIcon.color);
            Assert.GreaterOrEqual(commandCenterIcon.rectTransform.rect.width, 120f);
            Assert.GreaterOrEqual(commandCenterIcon.rectTransform.rect.height, 40f);
            AssertPanelImage("Build Row 0 Tile Glow", 0.18f);
            AssertPanelImage("Build Row 7 Icon", 0.6f);

            ClickConsoleButton(console, "Produce Tab");

            Image gunnerIcon = AssertPanelImage("Produce Row 0 Icon", 0.6f);
            Image harvesterIcon = AssertPanelImage("Produce Row 5 Icon", 0.6f);
            Image heavyTankIcon = AssertPanelImage("Produce Row 8 Icon", 0.6f);
            Assert.AreNotEqual(gunnerIcon.color, harvesterIcon.color);
            Assert.AreNotEqual(harvesterIcon.color, heavyTankIcon.color);
            AssertPanelImage("Production Queue Backplate", 0.7f);
        }

        [Test]
        public void TankVariantsUseImportedBastionModelPayloads()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            RtsUnit light = game.CreateUnit(RtsTeam.Player, UnitKind.LightTank, new Vector3(-52f, 0f, -44f));
            RtsUnit medium = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-48f, 0f, -44f));
            RtsUnit heavy = game.CreateUnit(RtsTeam.Player, UnitKind.HeavyTank, new Vector3(-44f, 0f, -44f));

            Assert.AreEqual(UnitKind.LightTank, light.UnitKind);
            Assert.AreEqual(UnitKind.MediumTank, medium.UnitKind);
            Assert.AreEqual(UnitKind.HeavyTank, heavy.UnitKind);
            Assert.IsNotNull(light.transform.Find("Light Tank Model"));
            Assert.IsNotNull(medium.transform.Find("Medium Tank Model"));
            Assert.IsNotNull(heavy.transform.Find("Heavy Tank Model"));
            Assert.IsNotNull(light.transform.Find("Tank Team Roof Plate"));
            Assert.IsNotNull(medium.transform.Find("Tank Team Left Plate"));
            Assert.IsNotNull(heavy.transform.Find("Tank Armor Detail Plate"));
        }

        [Test]
        public void InfantryVariantsUseImportedBastionModelPayloads()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            RtsUnit gunner = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-52f, 0f, -48f));
            RtsUnit grenadier = game.CreateUnit(RtsTeam.Player, UnitKind.Grenadier, new Vector3(-50f, 0f, -48f));
            RtsUnit rocket = game.CreateUnit(RtsTeam.Player, UnitKind.RocketSoldier, new Vector3(-48f, 0f, -48f));
            RtsUnit flamer = game.CreateUnit(RtsTeam.Player, UnitKind.FlameTrooper, new Vector3(-46f, 0f, -48f));
            RtsUnit engineer = game.CreateUnit(RtsTeam.Player, UnitKind.Engineer, new Vector3(-44f, 0f, -48f));

            Assert.AreEqual("Gunner", RtsBalance.GetUnit(UnitKind.Rifleman).Name);
            Assert.IsNotNull(gunner.transform.Find("Gunner Model"));
            Assert.IsNotNull(grenadier.transform.Find("Grenadier Model"));
            Assert.IsNotNull(rocket.transform.Find("Rocket Soldier Model"));
            Assert.IsNotNull(flamer.transform.Find("Flame Trooper Model"));
            Assert.IsNotNull(engineer.transform.Find("Engineer Model"));
            Assert.IsNotNull(gunner.transform.Find("Infantry Team Top Plate"));
            Assert.IsNotNull(engineer.transform.Find("Infantry Kit Detail Plate"));
        }

        [Test]
        public void GeneratedUnitsInstallProceduralMotionRigs()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            RtsUnit rifleman = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-52f, 0f, -50f));
            RtsUnit medium = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-48f, 0f, -50f));
            RtsUnit harvester = game.CreateUnit(RtsTeam.Player, UnitKind.Harvester, new Vector3(-44f, 0f, -50f));

            RtsUnitVisualAnimator infantryAnimator = rifleman.GetComponent<RtsUnitVisualAnimator>();
            RtsUnitVisualAnimator tankAnimator = medium.GetComponent<RtsUnitVisualAnimator>();
            RtsUnitVisualAnimator harvesterAnimator = harvester.GetComponent<RtsUnitVisualAnimator>();

            Assert.IsNotNull(infantryAnimator);
            Assert.IsTrue(infantryAnimator.HasLegRigForTests);
            Assert.IsFalse(infantryAnimator.HasTurretRigForTests);
            Assert.IsNotNull(tankAnimator);
            Assert.IsTrue(tankAnimator.HasTrackRigForTests);
            Assert.IsTrue(tankAnimator.HasTurretRigForTests);
            Assert.IsNotNull(harvesterAnimator);
            Assert.IsTrue(harvesterAnimator.HasTrackRigForTests);
            Assert.IsFalse(harvesterAnimator.HasRoundWheelRigForTests);
            Assert.IsFalse(harvesterAnimator.HasTurretRigForTests);
        }

        [Test]
        public void ProceduralVisualRigsAnimateMovementAndTurretAim()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            RtsUnit rifleman = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-52f, 0f, -52f));
            RtsUnitVisualAnimator infantryAnimator = rifleman.GetComponent<RtsUnitVisualAnimator>();
            Transform leg = infantryAnimator.FirstLegForTests;
            Quaternion legStart = leg.localRotation;

            rifleman.transform.position += rifleman.transform.forward * 1.2f;
            infantryAnimator.TickVisualsForTests(0.2f);
            Assert.Greater(Quaternion.Angle(legStart, leg.localRotation), 1f);

            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-48f, 0f, -52f));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, tank.transform.position + tank.transform.right * 6f);
            RtsUnitVisualAnimator tankAnimator = tank.GetComponent<RtsUnitVisualAnimator>();
            Transform track = tankAnimator.FirstTrackPadForTests;
            Transform turret = tankAnimator.TurretPivotForTests;
            Vector3 trackStart = track.localPosition;
            Quaternion turretStart = turret.localRotation;

            tank.transform.position += tank.transform.forward * 1.5f;
            tank.IssueAttack(enemy);
            tankAnimator.TickVisualsForTests(0.2f);

            Assert.Greater((track.localPosition - trackStart).sqrMagnitude, 0.0001f);
            Assert.Greater(Quaternion.Angle(turretStart, turret.localRotation), 1f);
        }

        [Test]
        public void ImportedModelPalettesAreNotOverriddenByTeamTint()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.LightTank, new Vector3(-52f, 0f, -54f));
            Transform model = tank.transform.Find("Light Tank Model");
            Transform teamPlate = tank.transform.Find("Tank Team Roof Plate");
            Assert.IsNotNull(model);
            Assert.IsNotNull(teamPlate);

            Renderer modelRenderer = model.GetComponentInChildren<Renderer>();
            Renderer teamRenderer = teamPlate.GetComponent<Renderer>();
            Assert.IsNotNull(modelRenderer);
            Assert.IsNotNull(teamRenderer);
            Assert.IsNull(modelRenderer.GetComponent<RtsTeamTintTarget>(), "Imported model renderers should keep their Bastion palette instead of receiving team tint.");
            Assert.IsNotNull(teamRenderer.GetComponent<RtsTeamTintTarget>(), "Only explicit team plates should receive the team tint property block.");
        }

        [Test]
        public void ProducedUnitsStartInsideProductionBuildingAndExit()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            ProductionStructure barracks = game.CreateStructure(RtsTeam.Player, StructureKind.Barracks, new Vector3(-76f, 0f, -62f)) as ProductionStructure;
            Assert.IsNotNull(barracks);

            RtsUnit produced = barracks.SpawnProducedUnitForTests(UnitKind.Rifleman, null);
            Assert.IsNotNull(produced);

            Vector3 offset = produced.transform.position - barracks.transform.position;
            offset.y = 0f;
            Assert.Less(offset.magnitude, barracks.FootprintRadius, "Produced infantry should begin inside the producer footprint.");

            RtsUnitOrderSaveData order = produced.CaptureOrderState();
            Assert.AreEqual("Move", order.orderType);
            Vector3 destination = order.destination.ToVector3();
            Vector3 exitOffset = destination - barracks.transform.position;
            exitOffset.y = 0f;
            Assert.Greater(exitOffset.magnitude, barracks.FootprintRadius + 1.5f, "Produced units should receive an exit movement target outside the building.");
        }

        [Test]
        public void IdleCombatUnitsAutoAttackNearbyEnemies()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsUnit gunner = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-12f, 0f, -12f));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(-8f, 0f, -12f));

            float enemyHealth = enemy.Health;
            gunner.TickOrdersForTests(0.1f);

            Assert.Less(enemy.Health, enemyHealth);
            Assert.AreEqual("Attack", gunner.CaptureOrderState().orderType);
            Assert.AreEqual(enemy.PersistentId, gunner.CaptureOrderState().targetEntityId);
        }

        [Test]
        public void CombatUnitsRetaliateAgainstAttackers()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsUnit gunner = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-16f, 0f, -16f));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(-12.5f, 0f, -16f));

            gunner.IssueMove(new Vector3(-32f, 0f, -16f));
            gunner.TakeDamage(1f, enemy);
            float enemyHealth = enemy.Health;
            gunner.TickOrdersForTests(0.1f);

            Assert.Less(enemy.Health, enemyHealth);
            Assert.AreEqual("Attack", gunner.CaptureOrderState().orderType);
            Assert.AreEqual(enemy.PersistentId, gunner.CaptureOrderState().targetEntityId);
        }

        [Test]
        public void StructuresUseImportedBastionModelPayloads()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            RtsStructure command = game.CreateStructure(RtsTeam.Player, StructureKind.CommandCenter, new Vector3(-60f, 0f, -40f));
            RtsStructure refinery = game.CreateStructure(RtsTeam.Player, StructureKind.Refinery, new Vector3(-52f, 0f, -40f));
            RtsStructure barracks = game.CreateStructure(RtsTeam.Player, StructureKind.Barracks, new Vector3(-44f, 0f, -40f));
            RtsStructure warFactory = game.CreateStructure(RtsTeam.Player, StructureKind.WarFactory, new Vector3(-36f, 0f, -40f));
            RtsStructure powerPlant = game.CreateStructure(RtsTeam.Player, StructureKind.PowerPlant, new Vector3(-28f, 0f, -40f));
            RtsStructure turret = game.CreateStructure(RtsTeam.Player, StructureKind.Turret, new Vector3(-20f, 0f, -40f));
            RtsStructure gunTower = game.CreateStructure(RtsTeam.Player, StructureKind.GunTower, new Vector3(-12f, 0f, -40f));
            RtsStructure advanced = game.CreateStructure(RtsTeam.Player, StructureKind.AdvancedGunTower, new Vector3(-4f, 0f, -40f));
            RtsUnit harvester = game.CreateUnit(RtsTeam.Player, UnitKind.Harvester, new Vector3(4f, 0f, -40f));

            Assert.IsNotNull(Resources.Load<GameObject>("StructureModels/BastionFabricationHub/Meshes/Bastion_FabricationHub_Static"));
            Assert.IsNotNull(command.transform.Find("Command Center Model"));
            Assert.IsNotNull(refinery.transform.Find("Refinery Model"));
            Assert.IsNotNull(barracks.transform.Find("Barracks Model"));
            Assert.IsNotNull(warFactory.transform.Find("War Factory Model"));
            Assert.IsNotNull(powerPlant.transform.Find("Power Plant Model"));
            Assert.IsNotNull(turret.transform.Find("Guard Turret Model"));
            Assert.IsNotNull(gunTower.transform.Find("Gun Tower Model"));
            Assert.IsNotNull(advanced.transform.Find("Advanced Gun Tower Model"));
            Assert.IsNotNull(harvester.transform.Find("Harvester Model"));
            Assert.IsNotNull(command.transform.Find("Structure Team Roof Plate"));
            Assert.IsNotNull(refinery.transform.Find("Structure Armor Detail Plate"));
            Assert.IsNotNull(advanced.transform.Find("Structure Team Left Plate"));
            Assert.IsNotNull(harvester.transform.Find("Harvester Team Roof Plate"));
            Assert.IsInstanceOf<TurretStructure>(turret);
            Assert.IsInstanceOf<TurretStructure>(gunTower);
            Assert.IsInstanceOf<TurretStructure>(advanced);
        }

        [Test]
        public void EngineerRepairsDamagedFriendlyTargets()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsUnit engineer = game.CreateUnit(RtsTeam.Player, UnitKind.Engineer, new Vector3(-58f, 0f, -62f));
            RtsStructure powerPlant = FindStructure(game, RtsTeam.Player, StructureKind.PowerPlant);
            powerPlant.TakeDamage(120f, null);

            game.ClearSelection();
            game.SelectEntity(engineer, false);

            Assert.AreEqual(RtsContextCommandKind.Repair, game.CommandDispatcher.ResolveContextCommand(powerPlant, null, powerPlant.transform.position));

            float damagedHealth = powerPlant.Health;
            engineer.IssueRepair(powerPlant);
            for (int i = 0; i < 120 && powerPlant.Health <= damagedHealth; i++)
            {
                engineer.TickOrdersForTests(0.1f);
            }

            Assert.Greater(powerPlant.Health, damagedHealth);
        }

        [Test]
        public void MediumTankBoardsRiflemanAndFiresPassengerWeapon()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            MediumTankUnit medium = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-52f, 0f, -42f)) as MediumTankUnit;
            RtsUnit rifleman = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, medium.transform.position + new Vector3(3.2f, 0f, 0f));
            Assert.IsNotNull(medium);

            game.ClearSelection();
            game.SelectEntity(rifleman, false);
            Assert.AreEqual(RtsContextCommandKind.Board, game.CommandDispatcher.ResolveContextCommand(medium, null, medium.transform.position));

            rifleman.IssueBoardMediumTank(medium);
            for (int i = 0; i < 80 && medium.LoadedRiflemen == 0; i++)
            {
                rifleman.TickOrdersForTests(0.1f);
            }

            Assert.AreEqual(1, medium.LoadedRiflemen);
            Assert.IsFalse(HasEntity(game, rifleman), "Boarded rifleman should leave the active entity list.");

            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, medium.transform.position + new Vector3(5.5f, 0f, 0f));
            float expectedDamage = medium.Damage + RtsBalance.GetUnit(UnitKind.Rifleman).Damage;
            float healthBefore = enemy.Health;

            medium.IssueAttack(enemy);
            medium.TickOrdersForTests(0.1f);

            Assert.AreEqual(healthBefore - expectedDamage, enemy.Health, 0.001f);
        }

        [Test]
        public void EntityHealthBarsTrackSelectionDamageRepairAndFog()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsUnit rifle = FindPlayerUnit(game, UnitKind.Rifleman);
            Transform healthBar = rifle.transform.Find("Health Bar");
            Transform fill = healthBar != null ? healthBar.Find("Health Fill") : null;
            Assert.IsNotNull(healthBar);
            Assert.IsNotNull(fill);
            Assert.IsTrue(healthBar.gameObject.activeSelf, "Initially selected rifle should show a health bar.");

            game.ClearSelection();
            Assert.IsFalse(healthBar.gameObject.activeSelf, "Full-health unselected unit should hide its health bar.");

            rifle.TakeDamage(rifle.MaxHealth * 0.5f, null);
            Assert.IsTrue(healthBar.gameObject.activeSelf, "Damaged unit should show its health bar.");
            Assert.AreEqual(rifle.HealthPercent, fill.localScale.x, 0.001f);

            rifle.Repair(rifle.MaxHealth);
            Assert.IsFalse(healthBar.gameObject.activeSelf, "Fully repaired unselected unit should hide its health bar.");

            RtsUnit enemy = FindEnemyUnit(game, UnitKind.HeavyTank);
            Transform enemyHealthBar = enemy.transform.Find("Health Bar");
            Assert.IsNotNull(enemyHealthBar);
            Assert.IsFalse(game.IsEntityVisible(enemy));
            enemy.TakeDamage(1f, null);
            Assert.IsFalse(enemyHealthBar.gameObject.activeSelf, "Fogged enemy health bars should stay hidden.");

            RtsUnit scout = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, enemy.transform.position + new Vector3(4f, 0f, 0f));
            game.FogOfWar.RefreshNowForTests();
            Assert.IsTrue(game.IsEntityVisible(enemy));
            Assert.IsTrue(enemyHealthBar.gameObject.activeSelf, "Damaged enemy health bars should appear after fog reveals them.");

            scout.transform.position = new Vector3(-100f, 0f, -100f);
            game.FogOfWar.RefreshNowForTests();
            Assert.IsFalse(game.IsEntityVisible(enemy));
            Assert.IsFalse(enemyHealthBar.gameObject.activeSelf, "Damaged enemy health bars should hide again when fog covers them.");
        }

        [Test]
        public void GeneratedBattlefieldSetDressingDoesNotBlockPointerRaycasts()
        {
            CreateInitializedGame(RtsRuntimeMode.Desktop);

            AssertSceneObjectHasNoCollider("Projected Water Channel A");
            AssertSceneObjectHasNoCollider("Projected Water Channel B");
            AssertSceneObjectHasNoCollider("West Mesa Ridge");
            AssertSceneObjectHasNoCollider("Southwest Rock 1");
            AssertSceneObjectHasNoCollider("South Canyon Cliff Face");
            AssertSceneObjectHasNoCollider("Northwest Mountain Peak 1");
            AssertSceneObjectHasNoCollider("Central Dry Wash");
            AssertSceneObjectHasNoCollider("Northwest Table Pylon");

            RaycastHit hit;
            Assert.IsTrue(Physics.Raycast(new Ray(new Vector3(8f, 18f, -6f), Vector3.down), out hit, 500f));
            Assert.AreEqual("Battlefield", hit.collider.gameObject.name);
        }

        [Test]
        public void GeneratedBattlefieldUsesProceduralGroundTextures()
        {
            CreateInitializedGame(RtsRuntimeMode.Desktop);

            AssertSceneObjectUsesTexture("Battlefield", "Sand Ground");
            AssertSceneObjectUsesTexture("West Dune Shelf", "Dune Accent");
            AssertSceneObjectUsesTexture("Projected Water Channel A", "Water Ripple");
            AssertSceneObjectUsesTexture("West Mesa Ridge", "Ridge Rock");
            AssertSceneObjectUsesTexture("South Canyon Cliff Face", "Cliff Face");
            AssertSceneObjectUsesTexture("Northwest Mountain Peak 1", "Mountain Stone");
            AssertSceneObjectUsesTexture("South Canyon Cliff Talus Fan 1", "Talus");
            AssertSceneObjectUsesTexture("Central Dry Wash", "Dry Wash");
            AssertSceneObjectUsesTexture("Southwest Blast Scorch", "Scorch");
        }

        [Test]
        public void FogOfWarUsesSingleTextureOverlayMappedToWorldCells()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            Assert.IsNotNull(game.FogOfWar);
            Assert.IsTrue(game.FogOfWar.HasFogTextureForTests);
            Assert.AreEqual(1, game.FogOfWar.FogRendererCountForTests);

            Color playerBaseFog = game.FogOfWar.GetFogTextureColorForTests(game.GetPlayerBaseCenter());
            Assert.Less(playerBaseFog.a, 0.05f, "Player base cells should be fully visible in the fog overlay texture.");

            Color enemyBaseFog = game.FogOfWar.GetFogTextureColorForTests(game.GetEnemyBaseCenter());
            Assert.Greater(enemyBaseFog.a, 0.7f, "Enemy base cells should remain unexplored at match start.");
        }

        [Test]
        public void ForcedQuestInitializationMakesHeadCameraMainAndDisablesSceneCameras()
        {
            GameObject sceneCameraObject = new GameObject("Scene Main Camera");
            sceneCameraObject.tag = "MainCamera";
            Camera sceneCamera = sceneCameraObject.AddComponent<Camera>();

            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);

            Assert.IsFalse(sceneCamera.enabled);
            Assert.AreEqual("Untagged", sceneCameraObject.tag);
            Assert.AreEqual("MainCamera", game.QuestRig.HeadCamera.gameObject.tag);
            Assert.AreSame(game.QuestRig.HeadCamera, Camera.main);
        }

        [Test]
        public void QuestInitializationScalesCameraClipPlanesWithTabletop()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();

            Assert.IsNotNull(settings);
            Assert.IsNotNull(game.QuestRig.HeadCamera);
            Assert.AreEqual(126f, settings.SimulationUnitsPerMeter, 0.001f);
            Assert.AreEqual(settings.CameraNearClipSimulationUnits, game.QuestRig.HeadCamera.nearClipPlane, 0.001f);
            Assert.AreEqual(settings.CameraFarClipSimulationUnits, game.QuestRig.HeadCamera.farClipPlane, 0.001f);
            Assert.Greater(game.QuestRig.HeadCamera.farClipPlane, RtsBalance.MapHalfSize * 2f);
        }

        [Test]
        public void QuestInitializationPlacesBattlefieldAtTabletopHeight()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();

            Assert.IsNotNull(settings);
            Assert.IsNotNull(game.QuestRig);
            Assert.AreEqual(0.82f, settings.BoardHeightMeters, 0.001f);
            Assert.AreEqual(1.78f, settings.BattlefieldWidthMeters, 0.01f);
            Assert.AreEqual(settings.GetRigRootPosition(), game.QuestRig.RigRoot.position);
            Assert.AreEqual(-settings.BoardHeightSimulationUnits, game.QuestRig.RigRoot.position.y, 0.001f);
        }

        [Test]
        public void QuestInitializationAppliesInjectedRoomSizedProfileBeforeRigCreation()
        {
            RtsProfileSettings profile = CreateProfileSettingsForTests(RtsProfileSettingsData.RoomSizedTabletopScale, RtsProfileSettingsData.RoomSizedPointerLength);
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr, profile);
            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();

            Assert.AreSame(profile, game.ProfileSettings);
            Assert.IsNotNull(settings);
            Assert.AreEqual(126f / RtsProfileSettingsData.RoomSizedTabletopScale, settings.SimulationUnitsPerMeter, 0.001f);
            Assert.AreEqual(4f, settings.BattlefieldWidthMeters, 0.01f);
            Assert.AreEqual(RtsProfileSettingsData.RoomSizedPointerLength, settings.RayLengthMeters, 0.001f);
            Assert.AreEqual(RtsProfileSettingsData.RoomSizedPointerLength * settings.SimulationUnitsPerMeter, settings.RayLengthSimulationUnits, 0.001f);
            AssertVectorNear(Vector3.one * settings.SimulationUnitsPerMeter, game.QuestRig.RigRoot.localScale);
            Assert.AreEqual(settings.GetRigRootPosition(), game.QuestRig.RigRoot.position);
            Assert.Greater(game.QuestRig.HeadCamera.farClipPlane, RtsBalance.MapHalfSize * 2f);
        }

        [Test]
        public void QuestInitializationSeedsFallbackTrackedPoses()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();

            Assert.IsNotNull(settings);
            AssertVectorNear(settings.FallbackHeadLocalPositionMeters, game.QuestRig.Head.localPosition);
            AssertVectorNear(settings.FallbackLeftControllerLocalPositionMeters, game.QuestRig.LeftController.localPosition);
            AssertVectorNear(settings.FallbackRightControllerLocalPositionMeters, game.QuestRig.RightController.localPosition);

            Vector3 expectedHeadWorld = settings.GetRigRootPosition() + settings.FallbackHeadLocalPositionMeters * settings.SimulationUnitsPerMeter;
            AssertVectorNear(expectedHeadWorld, game.QuestRig.HeadCamera.transform.position);
            Assert.Greater(game.QuestRig.HeadCamera.transform.position.y, 80f);
            Assert.Less(game.QuestRig.HeadCamera.transform.position.z, -140f);
            Assert.Less(game.QuestRig.RightController.forward.y, -0.1f);
            Assert.Greater(game.QuestRig.RightController.forward.z, 0.9f);
        }

        [Test]
        public void QuestTrackedPoseAppliesOnlyAvailableDeviceFeatures()
        {
            Transform tracked = new GameObject("Tracked Pose Test").transform;
            Vector3 fallbackPosition = new Vector3(0.1f, 1.4f, 0.25f);
            Quaternion fallbackRotation = Quaternion.Euler(12f, 4f, -3f);
            tracked.localPosition = fallbackPosition;
            tracked.localRotation = fallbackRotation;

            QuestTrackedNodePose.ApplyPoseForTests(tracked, false, new Vector3(8f, 8f, 8f), false, Quaternion.Euler(60f, 70f, 80f));
            AssertVectorNear(fallbackPosition, tracked.localPosition);
            AssertQuaternionNear(fallbackRotation, tracked.localRotation);

            Vector3 livePosition = new Vector3(-0.2f, 1.2f, 0.45f);
            QuestTrackedNodePose.ApplyPoseForTests(tracked, true, livePosition, false, Quaternion.Euler(40f, 50f, 60f));
            AssertVectorNear(livePosition, tracked.localPosition);
            AssertQuaternionNear(fallbackRotation, tracked.localRotation);

            Quaternion liveRotation = Quaternion.Euler(1f, 88f, 5f);
            QuestTrackedNodePose.ApplyPoseForTests(tracked, false, new Vector3(3f, 3f, 3f), true, liveRotation);
            AssertVectorNear(livePosition, tracked.localPosition);
            AssertQuaternionNear(liveRotation, tracked.localRotation);
        }

        [Test]
        public void QuestValidatorReportsCorePackageAndAndroidSettings()
        {
            var report = QuestXrProjectValidator.BuildValidationReport();

            AssertValidationPassed(report, "Unity version");
            AssertValidationPassed(report, "XR Management package");
            AssertValidationPassed(report, "OpenXR package");
            AssertValidationPassed(report, "XR Interaction Toolkit package");
            AssertValidationPassed(report, "Input System package");
            AssertValidationPassed(report, "Resolved XR Management package");
            AssertValidationPassed(report, "Resolved OpenXR package");
            AssertValidationPassed(report, "Resolved XR Interaction Toolkit package");
            AssertValidationPassed(report, "Resolved Input System package");
            AssertValidationPassed(report, "Forbidden XR packages absent");
            AssertValidationExists(report, "Android Build Support");
            AssertValidationPassed(report, "Android min API");
            AssertValidationPassed(report, "Android target API");
            AssertValidationPassed(report, "Android scripting backend");
            AssertValidationPassed(report, "Android architecture");
            AssertValidationPassed(report, "Android package id");
            AssertValidationPassed(report, "Active input handling");
            AssertValidationPassed(report, "Android multithreaded rendering");
            AssertValidationPassed(report, "Android graphics API");
            AssertValidationPassed(report, "Standalone OpenXR loader");
            AssertValidationPassed(report, "Android OpenXR loader");
            AssertValidationPassed(report, "Standalone Single Pass Instanced");
            AssertValidationPassed(report, "Standalone Oculus Touch profile");
            AssertValidationManual(report, "Headset setup");
        }

        [Test]
        public void AndroidBuildSupportValidationReportsActionableInstallState()
        {
            QuestXrProjectValidator.ValidationItem supported = QuestXrProjectValidator.CreateAndroidBuildSupportValidationItem(true);
            Assert.AreEqual("Android Build Support", supported.Label);
            Assert.IsTrue(supported.Passed);
            Assert.IsFalse(supported.WarningOnly);
            StringAssert.Contains("BuildTarget.Android is supported", supported.Detail);

            QuestXrProjectValidator.ValidationItem missing = QuestXrProjectValidator.CreateAndroidBuildSupportValidationItem(false);
            Assert.AreEqual("Android Build Support", missing.Label);
            Assert.IsFalse(missing.Passed);
            Assert.IsFalse(missing.WarningOnly);
            StringAssert.Contains("Android Build Support is not installed", missing.Detail);
            StringAssert.Contains("SDK and NDK Tools", missing.Detail);
            StringAssert.Contains("OpenJDK", missing.Detail);
        }

        [Test]
        public void QuestValidatorRejectsForbiddenMetaXrPackages()
        {
            QuestXrProjectValidator.ValidationItem clean = QuestXrProjectValidator.CreateForbiddenXrPackagesValidationItem("{\"dependencies\":{\"com.unity.xr.openxr\":\"1.15.1\"}}");
            Assert.AreEqual("Forbidden XR packages absent", clean.Label);
            Assert.IsTrue(clean.Passed);
            Assert.IsFalse(clean.WarningOnly);

            QuestXrProjectValidator.ValidationItem metaAllInOne = QuestXrProjectValidator.CreateForbiddenXrPackagesValidationItem("{\"dependencies\":{\"com.meta.xr.sdk.all\":\"72.0.0\"}}");
            Assert.IsFalse(metaAllInOne.Passed);
            Assert.IsFalse(metaAllInOne.WarningOnly);
            StringAssert.Contains("Meta XR SDK", metaAllInOne.Detail);

            QuestXrProjectValidator.ValidationItem unityMetaOpenXr = QuestXrProjectValidator.CreateForbiddenXrPackagesValidationItem("{\"dependencies\":{\"com.unity.xr.meta-openxr\":\"2.0.0\"}}");
            Assert.IsFalse(unityMetaOpenXr.Passed);
            StringAssert.Contains("com.unity.xr.meta-openxr", unityMetaOpenXr.Detail);
        }

        [Test]
        public void QuestRuntimeSmokeReportCoversGeneratedQuestObjects()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            var report = QuestRuntimeSmokeReport.Build(game);

            AssertSmokePassed(report, "Runtime mode");
            AssertSmokePassed(report, "Desktop command camera absent");
            AssertSmokePassed(report, "Desktop input absent");
            AssertSmokePassed(report, "Desktop HUD absent");
            AssertSmokePassed(report, "Screen-space overlay canvases absent");
            AssertSmokePassed(report, "Desktop event system absent");
            AssertSmokePassed(report, "Quest locomotion components absent");
            AssertSmokePassed(report, "Quest rig present");
            AssertSmokePassed(report, "Quest settings present");
            AssertSmokePassed(report, "Quest input present");
            AssertSmokePassed(report, "Quest world HUD present");
            AssertSmokePassed(report, "Quest world HUD control hints");
            AssertSmokePassed(report, "Quest world HUD non-interactive");
            AssertSmokePassed(report, "Quest tactical map present");
            AssertSmokePassed(report, "Quest tactical map non-interactive");
            AssertSmokePassed(report, "Quest command console present");
            AssertSmokePassed(report, "Quest world UI anchored to rig");
            AssertSmokePassed(report, "Quest command console panel ray");
            AssertSmokePassed(report, "View camera uses XR head");
            AssertSmokePassed(report, "Tabletop scale");
            AssertSmokePassed(report, "Board physical width");
            AssertSmokePassed(report, "Tabletop height offset");
            AssertSmokePassed(report, "Rig scale applied");
            AssertSmokePassed(report, "Fallback tabletop view");
            AssertSmokePassed(report, "Head tracking node");
            AssertSmokePassed(report, "Left controller node");
            AssertSmokePassed(report, "Right controller node");
            AssertSmokePassed(report, "Pointer visuals");
            AssertSmokeManual(report, "Physical headset verification");
        }

        [Test]
        public void QuestSceneBudgetReportCoversGeneratedRuntimeFootprint()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            RtsSceneBudgetSnapshot snapshot = RtsSceneBudgetSnapshot.Capture(game);
            var report = RtsSceneBudgetReport.BuildQuestBudget(game);

            Assert.AreEqual("QuestVr", snapshot.runtimeMode);
            Assert.AreEqual(14, snapshot.entityCount);
            Assert.AreEqual(61, snapshot.resourceNodeCount);
            Assert.Greater(snapshot.totalGameObjects, 0);
            Assert.Greater(snapshot.rendererCount, 0);
            Assert.Greater(snapshot.uniqueSharedMaterialCount, 0);
            Assert.AreEqual(1, snapshot.enabledCameraCount);
            Assert.AreEqual(1, snapshot.enabledLightCount);
            Assert.GreaterOrEqual(snapshot.worldSpaceCanvasCount, 3);
            Assert.AreEqual(0, snapshot.screenSpaceOverlayCanvasCount);
            Assert.AreEqual(1, snapshot.tacticalMapCanvasCount);
            Assert.AreEqual(1, snapshot.fogOverlayObjectCount);
            Assert.AreEqual(1, snapshot.fogOverlayRendererCount);
            Assert.AreEqual(0, snapshot.fogCellObjectCount);
            Assert.Greater(snapshot.visualSetDressingObjectCount, 0);
            Assert.AreEqual(0, snapshot.visualSetDressingColliderCount);

            AssertBudgetPassed(report, "Runtime mode");
            AssertBudgetPassed(report, "GameObject budget");
            AssertBudgetPassed(report, "Renderer budget");
            AssertBudgetPassed(report, "Shared material budget");
            AssertBudgetPassed(report, "Collider budget");
            AssertBudgetPassed(report, "Light budget");
            AssertBudgetPassed(report, "Camera budget");
            AssertBudgetPassed(report, "World-space Quest UI");
            AssertBudgetPassed(report, "Quest tactical map UI");
            AssertBudgetPassed(report, "Fog overlay budget");
            AssertBudgetPassed(report, "Visual set dressing colliders");
        }

        [Test]
        public void QuestSceneBudgetValidatorBuildsGeneratedQuestReport()
        {
            var report = QuestSceneBudgetValidator.BuildGeneratedQuestSceneBudgetReport();

            AssertBudgetPassed(report, "Runtime mode");
            AssertBudgetPassed(report, "World-space Quest UI");
            AssertBudgetPassed(report, "Quest tactical map UI");
            AssertBudgetPassed(report, "Fog overlay budget");
            AssertBudgetPassed(report, "Visual set dressing colliders");
        }

        [Test]
        public void GeneratedRuntimeValidatorsCleanUpTemporaryObjects()
        {
            DesktopRuntimeSmokeValidator.BuildGeneratedDesktopRuntimeReport();
            AssertNoGeneratedObjects("Desktop Runtime Smoke Test", "Command Camera", "Sun", "EventSystem");

            QuestRuntimeSmokeValidator.BuildGeneratedQuestRuntimeReport();
            AssertNoGeneratedObjects("Quest Runtime Smoke Test", "Sun");

            QuestSceneBudgetValidator.BuildGeneratedQuestSceneBudgetReport();
            AssertNoGeneratedObjects("Quest Scene Budget Runtime", "Sun");
        }

        [Test]
        public void DesktopRuntimeSmokeReportCoversGeneratedDesktopObjects()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            var report = DesktopRuntimeSmokeReport.Build(game);

            AssertDesktopSmokePassed(report, "Runtime mode");
            AssertDesktopSmokePassed(report, "Command camera present");
            AssertDesktopSmokePassed(report, "View camera uses command camera");
            AssertDesktopSmokePassed(report, "Desktop input present");
            AssertDesktopSmokePassed(report, "Desktop HUD present");
            AssertDesktopSmokePassed(report, "Desktop event system present");
            AssertDesktopSmokePassed(report, "Quest rig absent");
            AssertDesktopSmokePassed(report, "Quest world HUD absent");
            AssertDesktopSmokePassed(report, "Quest tactical map absent");
            AssertDesktopSmokePassed(report, "Quest command console absent");
            AssertDesktopSmokePassed(report, "Command dispatcher present");
            AssertDesktopSmokePassed(report, "Build manager present");
            AssertDesktopSmokePassed(report, "Initial entities spawned");
            AssertDesktopSmokePassed(report, "Initial selection present");
            AssertDesktopSmokeManual(report, "Desktop control regression");
        }

        [Test]
        public void RuntimeDiagnosticsSnapshotCountsGeneratedDesktopMatch()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsRuntimeDiagnosticsSnapshot snapshot = RtsRuntimeDiagnosticsSnapshot.Capture(game);

            Assert.AreEqual("Desktop", snapshot.runtimeMode);
            Assert.AreEqual("Running", snapshot.matchState);
            Assert.IsTrue(snapshot.acceptsPlayerInput);
            Assert.AreEqual(14, snapshot.entityCount);
            Assert.AreEqual(14, snapshot.aliveEntityCount);
            Assert.AreEqual(7, snapshot.playerEntityCount);
            Assert.AreEqual(7, snapshot.enemyEntityCount);
            Assert.AreEqual(2, snapshot.selectedEntityCount);
            Assert.AreEqual(6, snapshot.unitCount);
            Assert.AreEqual(8, snapshot.structureCount);
            Assert.AreEqual(61, snapshot.resources.nodeCount);
            Assert.Greater(snapshot.resources.remainingAmount, 0);
            Assert.AreEqual(4, snapshot.production.producerCount);
            Assert.AreEqual(0, snapshot.production.totalQueueItems);
            Assert.IsFalse(snapshot.buildPlacement.active);
            Assert.AreEqual(RtsBalance.MapHalfSize * 2f, snapshot.tabletop.simulationWidth, 0.001f);
            Assert.AreEqual(56 * 56, snapshot.fog.totalCells);
            Assert.Greater(snapshot.fog.exploredCells, 0);
            Assert.AreEqual(3, snapshot.unitKinds.Count);
            Assert.AreEqual(5, snapshot.structureKinds.Count);
            Assert.AreEqual(7, FindDiagnosticsTeam(snapshot, "Player").aliveEntities);
            Assert.AreEqual(7, FindDiagnosticsTeam(snapshot, "Enemy").aliveEntities);
        }

        [Test]
        public void RuntimeDiagnosticsSnapshotCapturesQuestTabletopScale()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            RtsRuntimeDiagnosticsSnapshot snapshot = RtsRuntimeDiagnosticsSnapshot.Capture(game);

            Assert.AreEqual("QuestVr", snapshot.runtimeMode);
            Assert.AreEqual(126f, snapshot.tabletop.simulationUnitsPerMeter, 0.001f);
            Assert.AreEqual(1.78f, snapshot.tabletop.battlefieldWidthMeters, 0.01f);
            Assert.AreEqual(0.82f, snapshot.tabletop.boardHeightMeters, 0.001f);
            Assert.IsTrue(snapshot.acceptsSystemInput);
            Assert.IsTrue(snapshot.acceptsPlayerInput);
        }

        [Test]
        public void SoakScenarioExporterCreatesPopulatedDiagnosticsBaseline()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsSoakScenarioExporter.PopulateGeneratedMatchForSoak(game);
            RtsRuntimeDiagnosticsSnapshot snapshot = RtsRuntimeDiagnosticsSnapshot.Capture(game);

            Assert.GreaterOrEqual(snapshot.entityCount, 69);
            Assert.GreaterOrEqual(snapshot.playerEntityCount, 32);
            Assert.GreaterOrEqual(snapshot.enemyEntityCount, 37);
            Assert.GreaterOrEqual(snapshot.selectedEntityCount, 22);
            Assert.GreaterOrEqual(snapshot.production.producerCount, 5);
            Assert.GreaterOrEqual(snapshot.production.activeProducerCount, 4);
            Assert.GreaterOrEqual(snapshot.production.totalQueueItems, 6);
            Assert.IsTrue(snapshot.buildPlacement.active);
            Assert.IsTrue(snapshot.buildPlacement.hasPoint);
            Assert.IsTrue(snapshot.buildPlacement.valid);
            int infantryTotal = FindDiagnosticsUnitKind(snapshot, "Rifleman").total +
                FindDiagnosticsUnitKind(snapshot, "Grenadier").total +
                FindDiagnosticsUnitKind(snapshot, "RocketSoldier").total +
                FindDiagnosticsUnitKind(snapshot, "FlameTrooper").total +
                FindDiagnosticsUnitKind(snapshot, "Engineer").total;
            Assert.GreaterOrEqual(infantryTotal, 34);
            Assert.GreaterOrEqual(FindDiagnosticsUnitKind(snapshot, "Harvester").player, 5);
            int armorTotal = FindDiagnosticsUnitKind(snapshot, "LightTank").total +
                FindDiagnosticsUnitKind(snapshot, "MediumTank").total +
                FindDiagnosticsUnitKind(snapshot, "HeavyTank").total;
            Assert.GreaterOrEqual(armorTotal, 21);
            Assert.GreaterOrEqual(FindDiagnosticsStructureKind(snapshot, "WarFactory").player, 1);
            Assert.Less(FindDiagnosticsTeam(snapshot, "Player").idleUnits, 8);
        }

        [Test]
        public void DesktopBuildArtifactValidationRequiresExistingNonEmptyFile()
        {
            string directory = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-BuildValidation");
            Directory.CreateDirectory(directory);
            string missingPath = Path.Combine(directory, "missing.exe");
            string emptyPath = Path.Combine(directory, "empty.exe");
            string validPath = Path.Combine(directory, "valid.exe");

            try
            {
                File.WriteAllBytes(emptyPath, new byte[0]);
                File.WriteAllBytes(validPath, new byte[] { 1, 2, 3 });

                Assert.IsFalse(RtsBuildAutomation.IsValidBuildArtifact(null));
                Assert.IsFalse(RtsBuildAutomation.IsValidBuildArtifact(missingPath));
                Assert.IsFalse(RtsBuildAutomation.IsValidBuildArtifact(emptyPath));
                Assert.IsTrue(RtsBuildAutomation.IsValidBuildArtifact(validPath));
            }
            finally
            {
                if (File.Exists(emptyPath))
                {
                    File.Delete(emptyPath);
                }

                if (File.Exists(validPath))
                {
                    File.Delete(validPath);
                }
            }
        }

        [Test]
        public void DesktopBuildUnsupportedMessageNamesRequiredEditorModule()
        {
            string message = RtsBuildAutomation.GetUnsupportedDesktopBuildTargetMessage();
            StringAssert.Contains("StandaloneWindows64", message);
            StringAssert.Contains("Windows Build Support", message);
            StringAssert.Contains("Unity 2022.3.62f3", message);
        }

        [Test]
        public void DesktopBuildTemplateValidationRequiresWindowsPlayerExecutable()
        {
            string directory = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-EditorTemplateValidation");
            string editorPath = Path.Combine(directory, "Editor", "Unity.exe");
            string templatePath = Path.Combine(directory, "Editor", "Data", "PlaybackEngines", "windowsstandalonesupport", "Variations", "win64_player_development_mono", "WindowsPlayer.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(editorPath));
            Directory.CreateDirectory(Path.GetDirectoryName(templatePath));

            try
            {
                File.WriteAllBytes(editorPath, new byte[] { 1, 2, 3 });

                Assert.IsFalse(RtsBuildAutomation.HasWindowsStandalonePlayerTemplate(editorPath));

                File.WriteAllBytes(templatePath, new byte[] { 4, 5, 6 });
                Assert.IsTrue(RtsBuildAutomation.HasWindowsStandalonePlayerTemplate(editorPath));

                string message = RtsBuildAutomation.GetMissingDesktopPlayerTemplateMessage(editorPath);
                StringAssert.Contains("WindowsPlayer.exe", message);
                StringAssert.Contains("Windows Build Support", message);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void DesktopBuildSupportValidationReportsEnvironmentFailures()
        {
            string directory = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-BuildSupportValidation");
            string editorPath = Path.Combine(directory, "Editor", "Unity.exe");
            string templatePath = Path.Combine(directory, "Editor", "Data", "PlaybackEngines", "windowsstandalonesupport", "Variations", "win64_player_development_mono", "WindowsPlayer.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(editorPath));
            Directory.CreateDirectory(Path.GetDirectoryName(templatePath));

            try
            {
                File.WriteAllBytes(editorPath, new byte[] { 1, 2, 3 });

                Assert.IsFalse(RtsBuildAutomation.TryValidateDesktopBuildSupport(false, editorPath, out string unsupportedError));
                StringAssert.Contains("StandaloneWindows64", unsupportedError);

                Assert.IsFalse(RtsBuildAutomation.TryValidateDesktopBuildSupport(true, editorPath, out string missingTemplateError));
                StringAssert.Contains("WindowsPlayer.exe", missingTemplateError);

                File.WriteAllBytes(templatePath, new byte[] { 4, 5, 6 });
                Assert.IsTrue(RtsBuildAutomation.TryValidateDesktopBuildSupport(true, editorPath, out string validationError));
                Assert.AreEqual(string.Empty, validationError);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void DispatcherSelectsClearsAndAddsFromWorldRays()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            game.ClearSelection();

            RtsEntity first = FindPlayerEntity(game, typeof(RtsStructure));
            RtsEntity second = FindPlayerEntity(game, typeof(RtsUnit));

            game.CommandDispatcher.SelectFromRay(RayAt(first), false, 500f);
            Assert.AreEqual(1, game.Selection.Count);
            Assert.AreSame(first, game.Selection[0]);

            game.CommandDispatcher.SelectFromRay(RayAt(second), true, 500f);
            Assert.AreEqual(2, game.Selection.Count);

            Assert.AreEqual(RtsCommandResult.None, game.CommandDispatcher.SelectFromRay(RayAtPoint(new Vector3(0f, 0f, 0f)), true, 500f));
            Assert.AreEqual(2, game.Selection.Count);

            Assert.AreEqual(RtsCommandResult.SelectionCleared, game.CommandDispatcher.SelectFromRay(new Ray(new Vector3(220f, 20f, 220f), Vector3.down), false, 500f));
            Assert.AreEqual(0, game.Selection.Count);
        }

        [Test]
        public void DispatcherGathersOnlyLivingPlayerUnitsForCommands()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            game.ClearSelection();

            RtsUnit liveUnit = FindPlayerUnit(game, UnitKind.Rifleman);
            RtsUnit deadUnit = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, liveUnit.transform.position + new Vector3(3f, 0f, 0f));
            deadUnit.SetHealthForRestore(0f);

            game.SelectEntity(liveUnit, false);
            game.SelectEntity(deadUnit, true);

            System.Collections.Generic.List<RtsUnit> gathered = new System.Collections.Generic.List<RtsUnit>();
            game.CommandDispatcher.GatherSelectedControllableUnits(gathered);

            Assert.AreEqual(1, gathered.Count);
            Assert.AreSame(liveUnit, gathered[0]);
        }

        [Test]
        public void DispatcherResolvesContextCommandPriority()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            game.ClearSelection();

            RtsUnit playerUnit = (RtsUnit)FindPlayerEntity(game, typeof(RtsUnit));
            game.SelectEntity(playerUnit, false);

            RtsEntity visibleEnemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, playerUnit.transform.position + new Vector3(4f, 0f, 0f));
            Physics.SyncTransforms();
            Assert.AreEqual(RtsContextCommandKind.Attack, game.CommandDispatcher.ResolveContextCommand(visibleEnemy, null, visibleEnemy.transform.position));

            ResourceNode resource = game.ResourceNodes[0];
            Assert.AreEqual(RtsContextCommandKind.Attack, game.CommandDispatcher.ResolveContextCommand(visibleEnemy, resource, resource.transform.position));
            Assert.AreEqual(RtsContextCommandKind.Harvest, game.CommandDispatcher.ResolveContextCommand(null, resource, resource.transform.position));

            ProductionStructure producer = (ProductionStructure)FindPlayerProduction(game);
            game.ClearSelection();
            game.SelectEntity(producer, false);
            Assert.AreEqual(RtsContextCommandKind.Harvest, game.CommandDispatcher.ResolveContextCommand(null, resource, producer.transform.position + new Vector3(8f, 0f, 0f)));
            Assert.AreEqual(RtsContextCommandKind.Rally, game.CommandDispatcher.ResolveContextCommand(null, null, producer.transform.position + new Vector3(8f, 0f, 0f)));

            game.ClearSelection();
            game.SelectEntity(playerUnit, false);
            Assert.AreEqual(RtsContextCommandKind.Move, game.CommandDispatcher.ResolveContextCommand(null, null, playerUnit.transform.position + new Vector3(8f, 0f, 6f)));
        }

        [Test]
        public void QuestControllerMapsTriggerSelectionAndAdditiveModifier()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            Assert.IsNotNull(controller);

            game.ClearSelection();
            RtsEntity first = FindPlayerEntity(game, typeof(RtsStructure));
            RtsEntity second = FindPlayerEntity(game, typeof(RtsUnit));

            Assert.AreEqual(RtsCommandResult.SelectionChanged, controller.ProcessInputFrameForTests(QuestFrame(RayAt(first), false, true, false, false, false), false));
            Assert.AreEqual(1, game.Selection.Count);
            Assert.AreSame(first, game.Selection[0]);

            Assert.AreEqual(RtsCommandResult.None, controller.ProcessInputFrameForTests(QuestFrame(RayAt(second), false, true, false, false, false), false));
            Assert.AreEqual(1, game.Selection.Count);
            Assert.AreSame(first, game.Selection[0]);

            controller.ProcessInputFrameForTests(QuestFrame(RayAt(second), false, false, false, false, false), false);
            Assert.AreEqual(RtsCommandResult.SelectionChanged, controller.ProcessInputFrameForTests(QuestFrame(RayAt(second), true, true, false, false, false), false));
            Assert.AreEqual(2, game.Selection.Count);
            Assert.AreSame(first, game.Selection[0]);
            Assert.AreSame(second, game.Selection[1]);
        }

        [Test]
        public void QuestControllerAreaSelectsNearbyUnitsWithLeftTriggerModifier()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            Assert.IsNotNull(controller);

            RtsUnit first = FindPlayerUnit(game, UnitKind.Rifleman);
            RtsUnit second = FindSecondPlayerUnit(game, UnitKind.Rifleman, first);
            Vector3 center = (first.GroundPosition + second.GroundPosition) * 0.5f;

            game.ClearSelection();
            Assert.AreEqual(RtsCommandResult.SelectionChanged, controller.ProcessInputFrameForTests(QuestFrame(RayAtPoint(center), true, true, false, false, false), false));

            Assert.AreEqual(2, game.Selection.Count);
            Assert.IsTrue(first.IsSelected);
            Assert.IsTrue(second.IsSelected);
            for (int i = 0; i < game.Selection.Count; i++)
            {
                Assert.IsInstanceOf<RtsUnit>(game.Selection[i], "Quest area selection should gather nearby units without selecting structures.");
            }
        }

        [Test]
        public void QuestControllerMapsCommandCancelAndHeldButtonTransitions()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            Assert.IsNotNull(controller);

            game.ClearSelection();
            RtsUnit unit = (RtsUnit)FindPlayerEntity(game, typeof(RtsUnit));
            game.SelectEntity(unit, false);

            Ray terrainRay = RayAtPoint(new Vector3(-24f, 0f, -34f));
            Assert.AreEqual(RtsCommandResult.MoveIssued, controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, true, false, false), false));
            Assert.AreEqual("Move", unit.CaptureOrderState().orderType);

            Assert.AreEqual(RtsCommandResult.None, controller.ProcessInputFrameForTests(QuestFrame(RayAtPoint(new Vector3(-18f, 0f, -28f)), false, false, true, false, false), false));
            Assert.AreEqual("Move", unit.CaptureOrderState().orderType);

            controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, false, false), false);
            Assert.AreEqual(RtsCommandResult.AttackMoveIssued, controller.ProcessInputFrameForTests(QuestFrame(terrainRay, true, false, true, false, false), false));
            Assert.AreEqual("AttackMove", unit.CaptureOrderState().orderType);

            controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, false, false), false);
            Assert.AreEqual(RtsCommandResult.SelectionCleared, controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, true, false), false));
            Assert.AreEqual(0, game.Selection.Count);

            controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, false, false), false);
            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.PowerPlant));
            Assert.AreEqual(RtsCommandResult.PlacementCanceled, controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, true, false), false));
            Assert.IsFalse(game.BuildManager.IsPlacing);
        }

        [Test]
        public void QuestControllerConsoleToggleUsesLeftPrimaryButtonDownOnly()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(console);
            Assert.IsFalse(console.IsOpen);

            Ray terrainRay = RayAtPoint(new Vector3(-24f, 0f, -34f));

            controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, false, true), false);
            Assert.IsTrue(console.IsOpen);

            controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, false, true), false);
            Assert.IsTrue(console.IsOpen, "Holding X/left primary should not toggle every frame.");

            ReleaseButtons(controller, terrainRay);
            controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, false, true), false);
            Assert.IsFalse(console.IsOpen);
        }

        [Test]
        public void QuestControllerDoesNotChangeActivePlacementWhilePaused()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            Assert.IsNotNull(controller);

            Vector3 placementPoint = FindValidBuildPoint(game, StructureKind.PowerPlant);
            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.PowerPlant));
            game.BuildManager.UpdatePlacementAtPoint(placementPoint);
            Assert.IsTrue(game.BuildManager.IsPlacing);
            Assert.IsTrue(game.BuildManager.PlacementValid);

            game.SetUserPaused(true);
            Ray outsideMapRay = RayAtPoint(new Vector3(RtsBalance.MapHalfSize + 40f, 0f, RtsBalance.MapHalfSize + 40f));

            Assert.AreEqual(RtsCommandResult.None, controller.ProcessInputFrameForTests(QuestFrame(outsideMapRay, false, false, true, true, false), false));
            Assert.IsTrue(game.BuildManager.IsPlacing);
            Assert.IsTrue(game.BuildManager.PlacementValid);
            AssertVectorNear(placementPoint, game.BuildManager.PlacementPoint);
        }

        [Test]
        public void QuestControllerPrimaryButtonIssuesContextCommands()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            Assert.IsNotNull(controller);

            RtsUnit attacker = FindPlayerUnit(game, UnitKind.Rifleman);
            RtsEntity enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, attacker.transform.position + new Vector3(7f, 0f, 0f));
            Physics.SyncTransforms();

            game.ClearSelection();
            game.SelectEntity(attacker, false);
            Ray enemyRay = RayAt(enemy);
            Assert.AreEqual(RtsCommandResult.AttackIssued, controller.ProcessInputFrameForTests(QuestFrame(enemyRay, false, false, true, false, false), false));
            RtsUnitOrderSaveData attackOrder = attacker.CaptureOrderState();
            Assert.AreEqual("Attack", attackOrder.orderType);
            Assert.AreEqual(enemy.PersistentId, attackOrder.targetEntityId);
            ReleaseButtons(controller, enemyRay);

            HarvesterUnit harvester = (HarvesterUnit)FindPlayerUnit(game, UnitKind.Harvester);
            ResourceNode resource = game.ResourceNodes[0];
            game.ClearSelection();
            game.SelectEntity(harvester, false);
            Ray resourceRay = RayAtResource(resource);
            Assert.AreEqual(RtsCommandResult.HarvestIssued, controller.ProcessInputFrameForTests(QuestFrame(resourceRay, false, false, true, false, false), false));
            RtsHarvesterSaveData harvesterState = harvester.CaptureHarvesterState();
            Assert.AreEqual(1, harvesterState.state);
            Assert.AreEqual(resource.PersistentId, harvesterState.targetResourceNodeId);
            Assert.Greater(harvesterState.homeRefineryEntityId, 0);
            ReleaseButtons(controller, resourceRay);

            ProductionStructure producer = FindPlayerProduction(game);
            Vector3 rallyPoint = new Vector3(-42f, 0f, -42f);
            game.ClearSelection();
            game.SelectEntity(producer, false);
            Ray rallyRay = RayAtPoint(rallyPoint);
            Assert.AreEqual(RtsCommandResult.RallyPointSet, controller.ProcessInputFrameForTests(QuestFrame(rallyRay, false, false, true, false, false), false));
            Assert.IsTrue(producer.HasRallyPoint);
            AssertVectorNear(rallyPoint, producer.RallyPoint);
            ReleaseButtons(controller, rallyRay);

            Vector3 movePoint = new Vector3(-24f, 0f, -34f);
            game.ClearSelection();
            game.SelectEntity(attacker, false);
            Ray moveRay = RayAtPoint(movePoint);
            Assert.AreEqual(RtsCommandResult.MoveIssued, controller.ProcessInputFrameForTests(QuestFrame(moveRay, false, false, true, false, false), false));
            Assert.AreEqual("Move", attacker.CaptureOrderState().orderType);
        }

        [Test]
        public void QuestPointerFeedbackUpdatesLineReticleAndMissState()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(settings);
            Assert.IsNotNull(controller.PointerLineForTests);
            Assert.IsNotNull(controller.ReticleForTests);
            Assert.AreEqual(settings.RayWidthSimulationUnits, controller.PointerLineForTests.widthMultiplier, 0.001f);

            Vector3 hitPoint = new Vector3(-24f, 0f, -34f);
            Ray hitRay = RayAtPoint(hitPoint);
            controller.ProcessInputFrameForTests(QuestFrame(hitRay, false, false, false, false, false), true);

            Assert.IsTrue(controller.PointerLineForTests.enabled);
            AssertVectorNear(hitRay.origin, controller.PointerLineForTests.GetPosition(0));
            AssertVectorNear(hitPoint, controller.PointerLineForTests.GetPosition(1));
            Assert.IsTrue(controller.ReticleForTests.gameObject.activeSelf);
            AssertVectorNear(hitPoint, controller.ReticleForTests.position);
            AssertVectorNear(Vector3.one * settings.ReticleSizeMeters, controller.ReticleForTests.localScale);
            AssertVectorNear(Vector3.one * settings.ReticleSizeSimulationUnits, controller.ReticleForTests.lossyScale);

            Ray missRay = new Ray(new Vector3(180f, 12f, 180f), Vector3.up);
            controller.ProcessInputFrameForTests(QuestFrame(missRay, false, false, false, false, false), true);

            Assert.IsTrue(controller.PointerLineForTests.enabled);
            AssertVectorNear(missRay.origin, controller.PointerLineForTests.GetPosition(0));
            AssertVectorNear(missRay.GetPoint(settings.RayLengthSimulationUnits), controller.PointerLineForTests.GetPosition(1));
            Assert.IsFalse(controller.ReticleForTests.gameObject.activeSelf);
        }

        [Test]
        public void QuestPointerFeedbackUsesContextColorsForTargetsAndConsole()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(console);

            RtsUnit playerUnit = FindPlayerUnit(game, UnitKind.Rifleman);
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, playerUnit.transform.position + new Vector3(4f, 0f, 0f));
            Physics.SyncTransforms();
            game.FogOfWar.RefreshNowForTests();

            controller.ProcessInputFrameForTests(QuestFrame(RayAt(enemy), false, false, false, false, false), true);
            AssertColorNear(new Color(1f, 0.32f, 0.22f, 0.95f), controller.PointerLineForTests.startColor);

            ResourceNode resource = game.ResourceNodes[0];
            controller.ProcessInputFrameForTests(QuestFrame(RayAtResource(resource), false, false, false, false, false), true);
            AssertColorNear(new Color(0.25f, 1f, 0.48f, 0.95f), controller.PointerLineForTests.startColor);

            ProductionStructure producer = FindPlayerProduction(game);
            game.ClearSelection();
            game.SelectEntity(producer, false);
            controller.ProcessInputFrameForTests(QuestFrame(RayAtPoint(new Vector3(-42f, 0f, -42f)), false, false, false, false, false), true);
            AssertColorNear(new Color(0.55f, 0.95f, 1f, 0.95f), controller.PointerLineForTests.startColor);

            game.ClearSelection();
            controller.ProcessInputFrameForTests(QuestFrame(RayAtPoint(new Vector3(-24f, 0f, -34f)), false, false, false, false, false), true);
            AssertColorNear(new Color(0.3f, 0.88f, 1f, 0.95f), controller.PointerLineForTests.startColor);

            console.SetOpen(true);
            Ray panelRay = new Ray(console.PanelRect.position - console.PanelRect.forward * 8f, console.PanelRect.forward);
            controller.ProcessInputFrameForTests(QuestFrame(panelRay, false, false, false, false, false), true);
            AssertColorNear(new Color(0.72f, 0.92f, 1f, 0.95f), controller.PointerLineForTests.startColor);

            Ray missRay = new Ray(new Vector3(180f, 12f, 180f), Vector3.up);
            controller.ProcessInputFrameForTests(QuestFrame(missRay, false, false, false, false, false), true);
            AssertColorNear(new Color(0.55f, 0.6f, 0.62f, 0.65f), controller.PointerLineForTests.startColor);
        }

        [Test]
        public void QuestPointerFeedbackHidesWhenSystemInputIsBlocked()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            Assert.IsNotNull(controller);

            Ray terrainRay = RayAtPoint(new Vector3(-24f, 0f, -34f));
            controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, false, false, false, false), true);
            Assert.IsTrue(controller.PointerLineForTests.enabled);
            Assert.IsTrue(controller.ReticleForTests.gameObject.activeSelf);

            game.Lifecycle.SetInputFocusForTests(false);

            Assert.AreEqual(RtsCommandResult.None, controller.ProcessInputFrameForTests(QuestFrame(terrainRay, false, true, true, true, false), true));
            Assert.IsFalse(controller.PointerLineForTests.enabled);
            Assert.IsFalse(controller.ReticleForTests.gameObject.activeSelf);
        }

        [Test]
        public void QuestPerFrameCodeAvoidsObviousAllocatingPatterns()
        {
            string[] hotLoops =
            {
                "Assets/Scripts/Runtime/QuestRtsInputController.cs|private void Update(",
                "Assets/Scripts/Runtime/QuestRtsInputController.cs|private RtsCommandResult ProcessInputFrame(",
                "Assets/Scripts/Runtime/QuestRtsInputController.cs|private void UpdatePointer(Ray ray, bool hasHit, RaycastHit hit)",
                "Assets/Scripts/Runtime/QuestRtsInputController.cs|private void UpdatePointer(Ray ray, bool hasHit, Vector3 hitPoint, Color color)",
                "Assets/Scripts/Runtime/QuestTrackedNodePose.cs|private void Update(",
                "Assets/Scripts/Runtime/QuestWorldHud.cs|private void Update(",
                "Assets/Scripts/Runtime/QuestTacticalMap.cs|private void Update(",
                "Assets/Scripts/Runtime/QuestTacticalMap.cs|private void Refresh(",
                "Assets/Scripts/Runtime/QuestTacticalMap.cs|private void RefreshResourcePips(",
                "Assets/Scripts/Runtime/QuestTacticalMap.cs|private void RefreshEntityPips(",
                "Assets/Scripts/Runtime/QuestCommandConsole.cs|private void Update("
            };

            string[] forbiddenPatterns =
            {
                "new GameObject",
                "GameObject.CreatePrimitive",
                "new Material",
                "FindObjectOfType",
                "FindObjectsOfType",
                ".Select(",
                ".Where(",
                ".ToList("
            };

            for (int i = 0; i < hotLoops.Length; i++)
            {
                string[] parts = hotLoops[i].Split('|');
                string body = ExtractMethodBody(parts[0], parts[1]);
                for (int j = 0; j < forbiddenPatterns.Length; j++)
                {
                    Assert.IsFalse(body.Contains(forbiddenPatterns[j]), hotLoops[i] + " should not contain " + forbiddenPatterns[j]);
                }
            }
        }

        [Test]
        public void CommandDispatcherSourceStaysDeviceInputIndependent()
        {
            string source = File.ReadAllText("Assets/Scripts/Runtime/RtsCommandDispatcher.cs");
            string[] forbiddenPatterns =
            {
                "Input.",
                "InputSystem",
                "InputDevice",
                "UnityEngine.XR",
                "XRNode",
                "CommonUsages",
                "KeyCode",
                "mousePosition",
                "GetMouseButton",
                "GetKey",
                "Screen.",
                "ScreenPointToRay",
                "Camera.main",
                "OnGUI",
                "GUI."
            };

            for (int i = 0; i < forbiddenPatterns.Length; i++)
            {
                Assert.IsFalse(source.Contains(forbiddenPatterns[i]), "RtsCommandDispatcher should stay device-independent and not contain " + forbiddenPatterns[i]);
            }
        }

        [Test]
        public void DesktopInputControllerDoesNotDuplicateContextCommandRules()
        {
            string source = File.ReadAllText("Assets/Scripts/Runtime/RtsInputController.cs");
            string[] forbiddenPatterns =
            {
                "Physics.Raycast",
                "GetComponentInParent<RtsEntity>",
                "GetComponentInParent<ResourceNode>",
                ".IssueMove(",
                ".IssueAttack(",
                ".IssueHarvest(",
                ".IssueAttackMove(",
                "FindNearestPlayerRefinery",
                "SetSelectedRallyPoint",
                "ResolveContextCommand"
            };

            for (int i = 0; i < forbiddenPatterns.Length; i++)
            {
                Assert.IsFalse(source.Contains(forbiddenPatterns[i]), "RtsInputController should delegate RTS target resolution to RtsCommandDispatcher and not contain " + forbiddenPatterns[i]);
            }

            Assert.IsTrue(source.Contains("dispatcher.SelectFromRay"), "Desktop selection clicks should delegate to RtsCommandDispatcher.");
            Assert.IsTrue(source.Contains("dispatcher.CommandFromRay"), "Desktop context commands should delegate to RtsCommandDispatcher.");
            Assert.IsTrue(source.Contains("dispatcher.AttackMoveFromRay"), "Desktop attack-move should delegate to RtsCommandDispatcher.");
            Assert.IsTrue(source.Contains("dispatcher.UpdatePlacement"), "Desktop placement updates should delegate to RtsCommandDispatcher.");
            Assert.IsTrue(source.Contains("dispatcher.ConfirmPlacement"), "Desktop placement confirmation should delegate to RtsCommandDispatcher.");
            Assert.IsTrue(source.Contains("dispatcher.CancelPlacement"), "Desktop placement cancellation should delegate to RtsCommandDispatcher.");
        }

        [Test]
        public void QuestInputControllerDoesNotImplementArtificialLocomotionOrBoardManipulation()
        {
            string source = File.ReadAllText("Assets/Scripts/Runtime/QuestRtsInputController.cs");
            string updateBody = ExtractMethodBody("Assets/Scripts/Runtime/QuestRtsInputController.cs", "private void Update(");
            string processBody = ExtractMethodBody("Assets/Scripts/Runtime/QuestRtsInputController.cs", "private RtsCommandResult ProcessInputFrame(");
            string[] forbiddenSourcePatterns =
            {
                "primary2DAxis",
                "secondary2DAxis",
                "CommonUsages.gripButton",
                "CommonUsages.grip",
                "thumbstick",
                "joystick",
                "Teleport",
                "Locomotion",
                "SnapTurn",
                "ContinuousTurn",
                "RigRoot",
                "HeadCamera",
                "QuestTabletopRig"
            };

            for (int i = 0; i < forbiddenSourcePatterns.Length; i++)
            {
                Assert.IsFalse(source.Contains(forbiddenSourcePatterns[i]), "QuestRtsInputController should stay command-only and not contain " + forbiddenSourcePatterns[i]);
            }

            string[] forbiddenPoseWrites =
            {
                ".position =",
                ".rotation =",
                ".localPosition =",
                ".localRotation =",
                ".Translate(",
                ".Rotate(",
                "SetParent("
            };

            for (int i = 0; i < forbiddenPoseWrites.Length; i++)
            {
                Assert.IsFalse(updateBody.Contains(forbiddenPoseWrites[i]), "QuestRtsInputController.Update should not move the rig, board, camera, or controller transforms with " + forbiddenPoseWrites[i]);
                Assert.IsFalse(processBody.Contains(forbiddenPoseWrites[i]), "QuestRtsInputController.ProcessInputFrame should only issue commands and not move transforms with " + forbiddenPoseWrites[i]);
            }
        }

        [Test]
        public void QuestTabletopRigDoesNotContinuouslyOverwriteTrackedHeadOrHands()
        {
            string source = File.ReadAllText("Assets/Scripts/Runtime/QuestTabletopRig.cs");
            string initializeBody = ExtractMethodBody("Assets/Scripts/Runtime/QuestTabletopRig.cs", "public void Initialize(");

            Assert.IsFalse(source.Contains("void Update("), "QuestTabletopRig should not run a per-frame pose override loop.");
            Assert.IsFalse(source.Contains("void LateUpdate("), "QuestTabletopRig should not late-override tracked poses.");
            Assert.IsFalse(source.Contains("HeadCamera.transform.position ="), "Head camera position should come from the tracked head node.");
            Assert.IsFalse(source.Contains("HeadCamera.transform.rotation ="), "Head camera rotation should come from the tracked head node.");
            Assert.IsFalse(source.Contains("Head.position ="), "Head world position should not be overwritten after the tracked node is created.");
            Assert.IsFalse(source.Contains("Head.rotation ="), "Head world rotation should not be overwritten after the tracked node is created.");
            Assert.IsFalse(source.Contains("LeftController.position ="), "Left controller world position should not be overwritten after the tracked node is created.");
            Assert.IsFalse(source.Contains("RightController.position ="), "Right controller world position should not be overwritten after the tracked node is created.");

            Assert.IsTrue(initializeBody.Contains("RigRoot.position = settings.GetRigRootPosition();"), "Rig root tabletop height should be applied during initialization.");
            Assert.IsTrue(initializeBody.Contains("RigRoot.rotation = Quaternion.Euler(0f, settings.InitialYawDegrees, 0f);"), "Rig root initial yaw should be applied during initialization.");
            Assert.IsTrue(initializeBody.Contains("RigRoot.localScale = Vector3.one * settings.SimulationUnitsPerMeter;"), "Rig root scale should be applied during initialization.");
        }

        [Test]
        public void ConsoleModelBuildAvailabilityReflectsCreditsTechnologyAndPower()
        {
            RtsGame lowCreditGame = CreateInitializedGame(RtsRuntimeMode.Desktop);
            lowCreditGame.Resources.TrySpend(3300);
            RtsCommandConsoleModel lowCreditModel = CreateModel(lowCreditGame);
            RtsBuildOptionView powerPlant = GetBuildOption(lowCreditModel, StructureKind.PowerPlant);
            Assert.IsFalse(powerPlant.CanAfford);
            Assert.IsFalse(powerPlant.IsAvailable);
            Assert.AreEqual("Need credits", powerPlant.DisabledReason);

            TearDown();
            RtsGame missingTechGame = CreateInitializedGame(RtsRuntimeMode.Desktop);
            DestroyStructure(missingTechGame, StructureKind.Barracks);
            RtsCommandConsoleModel missingTechModel = CreateModel(missingTechGame);
            RtsBuildOptionView warFactoryMissingBarracks = GetBuildOption(missingTechModel, StructureKind.WarFactory);
            Assert.IsFalse(warFactoryMissingBarracks.HasPrerequisite);
            Assert.AreEqual("Needs Barracks", warFactoryMissingBarracks.DisabledReason);

            TearDown();
            RtsGame lowPowerGame = CreateInitializedGame(RtsRuntimeMode.Desktop);
            DestroyStructure(lowPowerGame, StructureKind.PowerPlant);
            RtsCommandConsoleModel lowPowerModel = CreateModel(lowPowerGame);
            RtsBuildOptionView warFactory = GetBuildOption(lowPowerModel, StructureKind.WarFactory);
            Assert.IsTrue(warFactory.IsAvailable);
            Assert.IsTrue(warFactory.WillCauseLowPower);
            Assert.AreEqual("Power -8", warFactory.PowerText);
        }

        [Test]
        public void ConstructionPlacementSpendsOnlyOnValidConfirmation()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            int startingCredits = game.Resources.Credits;
            int startingTurrets = CountPlayerStructures(game, StructureKind.Turret);

            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.Turret));
            Assert.AreEqual(startingCredits, game.Resources.Credits);

            Vector3 point = FindValidBuildPoint(game, StructureKind.Turret);
            game.BuildManager.UpdatePlacementAtPoint(point);
            Assert.IsTrue(game.BuildManager.PlacementValid);
            Assert.IsTrue(game.PlayerCommands.ConfirmConstructionPlacement());

            Assert.AreEqual(startingCredits - RtsBalance.GetStructure(StructureKind.Turret).Cost, game.Resources.Credits);
            Assert.AreEqual(startingTurrets + 1, CountPlayerStructures(game, StructureKind.Turret));

            int creditsAfterConfirm = game.Resources.Credits;
            Assert.IsFalse(game.PlayerCommands.ConfirmConstructionPlacement());
            Assert.AreEqual(creditsAfterConfirm, game.Resources.Credits);
        }

        [Test]
        public void CancellingPlacementSpendsNoCredits()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            int startingCredits = game.Resources.Credits;

            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.PowerPlant));
            game.BuildManager.UpdatePlacementAtPoint(FindValidBuildPoint(game, StructureKind.PowerPlant));
            Assert.IsTrue(game.PlayerCommands.CancelConstructionPlacement());

            Assert.IsFalse(game.BuildManager.IsPlacing);
            Assert.AreEqual(startingCredits, game.Resources.Credits);
        }

        [Test]
        public void InvalidPlacementCannotCreateStructure()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            int startingCredits = game.Resources.Credits;
            int startingTurrets = CountPlayerStructures(game, StructureKind.Turret);

            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.Turret));
            game.BuildManager.UpdatePlacementAtPoint(new Vector3(RtsBalance.MapHalfSize + 20f, 0f, RtsBalance.MapHalfSize + 20f));
            Assert.IsFalse(game.BuildManager.PlacementValid);
            Assert.AreEqual(BuildPlacementFailureReason.OutsideMap, game.BuildManager.LastFailureReason);
            Assert.IsFalse(game.PlayerCommands.ConfirmConstructionPlacement());

            Assert.AreEqual(startingCredits, game.Resources.Credits);
            Assert.AreEqual(startingTurrets, CountPlayerStructures(game, StructureKind.Turret));
        }

        [Test]
        public void PlacementProjectionHonorsProvidedRayLength()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            Vector3 validPoint = FindValidBuildPoint(game, StructureKind.PowerPlant);
            Ray ray = new Ray(validPoint + Vector3.up * 100f, Vector3.down);

            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.PowerPlant));

            game.CommandDispatcher.UpdatePlacement(ray, 40f);
            Assert.IsFalse(game.BuildManager.HasPlacementPoint);
            Assert.AreEqual(BuildPlacementFailureReason.NoGroundHit, game.BuildManager.LastFailureReason);

            game.CommandDispatcher.UpdatePlacement(ray, 120f);
            Assert.IsTrue(game.BuildManager.HasPlacementPoint);
            Assert.AreEqual(validPoint, game.BuildManager.PlacementPoint);
        }

        [Test]
        public void ProductionQueueSpendsOnceAndCancelRefundsQueuedItem()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            ProductionStructure barracks = FindPlayerProduction(game, StructureKind.Barracks);
            game.ClearSelection();
            game.SelectEntity(barracks, false);

            int startingCredits = game.Resources.Credits;
            int cost = RtsBalance.GetUnit(UnitKind.Rifleman).Cost;

            Assert.IsTrue(game.PlayerCommands.QueueProduction(UnitKind.Rifleman));
            Assert.AreEqual(startingCredits - cost, game.Resources.Credits);
            Assert.AreEqual(1, barracks.PendingQueueCount);

            Assert.IsTrue(game.PlayerCommands.CancelLastQueuedProduction());
            Assert.AreEqual(startingCredits, game.Resources.Credits);
            Assert.AreEqual(0, barracks.PendingQueueCount);
        }

        [Test]
        public void ProductionCancelRefundsActiveItemWhenQueueIsEmpty()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            ProductionStructure barracks = FindPlayerProduction(game, StructureKind.Barracks);
            game.ClearSelection();
            game.SelectEntity(barracks, false);

            int startingCredits = game.Resources.Credits;
            int cost = RtsBalance.GetUnit(UnitKind.Rifleman).Cost;

            Assert.IsTrue(game.PlayerCommands.QueueProduction(UnitKind.Rifleman));
            barracks.StartNextProductionForTests();

            Assert.IsTrue(barracks.HasActiveProduction);
            Assert.AreEqual(0, barracks.PendingQueueCount);
            Assert.AreEqual(startingCredits - cost, game.Resources.Credits);

            Assert.IsTrue(game.PlayerCommands.CancelProduction());
            Assert.IsFalse(barracks.HasActiveProduction);
            Assert.AreEqual(0, barracks.PendingQueueCount);
            Assert.AreEqual(startingCredits, game.Resources.Credits);
        }

        [Test]
        public void RepairAndSellRejectEnemyStructures()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            RtsStructure enemyStructure = FindStructure(game, RtsTeam.Enemy, StructureKind.CommandCenter);
            enemyStructure.TakeDamage(100f, null);

            Assert.IsFalse(game.PlayerCommands.CanRepairStructure(enemyStructure));
            Assert.IsFalse(game.PlayerCommands.CanSellStructure(enemyStructure));
        }

        [Test]
        public void QuestConsoleCapturesPanelPointerBeforeBattlefieldSelection()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            console.SetOpen(true);
            Ray panelRay = new Ray(console.PanelRect.position - console.PanelRect.forward * 8f, console.PanelRect.forward);
            Assert.IsTrue(console.TryHandlePointer(panelRay, false));
        }

        [Test]
        public void QuestConsoleReportsPanelPointerHitWithinRayLength()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();
            Assert.IsNotNull(console);
            Assert.IsNotNull(settings);

            Ray closedRay = new Ray(console.PanelRect.position - console.PanelRect.forward * 8f, console.PanelRect.forward);
            Assert.IsFalse(console.TryGetPanelHit(closedRay, out _));

            console.SetOpen(true);
            Assert.IsTrue(console.TryGetPanelHit(closedRay, out Vector3 point));
            Assert.AreEqual(console.PanelRect.position.x, point.x, 0.001f);

            Ray distantRay = new Ray(console.PanelRect.position - console.PanelRect.forward * (settings.RayLengthSimulationUnits + 1f), console.PanelRect.forward);
            Assert.IsFalse(console.TryGetPanelHit(distantRay, out _));
        }

        [Test]
        public void QuestConsoleBuildRowStartsPlacementAndQuestPrimaryConfirmsStructure()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            QuestRtsInputController controller = game.GetComponent<QuestRtsInputController>();
            Assert.IsNotNull(console);
            Assert.IsNotNull(controller);

            int startingCredits = game.Resources.Credits;
            int cost = RtsBalance.GetStructure(StructureKind.PowerPlant).Cost;
            int startingPowerPlants = CountPlayerStructures(game, StructureKind.PowerPlant);

            console.SetOpen(true);
            ClickConsoleButton(console, "Build Row 1", false);

            Assert.IsFalse(console.IsOpen);
            Assert.IsTrue(game.BuildManager.IsPlacing);
            Assert.AreEqual(StructureKind.PowerPlant, game.BuildManager.PendingKind);
            Assert.AreEqual(startingCredits, game.Resources.Credits);

            Vector3 placementPoint = FindValidBuildPoint(game, StructureKind.PowerPlant);
            Ray placementRay = RayAtPoint(placementPoint);
            Assert.AreEqual(RtsCommandResult.PlacementConfirmed, controller.ProcessInputFrameForTests(QuestFrame(placementRay, false, false, true, false, false), false));

            Assert.IsFalse(game.BuildManager.IsPlacing);
            Assert.AreEqual(startingCredits - cost, game.Resources.Credits);
            Assert.AreEqual(startingPowerPlants + 1, CountPlayerStructures(game, StructureKind.PowerPlant));
        }

        [Test]
        public void QuestConsoleProduceTabQueuesAndCancelsSelectedProducerThroughPointer()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            ProductionStructure barracks = FindPlayerProduction(game, StructureKind.Barracks);
            game.ClearSelection();
            game.SelectEntity(barracks, false);

            int startingCredits = game.Resources.Credits;
            int cost = RtsBalance.GetUnit(UnitKind.Rifleman).Cost;

            console.SetOpen(true);
            ClickConsoleButton(console, "Produce Tab");
            ClickConsoleButton(console, "Produce Row 0");

            Assert.AreEqual(startingCredits - cost, game.Resources.Credits);
            Assert.AreEqual(1, barracks.PendingQueueCount);

            ClickConsoleButton(console, "Cancel Queue Button");

            Assert.AreEqual(startingCredits, game.Resources.Credits);
            Assert.AreEqual(0, barracks.PendingQueueCount);
        }

        [Test]
        public void QuestConsoleProduceTabCancelsActiveProductionThroughPointer()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            ProductionStructure barracks = FindPlayerProduction(game, StructureKind.Barracks);
            game.ClearSelection();
            game.SelectEntity(barracks, false);

            int startingCredits = game.Resources.Credits;
            int cost = RtsBalance.GetUnit(UnitKind.Rifleman).Cost;

            Assert.IsTrue(game.PlayerCommands.QueueProduction(UnitKind.Rifleman));
            barracks.StartNextProductionForTests();
            Assert.IsTrue(barracks.HasActiveProduction);
            Assert.AreEqual(startingCredits - cost, game.Resources.Credits);

            console.SetOpen(true);
            ClickConsoleButton(console, "Produce Tab");
            ClickConsoleButton(console, "Cancel Queue Button");

            Assert.IsFalse(barracks.HasActiveProduction);
            Assert.AreEqual(startingCredits, game.Resources.Credits);
        }

        [Test]
        public void QuestConsoleSelectedTabRepairsDamagedStructureThroughPointer()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            RtsStructure powerPlant = FindStructure(game, RtsTeam.Player, StructureKind.PowerPlant);
            powerPlant.TakeDamage(100f, null);
            game.ClearSelection();
            game.SelectEntity(powerPlant, false);

            float damagedHealth = powerPlant.Health;
            float missingHealth = powerPlant.MaxHealth - powerPlant.Health;
            int expectedCost = Mathf.Clamp(Mathf.CeilToInt(missingHealth * 0.22f), 25, 180);
            int startingCredits = game.Resources.Credits;

            console.SetOpen(true);
            ClickConsoleButton(console, "Selected Tab");
            ClickConsoleButton(console, "Repair Button");

            Assert.Greater(powerPlant.Health, damagedHealth);
            Assert.AreEqual(Mathf.Min(powerPlant.MaxHealth, damagedHealth + expectedCost / 0.22f), powerPlant.Health, 0.01f);
            Assert.AreEqual(startingCredits - expectedCost, game.Resources.Credits);
        }

        [Test]
        public void QuestConsoleSelectedTabSellsSelectedStructureThroughPointer()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            RtsStructure barracks = FindStructure(game, RtsTeam.Player, StructureKind.Barracks);
            int startingCredits = game.Resources.Credits;
            int expectedRefund = Mathf.RoundToInt(RtsBalance.GetStructure(StructureKind.Barracks).Cost * 0.5f * barracks.HealthPercent);
            int startingBarracks = CountPlayerStructures(game, StructureKind.Barracks);

            game.ClearSelection();
            game.SelectEntity(barracks, false);
            console.SetOpen(true);
            ClickConsoleButton(console, "Selected Tab");
            ClickConsoleButton(console, "Sell Button");

            Assert.AreEqual(startingCredits + expectedRefund, game.Resources.Credits);
            Assert.AreEqual(startingBarracks - 1, CountPlayerStructures(game, StructureKind.Barracks));
            Assert.AreEqual(0, game.Selection.Count);
        }

        [Test]
        public void QuestConsoleSystemTabTogglesPauseThroughPointer()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            console.SetOpen(true);
            ClickConsoleButton(console, "System Tab");
            ClickConsoleButton(console, "Pause Button");

            Assert.IsTrue(game.IsUserPaused);
            Assert.IsTrue(game.Clock.IsPaused);

            ClickConsoleButton(console, "Pause Button");

            Assert.IsFalse(game.IsUserPaused);
            Assert.IsFalse(game.Clock.IsPaused);
        }

        [Test]
        public void QuestConsoleSystemTabSavesAndLoadsManualSlotThroughPointer()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-Console-" + Guid.NewGuid().ToString("N"));
            try
            {
                RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
                game.SetSaveServiceForTests(new RtsSaveService(game, new RtsSaveFileStore(tempPath)));
                QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
                Assert.IsNotNull(console);

                RtsEntity entity = FindPlayerEntity(game, typeof(RtsUnit));
                int entityId = entity.PersistentId;
                int startingCredits = game.Resources.Credits;
                entity.TakeDamage(42f, null);
                float savedHealth = entity.Health;

                console.SetOpen(true);
                ClickConsoleButton(console, "System Tab");
                ClickConsoleButton(console, "Save Button");

                Assert.IsTrue(game.CanLoadManualSave());
                Assert.IsTrue(game.GetManualSaveSummary().Contains(RtsMatchState.Running.ToString()));

                Assert.IsTrue(game.Resources.TrySpend(777));
                entity.Repair(999f);

                ClickConsoleButton(console, "Load Button");

                Assert.AreEqual(startingCredits, game.Resources.Credits);
                Assert.AreEqual(savedHealth, FindEntityById(game, entityId).Health, 0.001f);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [Test]
        public void QuestConsoleNewMatchButtonResetsMatchThroughPointer()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.QuestVr);
            QuestCommandConsole console = game.GetComponent<QuestCommandConsole>();
            Assert.IsNotNull(console);

            int startingEntities = game.Entities.Count;
            int startingResources = game.ResourceNodes.Count;
            game.ResourceNodes[0].Harvest(600);
            Assert.IsTrue(game.Resources.TrySpend(900));
            game.CreateUnit(RtsTeam.Player, UnitKind.Tank, new Vector3(-38f, 0f, -36f));
            game.SetUserPaused(true);
            game.ForceEndMatchForTests(RtsMatchState.Defeat);

            Assert.IsTrue(game.IsMatchOver);
            Assert.IsTrue(game.Clock.IsPaused);

            console.SetOpen(true);
            ClickConsoleButton(console, "System Tab");
            ClickConsoleButton(console, "New Match Button");

            Assert.AreEqual(RtsMatchState.Running, game.MatchState);
            Assert.AreEqual(0f, game.MatchTime, 0.001f);
            Assert.AreEqual(3400, game.Resources.Credits);
            Assert.AreEqual(startingEntities, game.Entities.Count);
            Assert.AreEqual(startingResources, game.ResourceNodes.Count);
            Assert.IsFalse(game.IsUserPaused);
            Assert.IsFalse(game.Clock.IsPaused);
            Assert.AreEqual(2, game.Selection.Count);
            Assert.AreEqual(game.ResourceNodes[0].MaxAmount, game.ResourceNodes[0].Amount);
        }

        [Test]
        public void ClosingMatchCancelsActivePlacement()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);
            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.Turret));
            Assert.IsTrue(game.BuildManager.IsPlacing);

            game.ForceEndMatchForTests(RtsMatchState.Defeat);

            Assert.IsTrue(game.IsMatchOver);
            Assert.IsFalse(game.BuildManager.IsPlacing);
        }

        private static RtsGame CreateInitializedGame(RtsRuntimeMode mode)
        {
            return CreateInitializedGame(mode, null);
        }

        private static RtsGame CreateInitializedGame(RtsRuntimeMode mode, RtsProfileSettings profileSettings)
        {
            RtsRuntimeModeResolver.ForceModeForTests(mode);
            GameObject root = new GameObject("Test RTS Game");
            RtsGame game = root.AddComponent<RtsGame>();
            if (profileSettings != null)
            {
                game.SetProfileSettingsForTests(profileSettings);
            }

            game.Initialize();
            Physics.SyncTransforms();
            return game;
        }

        private static RtsProfileSettings CreateProfileSettingsForTests(float tabletopScale, float pointerLength)
        {
            RtsProfileSettings settings = new RtsProfileSettings(Path.Combine(Path.GetTempPath(), "QuestCommandRTS-TestProfile.json"));
            settings.Data.tabletopScale = tabletopScale;
            settings.Data.pointerLength = pointerLength;
            settings.Data.Normalize();
            return settings;
        }

        private static Ray RayAt(RtsEntity entity)
        {
            return new Ray(entity.transform.position + Vector3.up * 18f, Vector3.down);
        }

        private static Ray RayAtPoint(Vector3 point)
        {
            return new Ray(point + Vector3.up * 18f, Vector3.down);
        }

        private static Ray RayAtResource(ResourceNode resource)
        {
            Vector3 origin = resource.transform.position + Vector3.up * 18f;
            Vector3[] offsets =
            {
                Vector3.zero,
                Vector3.forward * 0.75f,
                Vector3.back * 0.75f,
                Vector3.left * 0.75f,
                Vector3.right * 0.75f
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Ray ray = new Ray(origin + offsets[i], Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 500f) && hit.collider.GetComponentInParent<ResourceNode>() == resource)
                {
                    return ray;
                }
            }

            Assert.Fail("Could not find ray for resource " + resource.PersistentId);
            return new Ray(origin, Vector3.down);
        }

        private static QuestRtsInputFrame QuestFrame(Ray ray, bool leftTriggerHeld, bool rightTriggerHeld, bool primaryButtonHeld, bool secondaryButtonHeld, bool leftPrimaryButtonHeld)
        {
            return new QuestRtsInputFrame(ray, leftTriggerHeld, rightTriggerHeld, primaryButtonHeld, secondaryButtonHeld, leftPrimaryButtonHeld);
        }

        private static void ReleaseButtons(QuestRtsInputController controller, Ray ray)
        {
            controller.ProcessInputFrameForTests(QuestFrame(ray, false, false, false, false, false), false);
        }

        private static void ClickConsoleButton(QuestCommandConsole console, string objectName, bool expectHoverAfterClick = true)
        {
            RectTransform rect = FindRectTransform(objectName);
            Assert.IsTrue(rect.gameObject.activeInHierarchy, objectName + " should be active before it can be clicked.");

            Vector3 center = rect.TransformPoint(rect.rect.center);
            Ray ray = new Ray(center - console.PanelRect.forward * 8f, console.PanelRect.forward);
            Assert.IsTrue(console.TryHandlePointer(ray, true), objectName + " should capture pointer activation.");
            if (expectHoverAfterClick)
            {
                Assert.IsTrue(console.TryHandlePointer(ray, false), objectName + " should capture pointer hover.");
            }
        }

        private static RectTransform FindRectTransform(string objectName)
        {
            RectTransform[] rects = Object.FindObjectsOfType<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i] != null && rects[i].name == objectName)
                {
                    return rects[i];
                }
            }

            Assert.Fail("Missing RectTransform " + objectName);
            return null;
        }

        private static void AssertSceneObjectHasNoCollider(string objectName)
        {
            GameObject sceneObject = GameObject.Find(objectName);
            Assert.IsNotNull(sceneObject, "Missing generated scene object " + objectName);
            Assert.IsNull(sceneObject.GetComponent<Collider>(), objectName + " should be visual-only and should not block selection or command raycasts.");
        }

        private static void AssertSceneObjectUsesTexture(string objectName, string expectedTextureName)
        {
            GameObject sceneObject = GameObject.Find(objectName);
            Assert.IsNotNull(sceneObject, "Missing generated scene object " + objectName);
            Renderer renderer = sceneObject.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, objectName + " should have a renderer.");
            Assert.IsNotNull(renderer.sharedMaterial, objectName + " should have a shared material.");
            Texture texture = renderer.sharedMaterial.mainTexture;
            Assert.IsNotNull(texture, objectName + " should use a procedural terrain texture.");
            StringAssert.Contains(expectedTextureName, texture.name);
        }

        private static Image AssertPanelImage(string objectName, float minimumAlpha)
        {
            Image image = FindRectTransform(objectName).GetComponent<Image>();
            Assert.IsNotNull(image, objectName + " should have an Image component.");
            Assert.GreaterOrEqual(image.color.a, minimumAlpha, objectName + " should be visible in the world-space console frame.");
            return image;
        }

        private static void AssertVectorNear(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }

        private static void AssertColorNear(Color expected, Color actual)
        {
            const float tolerance = 0.005f;
            Assert.AreEqual(expected.r, actual.r, tolerance);
            Assert.AreEqual(expected.g, actual.g, tolerance);
            Assert.AreEqual(expected.b, actual.b, tolerance);
            Assert.AreEqual(expected.a, actual.a, tolerance);
        }

        private static void AssertQuaternionNear(Quaternion expected, Quaternion actual)
        {
            Assert.Greater(Mathf.Abs(Quaternion.Dot(expected, actual)), 0.9999f);
        }

        private static string ExtractMethodBody(string path, string signature)
        {
            Assert.IsTrue(File.Exists(path), "Missing source file " + path);
            string text = File.ReadAllText(path);
            int signatureIndex = text.IndexOf(signature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(signatureIndex, 0, "Missing method signature " + signature + " in " + path);

            int openBrace = text.IndexOf('{', signatureIndex);
            Assert.GreaterOrEqual(openBrace, 0, "Missing method body for " + signature + " in " + path);

            int depth = 0;
            for (int i = openBrace; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    depth++;
                }
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(openBrace, i - openBrace + 1);
                    }
                }
            }

            Assert.Fail("Unclosed method body for " + signature + " in " + path);
            return string.Empty;
        }

        private static RtsEntity FindPlayerEntity(RtsGame game, System.Type type)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity != null && entity.Team == RtsTeam.Player && type.IsInstanceOfType(entity))
                {
                    return entity;
                }
            }

            Assert.Fail("Missing player entity of type " + type.Name);
            return null;
        }

        private static RtsEntity FindEntityById(RtsGame game, int id)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                if (game.Entities[i] != null && game.Entities[i].PersistentId == id)
                {
                    return game.Entities[i];
                }
            }

            Assert.Fail("Missing entity id " + id);
            return null;
        }

        private static RtsUnit FindPlayerUnit(RtsGame game, UnitKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Player && unit.UnitKind == kind)
                {
                    return unit;
                }
            }

            Assert.Fail("Missing player unit " + kind);
            return null;
        }

        private static bool HasEntity(RtsGame game, RtsEntity expected)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                if (game.Entities[i] == expected)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountLivingPlayerUnits(RtsGame game)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Player && unit.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private static RtsUnit FindSecondPlayerUnit(RtsGame game, UnitKind kind, RtsUnit first)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit != first && unit.Team == RtsTeam.Player && unit.UnitKind == kind)
                {
                    return unit;
                }
            }

            Assert.Fail("Missing second player unit " + kind);
            return null;
        }

        private static RtsUnit FindEnemyUnit(RtsGame game, UnitKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Enemy && unit.UnitKind == kind)
                {
                    return unit;
                }
            }

            Assert.Fail("Missing enemy unit " + kind);
            return null;
        }

        private static ProductionStructure FindPlayerProduction(RtsGame game)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player)
                {
                    return producer;
                }
            }

            Assert.Fail("Missing player production structure");
            return null;
        }

        private static ProductionStructure FindPlayerProduction(RtsGame game, StructureKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.StructureKind == kind)
                {
                    return producer;
                }
            }

            Assert.Fail("Missing player production structure " + kind);
            return null;
        }

        private static RtsCommandConsoleModel CreateModel(RtsGame game)
        {
            RtsCommandConsoleModel model = new RtsCommandConsoleModel();
            model.Initialize(game);
            return model;
        }

        private static RtsBuildOptionView GetBuildOption(RtsCommandConsoleModel model, StructureKind kind)
        {
            for (int i = 0; i < model.BuildOptionCount; i++)
            {
                RtsBuildOptionView option = model.GetBuildOption(i);
                if (option.Kind == kind)
                {
                    return option;
                }
            }

            Assert.Fail("Missing build option " + kind);
            return new RtsBuildOptionView();
        }

        private static void DestroyStructure(RtsGame game, StructureKind kind)
        {
            RtsStructure structure = FindStructure(game, RtsTeam.Player, kind);
            Object.DestroyImmediate(structure.gameObject);
            Physics.SyncTransforms();
            game.RecalculatePower();
        }

        private static RtsStructure FindStructure(RtsGame game, RtsTeam team, StructureKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == team && structure.StructureKind == kind)
                {
                    return structure;
                }
            }

            Assert.Fail("Missing " + team + " structure " + kind);
            return null;
        }

        private static RtsDiagnosticsTeamSnapshot FindDiagnosticsTeam(RtsRuntimeDiagnosticsSnapshot snapshot, string team)
        {
            for (int i = 0; i < snapshot.teams.Count; i++)
            {
                if (snapshot.teams[i].team == team)
                {
                    return snapshot.teams[i];
                }
            }

            Assert.Fail("Missing diagnostics team " + team);
            return null;
        }

        private static RtsDiagnosticsUnitKindSnapshot FindDiagnosticsUnitKind(RtsRuntimeDiagnosticsSnapshot snapshot, string kind)
        {
            for (int i = 0; i < snapshot.unitKinds.Count; i++)
            {
                if (snapshot.unitKinds[i].kind == kind)
                {
                    return snapshot.unitKinds[i];
                }
            }

            Assert.Fail("Missing diagnostics unit kind " + kind);
            return null;
        }

        private static RtsDiagnosticsStructureKindSnapshot FindDiagnosticsStructureKind(RtsRuntimeDiagnosticsSnapshot snapshot, string kind)
        {
            for (int i = 0; i < snapshot.structureKinds.Count; i++)
            {
                if (snapshot.structureKinds[i].kind == kind)
                {
                    return snapshot.structureKinds[i];
                }
            }

            Assert.Fail("Missing diagnostics structure kind " + kind);
            return null;
        }

        private static int CountPlayerStructures(RtsGame game, StructureKind kind)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Player && structure.StructureKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector3 FindValidBuildPoint(RtsGame game, StructureKind kind)
        {
            BuildPlacementFailureReason reason;
            for (float z = -52f; z <= -24f; z += 4f)
            {
                for (float x = -104f; x <= -48f; x += 4f)
                {
                    Vector3 point = new Vector3(x, 0f, z);
                    if (game.BuildManager.CanPlaceAt(point, kind, out reason))
                    {
                        return point;
                    }
                }
            }

            Assert.Fail("No valid build point found for " + kind);
            return Vector3.zero;
        }

        private static void AssertValidationPassed(System.Collections.Generic.List<QuestXrProjectValidator.ValidationItem> report, string label)
        {
            QuestXrProjectValidator.ValidationItem item = FindValidationItem(report, label);
            Assert.IsTrue(item.Passed, label + " should pass. Detail: " + item.Detail);
            Assert.IsFalse(item.WarningOnly, label + " should be a hard validation item.");
        }

        private static void AssertValidationManual(System.Collections.Generic.List<QuestXrProjectValidator.ValidationItem> report, string label)
        {
            QuestXrProjectValidator.ValidationItem item = FindValidationItem(report, label);
            Assert.IsFalse(item.Passed, label + " should remain manually verified.");
            Assert.IsTrue(item.WarningOnly, label + " should be warning-only.");
        }

        private static void AssertValidationExists(System.Collections.Generic.List<QuestXrProjectValidator.ValidationItem> report, string label)
        {
            FindValidationItem(report, label);
        }

        private static QuestXrProjectValidator.ValidationItem FindValidationItem(System.Collections.Generic.List<QuestXrProjectValidator.ValidationItem> report, string label)
        {
            for (int i = 0; i < report.Count; i++)
            {
                if (report[i].Label == label)
                {
                    return report[i];
                }
            }

            Assert.Fail("Missing validation item " + label);
            return default;
        }

        private static void AssertSmokePassed(System.Collections.Generic.List<QuestRuntimeSmokeItem> report, string label)
        {
            QuestRuntimeSmokeItem item = FindSmokeItem(report, label);
            Assert.IsTrue(item.Passed, label + " should pass. Detail: " + item.Detail);
            Assert.IsFalse(item.Manual, label + " should be an automated smoke item.");
        }

        private static void AssertSmokeManual(System.Collections.Generic.List<QuestRuntimeSmokeItem> report, string label)
        {
            QuestRuntimeSmokeItem item = FindSmokeItem(report, label);
            Assert.IsFalse(item.Passed, label + " should remain manually verified.");
            Assert.IsTrue(item.Manual, label + " should be warning-only.");
        }

        private static QuestRuntimeSmokeItem FindSmokeItem(System.Collections.Generic.List<QuestRuntimeSmokeItem> report, string label)
        {
            for (int i = 0; i < report.Count; i++)
            {
                if (report[i].Label == label)
                {
                    return report[i];
                }
            }

            Assert.Fail("Missing smoke item " + label);
            return default;
        }

        private static void AssertDesktopSmokePassed(System.Collections.Generic.List<DesktopRuntimeSmokeItem> report, string label)
        {
            DesktopRuntimeSmokeItem item = FindDesktopSmokeItem(report, label);
            Assert.IsTrue(item.Passed, label + " should pass. Detail: " + item.Detail);
            Assert.IsFalse(item.Manual, label + " should be an automated smoke item.");
        }

        private static void AssertDesktopSmokeManual(System.Collections.Generic.List<DesktopRuntimeSmokeItem> report, string label)
        {
            DesktopRuntimeSmokeItem item = FindDesktopSmokeItem(report, label);
            Assert.IsFalse(item.Passed, label + " should remain manually verified.");
            Assert.IsTrue(item.Manual, label + " should be warning-only.");
        }

        private static DesktopRuntimeSmokeItem FindDesktopSmokeItem(System.Collections.Generic.List<DesktopRuntimeSmokeItem> report, string label)
        {
            for (int i = 0; i < report.Count; i++)
            {
                if (report[i].Label == label)
                {
                    return report[i];
                }
            }

            Assert.Fail("Missing desktop smoke item " + label);
            return default;
        }

        private static void AssertNoGeneratedObjects(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Assert.IsNull(GameObject.Find(names[i]), names[i] + " should be removed with the generated validation runtime.");
            }
        }

        private static void AssertBudgetPassed(System.Collections.Generic.List<RtsSceneBudgetItem> report, string label)
        {
            RtsSceneBudgetItem item = FindBudgetItem(report, label);
            Assert.IsTrue(item.Passed, label + " should pass. Detail: " + item.Detail);
        }

        private static RtsSceneBudgetItem FindBudgetItem(System.Collections.Generic.List<RtsSceneBudgetItem> report, string label)
        {
            for (int i = 0; i < report.Count; i++)
            {
                if (report[i].Label == label)
                {
                    return report[i];
                }
            }

            Assert.Fail("Missing scene budget item " + label);
            return default;
        }
    }
}
#endif
