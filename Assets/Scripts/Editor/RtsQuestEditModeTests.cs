#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

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
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(false, false, false));
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(false, false, true));
            Assert.IsFalse(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(true, false, false));
            Assert.IsTrue(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(true, false, true));
            Assert.IsTrue(RtsRuntimeModeResolver.EvaluateXrRuntimeStateForTests(false, true, false));
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
            Assert.IsNotNull(game.GetComponent<QuestCommandConsole>());
            Assert.IsNotNull(game.QuestRig);
            Assert.AreSame(game.QuestRig.HeadCamera.transform, game.GetViewCameraTransform());
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
        public void QuestValidatorReportsCorePackageAndAndroidSettings()
        {
            var report = QuestXrProjectValidator.BuildValidationReport();

            AssertValidationPassed(report, "Unity version");
            AssertValidationPassed(report, "XR Management package");
            AssertValidationPassed(report, "OpenXR package");
            AssertValidationPassed(report, "XR Interaction Toolkit package");
            AssertValidationPassed(report, "Input System package");
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

            game.CommandDispatcher.SelectFromRay(new Ray(new Vector3(220f, 20f, 220f), Vector3.down), false, 500f);
            Assert.AreEqual(0, game.Selection.Count);
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
            Assert.AreEqual(RtsContextCommandKind.Harvest, game.CommandDispatcher.ResolveContextCommand(null, resource, resource.transform.position));

            ProductionStructure producer = (ProductionStructure)FindPlayerProduction(game);
            game.ClearSelection();
            game.SelectEntity(producer, false);
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
            RtsRuntimeModeResolver.ForceModeForTests(mode);
            GameObject root = new GameObject("Test RTS Game");
            RtsGame game = root.AddComponent<RtsGame>();
            game.Initialize();
            Physics.SyncTransforms();
            return game;
        }

        private static Ray RayAt(RtsEntity entity)
        {
            return new Ray(entity.transform.position + Vector3.up * 18f, Vector3.down);
        }

        private static Ray RayAtPoint(Vector3 point)
        {
            return new Ray(point + Vector3.up * 18f, Vector3.down);
        }

        private static QuestRtsInputFrame QuestFrame(Ray ray, bool leftTriggerHeld, bool rightTriggerHeld, bool primaryButtonHeld, bool secondaryButtonHeld, bool leftPrimaryButtonHeld)
        {
            return new QuestRtsInputFrame(ray, leftTriggerHeld, rightTriggerHeld, primaryButtonHeld, secondaryButtonHeld, leftPrimaryButtonHeld);
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
    }
}
#endif
