#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestCommandRTS.Editor
{
    public sealed class RtsPersistenceEditModeTests
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
        public void SimulationClockAccumulatesOnlyWhenUnpaused()
        {
            RtsSimulationClock clock = new RtsSimulationClock();
            clock.Tick(1.25f);
            Assert.AreEqual(1.25f, clock.SimulationTime, 0.001f);

            clock.SetPaused(RtsPauseReason.ApplicationPause, true);
            clock.Tick(5f);
            Assert.AreEqual(1.25f, clock.SimulationTime, 0.001f);
            Assert.AreEqual(0f, clock.DeltaTime, 0.001f);

            clock.SetPaused(RtsPauseReason.ApplicationPause, false);
            clock.Tick(0.75f);
            Assert.AreEqual(2f, clock.SimulationTime, 0.001f);
        }

        [Test]
        public void SaveSerializerRejectsCorruption()
        {
            RtsMatchSaveData data = new RtsMatchSaveData();
            data.resources.credits = 1234;

            string json = RtsSaveSerializer.Serialize("test", data);
            Assert.IsTrue(RtsSaveSerializer.TryDeserialize(json, out RtsMatchSaveData restored, out string error), error);
            Assert.AreEqual(1234, restored.resources.credits);

            string corrupted = json.Replace("1234", "4321");
            Assert.IsFalse(RtsSaveSerializer.TryDeserialize(corrupted, out restored, out error));
            Assert.IsTrue(error.Contains("checksum"));
        }

        [Test]
        public void InitializedGameAssignsStableIds()
        {
            RtsGame game = CreateInitializedGame();

            Assert.Greater(game.Entities.Count, 0);
            Assert.Greater(game.ResourceNodes.Count, 0);

            for (int i = 0; i < game.Entities.Count; i++)
            {
                Assert.Greater(game.Entities[i].PersistentId, 0);
            }

            for (int i = 0; i < game.ResourceNodes.Count; i++)
            {
                Assert.Greater(game.ResourceNodes[i].PersistentId, 0);
            }
        }

        [Test]
        public void LifecycleFocusLossPausesAndBlocksCommands()
        {
            RtsGame game = CreateInitializedGame();

            Assert.IsTrue(game.AcceptsPlayerInput);
            game.Lifecycle.SetInputFocusForTests(false);

            Assert.IsFalse(game.AcceptsPlayerInput);
            Assert.IsTrue(game.Clock.IsPaused);
            Assert.IsFalse(game.PlayerCommands.QueueProduction(UnitKind.Rifleman));

            game.Lifecycle.SetInputFocusForTests(true);
            Assert.IsTrue(game.AcceptsPlayerInput);
            Assert.IsFalse(game.Clock.IsPaused);
        }

        [Test]
        public void UserPauseKeepsSystemInputWhileBlockingGameplayCommands()
        {
            RtsGame game = CreateInitializedGame();

            game.SetUserPaused(true);

            Assert.IsTrue(game.IsUserPaused);
            Assert.IsTrue(game.AcceptsSystemInput);
            Assert.IsFalse(game.AcceptsPlayerInput);
            Assert.IsFalse(game.PlayerCommands.QueueProduction(UnitKind.Rifleman));

            game.SetUserPaused(false);

            Assert.IsFalse(game.IsUserPaused);
            Assert.IsTrue(game.AcceptsSystemInput);
            Assert.IsTrue(game.AcceptsPlayerInput);
        }

        [Test]
        public void SaveMigrationRejectsFutureVersions()
        {
            RtsMatchSaveData data = new RtsMatchSaveData
            {
                schemaVersion = RtsSaveSerializer.CurrentSchemaVersion + 10
            };

            Assert.IsFalse(RtsSaveMigration.TryMigrate(data, out string error));
            Assert.IsTrue(error.Contains("newer"));
        }

        [Test]
        public void ProfileSettingsClampInvalidValuesOnLoadAndSave()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-Profile-" + Guid.NewGuid().ToString("N"));
            string settingsPath = Path.Combine(tempPath, "profile-settings.json");

            try
            {
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(settingsPath, "{ \"schemaVersion\": 1, \"masterVolume\": 2.5, \"musicVolume\": -1, \"effectsVolume\": 4, \"pointerLength\": -9, \"tabletopScale\": 99, \"tabletopHeight\": -3, \"uiScale\": 0.1, \"qualityPreset\": \"Ultra\", \"periodicAutosaveIntervalSeconds\": 9999 }");

                RtsProfileSettings settings = new RtsProfileSettings(settingsPath);
                Assert.IsTrue(settings.TryLoad(out string error), error);
                Assert.AreEqual(1f, settings.Data.masterVolume, 0.001f);
                Assert.AreEqual(0f, settings.Data.musicVolume, 0.001f);
                Assert.AreEqual(1f, settings.Data.effectsVolume, 0.001f);
                Assert.AreEqual(3.2f, settings.Data.pointerLength, 0.001f);
                Assert.AreEqual(RtsProfileSettingsData.MaxTabletopScale, settings.Data.tabletopScale, 0.001f);
                Assert.AreEqual(0.82f, settings.Data.tabletopHeight, 0.001f);
                Assert.AreEqual(0.75f, settings.Data.uiScale, 0.001f);
                Assert.AreEqual(900f, settings.Data.periodicAutosaveIntervalSeconds, 0.001f);
                Assert.AreEqual("Balanced", settings.Data.qualityPreset);

                settings.Data.masterVolume = 7f;
                settings.Data.qualityPreset = "quality";
                Assert.IsTrue(settings.TrySave(out error), error);
                Assert.IsFalse(File.Exists(settingsPath + ".tmp"));

                RtsProfileSettings restored = new RtsProfileSettings(settingsPath);
                Assert.IsTrue(restored.TryLoad(out error), error);
                Assert.AreEqual(1f, restored.Data.masterVolume, 0.001f);
                Assert.AreEqual("Quality", restored.Data.qualityPreset);
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
        public void ProfileSettingsRejectFutureSchemaWithoutCrashing()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-Profile-" + Guid.NewGuid().ToString("N"));
            string settingsPath = Path.Combine(tempPath, "profile-settings.json");

            try
            {
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(settingsPath, "{ \"schemaVersion\": 99, \"masterVolume\": 0.25 }");

                RtsProfileSettings settings = new RtsProfileSettings(settingsPath);
                Assert.IsFalse(settings.TryLoad(out string error));
                Assert.IsTrue(error.Contains("newer"));
                Assert.AreEqual(RtsProfileSettingsData.CurrentSchemaVersion, settings.Data.schemaVersion);
                Assert.AreEqual(1f, settings.Data.masterVolume, 0.001f);
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
        public void QuestTabletopSettingsApplyProfileScaleHeightPointerAndUi()
        {
            GameObject root = new GameObject("Quest Settings Test");
            QuestTabletopSettings settings = root.AddComponent<QuestTabletopSettings>();
            RtsProfileSettingsData profile = new RtsProfileSettingsData
            {
                tabletopScale = 1.25f,
                tabletopHeight = 1.1f,
                pointerLength = 4.5f,
                uiScale = 1.2f
            };

            settings.ApplyProfile(profile);

            Assert.AreEqual(100.8f, settings.SimulationUnitsPerMeter, 0.001f);
            Assert.AreEqual(1.1f, settings.BoardHeightMeters, 0.001f);
            Assert.AreEqual(4.5f, settings.RayLengthMeters, 0.001f);
            Assert.AreEqual(2.222f, settings.BattlefieldWidthMeters, 0.01f);
            Assert.AreEqual(0.696f, settings.StatusPanelSizeMeters.x, 0.001f);
            Assert.AreEqual(0.312f, settings.StatusPanelSizeMeters.y, 0.001f);
            Assert.AreEqual(0.624f, settings.CommandConsoleSizeMeters.y, 0.001f);
        }

        [Test]
        public void QuestTabletopSettingsSupportRoomSizedProfileScale()
        {
            GameObject root = new GameObject("Quest Room Scale Settings Test");
            QuestTabletopSettings settings = root.AddComponent<QuestTabletopSettings>();
            RtsProfileSettingsData profile = new RtsProfileSettingsData
            {
                tabletopScale = RtsProfileSettingsData.RoomSizedTabletopScale,
                pointerLength = RtsProfileSettingsData.RoomSizedPointerLength
            };

            settings.ApplyProfile(profile);

            Assert.AreEqual(126f / RtsProfileSettingsData.RoomSizedTabletopScale, settings.SimulationUnitsPerMeter, 0.001f);
            Assert.AreEqual(4f, settings.BattlefieldWidthMeters, 0.01f);
            Assert.AreEqual(RtsProfileSettingsData.RoomSizedPointerLength, settings.RayLengthMeters, 0.001f);
        }

        [Test]
        public void SaveServiceWritesAndLoadsManualSlotFromFileStore()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-" + Guid.NewGuid().ToString("N"));
            try
            {
                RtsGame game = CreateInitializedGame();
                RtsSaveService service = new RtsSaveService(game, new RtsSaveFileStore(tempPath));
                RtsEntity entity = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-76f, 0f, -72f));

                int entityId = entity.PersistentId;
                int credits = game.Resources.Credits;
                entity.TakeDamage(22f, null);
                float damagedHealth = entity.Health;

                Assert.IsTrue(service.TryWriteSlot("manual", out string error), error);
                Assert.IsTrue(service.HasSlot("manual"));

                game.Resources.TrySpend(250);
                entity.Repair(999f);

                Assert.IsTrue(service.TryLoadSlot("manual", out error), error);
                Assert.AreEqual(credits, game.Resources.Credits);
                Assert.AreEqual(damagedHealth, FindEntityById(game, entityId).Health, 0.001f);
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
        public void SaveSerializerExposesValidatedMetadata()
        {
            RtsMatchSaveData data = new RtsMatchSaveData
            {
                matchTime = 123.4f,
                matchState = RtsMatchState.Running.ToString(),
                statusMessage = "Metadata test",
                resources = new RtsResourceBankSaveData
                {
                    credits = 456,
                    powerProvided = 12,
                    powerUsed = 8
                }
            };
            data.entities.Add(new RtsEntitySaveData { id = 7, entityType = "Unit", team = RtsTeam.Player.ToString() });
            data.resourceNodes.Add(new RtsResourceNodeSaveData { id = 9, amount = 100, maxAmount = 200 });

            string json = RtsSaveSerializer.Serialize("manual", data);

            Assert.IsTrue(RtsSaveSerializer.TryReadMetadata(json, out RtsSaveMetadata metadata, out string error), error);
            Assert.AreEqual("manual", metadata.slotId);
            Assert.AreEqual(RtsSaveSerializer.CurrentSchemaVersion, metadata.schemaVersion);
            Assert.AreEqual(Application.version, metadata.gameVersion);
            Assert.AreEqual(Application.version, metadata.applicationVersion);
            Assert.AreEqual("default_skirmish_v1", metadata.skirmishConfigId);
            Assert.AreEqual("standard", metadata.difficultyId);
            Assert.AreEqual("room_tabletop_v1", metadata.mapId);
            Assert.AreEqual(527, metadata.mapSeed);
            Assert.IsFalse(string.IsNullOrEmpty(metadata.savedUtc));
            Assert.AreEqual(123.4f, metadata.matchTime, 0.001f);
            Assert.AreEqual(RtsMatchState.Running.ToString(), metadata.matchState);
            Assert.AreEqual(456, metadata.playerCredits);
            Assert.AreEqual(1, metadata.entityCount);
            Assert.AreEqual(1, metadata.resourceNodeCount);
        }

        [Test]
        public void SaveServiceFallsBackToBackupWhenPrimarySlotIsCorrupt()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-" + Guid.NewGuid().ToString("N"));
            try
            {
                RtsGame game = CreateInitializedGame();
                RtsSaveFileStore store = new RtsSaveFileStore(tempPath);
                RtsSaveService service = new RtsSaveService(game, store);
                RtsEntity entity = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-76f, 0f, -72f));

                int entityId = entity.PersistentId;
                int backupCredits = game.Resources.Credits;
                entity.TakeDamage(35f, null);
                float backupHealth = entity.Health;

                Assert.IsTrue(service.TryWriteSlot("manual", out string error), error);

                game.Resources.TrySpend(500);
                entity.Repair(999f);
                Assert.IsTrue(service.TryWriteSlot("manual", out error), error);
                Assert.IsTrue(File.Exists(store.GetBackupSlotPath("manual")));

                File.WriteAllText(store.GetSlotPath("manual"), "{ corrupt primary save");
                game.Resources.TrySpend(100);
                entity.TakeDamage(12f, null);

                Assert.IsTrue(service.TryLoadSlot("manual", out error), error);
                Assert.AreEqual(backupCredits, game.Resources.Credits);
                Assert.AreEqual(backupHealth, FindEntityById(game, entityId).Health, 0.001f);
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
        public void SaveServiceMetadataFallsBackToBackupAndListsBackupOnlySlots()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-" + Guid.NewGuid().ToString("N"));
            try
            {
                RtsGame game = CreateInitializedGame();
                RtsSaveFileStore store = new RtsSaveFileStore(tempPath);
                RtsSaveService service = new RtsSaveService(game, store);
                game.SetSaveServiceForTests(service);

                game.SetMatchTimeForTests(42f);
                Assert.IsTrue(service.TryWriteSlot("manual", out string error), error);

                game.SetMatchTimeForTests(87f);
                game.Resources.TrySpend(100);
                Assert.IsTrue(service.TryWriteSlot("manual", out error), error);
                Assert.IsTrue(File.Exists(store.GetBackupSlotPath("manual")));

                File.WriteAllText(store.GetSlotPath("manual"), "{ corrupt primary metadata");

                Assert.IsTrue(service.TryGetSlotMetadata("manual", out RtsSaveMetadata metadata, out error), error);
                Assert.IsTrue(metadata.readFromBackup);
                Assert.AreEqual(42f, metadata.matchTime, 0.001f);
                Assert.AreEqual("backup 0:42 " + RtsMatchState.Running, game.GetManualSaveSummary());

                File.Delete(store.GetSlotPath("manual"));
                System.Collections.Generic.List<string> slots = service.ListSlots();
                Assert.Contains("manual", slots);

                System.Collections.Generic.List<RtsSaveMetadata> allMetadata = service.ListSlotMetadata();
                Assert.AreEqual(1, allMetadata.Count);
                Assert.IsTrue(allMetadata[0].readFromBackup);
                Assert.AreEqual("manual", allMetadata[0].slotId);
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
        public void LifecyclePeriodicAutosaveUsesConfiguredIntervalAndRequiresActiveInput()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-" + Guid.NewGuid().ToString("N"));
            try
            {
                RtsGame game = CreateInitializedGame();
                RtsSaveFileStore store = new RtsSaveFileStore(tempPath);
                game.SetSaveServiceForTests(new RtsSaveService(game, store));
                game.ProfileSettings.Data.periodicAutosaveIntervalSeconds = 30f;

                game.Lifecycle.ScheduleNextPeriodicAutosaveForTests(0f);
                Assert.IsFalse(game.Lifecycle.EvaluatePeriodicAutosaveForTests(29f));
                Assert.IsFalse(store.HasSlot(RtsLifecycleCoordinator.PeriodicAutosaveSlot));

                game.Lifecycle.SetInputFocusForTests(false);
                game.Lifecycle.ScheduleNextPeriodicAutosaveForTests(0f);
                Assert.IsFalse(game.Lifecycle.EvaluatePeriodicAutosaveForTests(30f));
                Assert.IsFalse(store.HasSlot(RtsLifecycleCoordinator.PeriodicAutosaveSlot));
                Assert.IsTrue(store.HasSlot("focus-autosave"));

                game.Lifecycle.SetInputFocusForTests(true);
                game.Lifecycle.ScheduleNextPeriodicAutosaveForTests(0f);
                Assert.IsTrue(game.Lifecycle.EvaluatePeriodicAutosaveForTests(30f));
                Assert.IsTrue(store.HasSlot(RtsLifecycleCoordinator.PeriodicAutosaveSlot));
                Assert.IsFalse(game.Lifecycle.EvaluatePeriodicAutosaveForTests(30.5f));
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
        public void DispatcherIssuesAttackMoveAndStopOrders()
        {
            RtsGame game = CreateInitializedGame();
            RtsUnit unit = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-76f, 0f, -72f));
            game.ClearSelection();
            game.SelectEntity(unit, false);

            Vector3 targetPoint = unit.transform.position + new Vector3(14f, 0f, 9f);
            Assert.AreEqual(RtsCommandResult.AttackMoveIssued, game.CommandDispatcher.AttackMoveToPoint(targetPoint));

            RtsUnitOrderSaveData attackMove = unit.CaptureOrderState();
            Assert.AreEqual("AttackMove", attackMove.orderType);
            Assert.AreEqual(targetPoint.x, attackMove.destination.x, 0.01f);

            Assert.AreEqual(RtsCommandResult.StopIssued, game.CommandDispatcher.StopSelectedUnits());
            Assert.IsTrue(unit.IsIdle());
            Assert.AreEqual("None", unit.CaptureOrderState().orderType);
        }

        [Test]
        public void AttackMoveAcquiresEnemyAndPersistsThroughRestore()
        {
            RtsGame game = CreateInitializedGame();
            RtsUnit unit = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-76f, 0f, -72f));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, unit.transform.position + new Vector3(3.5f, 0f, 0f));
            Physics.SyncTransforms();

            int unitId = unit.PersistentId;
            int enemyId = enemy.PersistentId;
            float enemyHealth = enemy.Health;

            unit.IssueAttackMove(unit.transform.position + new Vector3(18f, 0f, 0f));
            TickCombatUntilDamaged(unit, enemy, enemyHealth);

            Assert.Less(enemy.Health, enemyHealth);
            RtsUnitOrderSaveData acquired = unit.CaptureOrderState();
            Assert.AreEqual("AttackMove", acquired.orderType);
            Assert.AreEqual(enemyId, acquired.targetEntityId);

            RtsMatchSaveData saved = game.CaptureSaveData();
            Assert.IsTrue(game.RestoreSaveData(saved, out string error), error);

            RtsUnit restoredUnit = (RtsUnit)FindEntityById(game, unitId);
            RtsUnitOrderSaveData restoredOrder = restoredUnit.CaptureOrderState();
            Assert.AreEqual("AttackMove", restoredOrder.orderType);
            Assert.AreEqual(enemyId, restoredOrder.targetEntityId);
        }

        [Test]
        public void EnemyDirectorEconomyStatePersistsThroughRestore()
        {
            RtsGame game = CreateInitializedGame();
            game.EnemyDirector.SetEconomyForTests(2345, 11f, 12f, 13f, 31f, 7f, 3);

            RtsMatchSaveData saved = game.CaptureSaveData();

            Assert.IsTrue(saved.enemyDirector.hasEconomyState);
            Assert.AreEqual(2345, saved.enemyDirector.enemyCredits);
            Assert.AreEqual(3, saved.enemyDirector.waveIndex);

            game.EnemyDirector.SetEconomyForTests(10, 1f, 1f, 1f, 1f, 1f, 0);
            Assert.IsTrue(game.RestoreSaveData(saved, out string error), error);

            RtsEnemyDirectorSaveData restored = game.EnemyDirector.CaptureState();
            Assert.AreEqual(2345, game.EnemyDirector.EnemyCreditsForTests);
            Assert.AreEqual(3, restored.waveIndex);
            Assert.AreEqual(11f, restored.nextIncomeTime, 0.001f);
            Assert.AreEqual(12f, restored.nextBuildTime, 0.001f);
            Assert.AreEqual(13f, restored.nextProductionTime, 0.001f);
        }

        [Test]
        public void EnemyDirectorSpendsCreditsToProduceUnits()
        {
            RtsGame game = CreateInitializedGame();
            game.CreateStructure(RtsTeam.Enemy, StructureKind.Barracks, new Vector3(74f, 0f, 64f));
            int beforeUnits = CountLivingEnemyUnits(game);

            game.EnemyDirector.SetEnemyCreditsForTests(500);
            Assert.IsTrue(game.EnemyDirector.TryProduceUnitForTests());

            Assert.AreEqual(beforeUnits + 1, CountLivingEnemyUnits(game));
            Assert.Less(game.EnemyDirector.EnemyCreditsForTests, 500);
        }

        [Test]
        public void EnemyDirectorRebuildsMissingPowerPlant()
        {
            RtsGame game = CreateInitializedGame();
            game.CreateStructure(RtsTeam.Enemy, StructureKind.PowerPlant, new Vector3(96f, 0f, 62f));
            RtsStructure powerPlant = FindEnemyStructure(game, StructureKind.PowerPlant);
            powerPlant.SetHealthForRestore(0f);

            Assert.AreEqual(0, CountLivingEnemyStructures(game, StructureKind.PowerPlant));

            game.EnemyDirector.SetEnemyCreditsForTests(1000);
            Assert.IsTrue(game.EnemyDirector.TryBuildBaseStructureForTests());

            Assert.AreEqual(1, CountLivingEnemyStructures(game, StructureKind.PowerPlant));
            Assert.Less(game.EnemyDirector.EnemyCreditsForTests, 1000);
        }

        [Test]
        public void SaveRestoreRoundTripsCoreSkirmishState()
        {
            RtsGame game = CreateInitializedGame();
            RtsEntity entity = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-76f, 0f, -72f));
            ResourceNode node = game.ResourceNodes[0];

            int entityId = entity.PersistentId;
            int nodeId = node.PersistentId;
            entity.TakeDamage(17f, null);
            node.Harvest(321);
            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.PowerPlant));
            game.BuildManager.UpdatePlacementAtPoint(FindValidBuildPoint(game, StructureKind.PowerPlant));

            RtsMatchSaveData saved = game.CaptureSaveData();
            int savedCredits = saved.resources.credits;
            float savedHealth = entity.Health;
            int savedAmount = node.Amount;

            game.Resources.TrySpend(500);
            entity.Repair(999f);
            node.Harvest(200);

            Assert.IsTrue(game.RestoreSaveData(saved, out string error), error);

            Assert.AreEqual(savedCredits, game.Resources.Credits);
            Assert.AreEqual(savedHealth, FindEntityById(game, entityId).Health, 0.001f);
            Assert.AreEqual(savedAmount, FindResourceById(game, nodeId).Amount);
            Assert.IsTrue(game.BuildManager.IsPlacing);
            Assert.AreEqual(StructureKind.PowerPlant, game.BuildManager.PendingKind);
        }

        [Test]
        public void RestartMatchClearsRestoredStateAndResetsLifecycle()
        {
            RtsGame game = CreateInitializedGame();
            int startingEntities = game.Entities.Count;
            int startingResources = game.ResourceNodes.Count;

            ResourceNode node = game.ResourceNodes[0];
            node.Harvest(500);
            game.Resources.TrySpend(700);
            game.CreateUnit(RtsTeam.Player, UnitKind.Tank, new Vector3(-40f, 0f, -40f));
            game.EnemyDirector.SetEconomyForTests(3210, 99f, 98f, 97f, 96f, 95f, 4);
            Assert.IsTrue(game.PlayerCommands.RequestConstruction(StructureKind.PowerPlant));
            game.BuildManager.UpdatePlacementAtPoint(FindValidBuildPoint(game, StructureKind.PowerPlant));
            game.SetUserPaused(true);

            RtsMatchSaveData dirtySave = game.CaptureSaveData();
            Assert.IsTrue(game.RestoreSaveData(dirtySave, out string error), error);
            game.ForceEndMatchForTests(RtsMatchState.Defeat);

            Assert.IsTrue(game.IsMatchOver);
            Assert.IsTrue(game.Clock.IsPaused);
            Assert.IsTrue(game.TryRestartMatch());

            Assert.AreEqual(RtsMatchState.Running, game.MatchState);
            Assert.AreEqual(0f, game.MatchTime, 0.001f);
            Assert.IsFalse(game.Clock.IsPaused);
            Assert.IsFalse(game.IsUserPaused);
            Assert.AreEqual(6200, game.Resources.Credits);
            Assert.AreEqual(startingEntities, game.Entities.Count);
            Assert.AreEqual(startingResources, game.ResourceNodes.Count);
            Assert.AreEqual(1, game.Selection.Count);
            Assert.IsFalse(game.BuildManager.IsPlacing);
            Assert.AreEqual(2600, game.EnemyDirector.EnemyCreditsForTests);

            for (int i = 0; i < game.ResourceNodes.Count; i++)
            {
                Assert.AreEqual(game.ResourceNodes[i].MaxAmount, game.ResourceNodes[i].Amount);
            }
        }

        private static RtsGame CreateInitializedGame()
        {
            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.Desktop);
            GameObject root = new GameObject("Test RTS Game");
            RtsGame game = root.AddComponent<RtsGame>();
            game.Initialize();
            Physics.SyncTransforms();
            return game;
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

        private static void TickCombatUntilDamaged(RtsUnit attacker, RtsEntity target, float startingHealth)
        {
            for (int i = 0; i < 80 && target.Health >= startingHealth; i++)
            {
                if (attacker != null)
                {
                    attacker.TickOrdersForTests(0.1f);
                }

                RtsProjectile[] projectiles = Object.FindObjectsOfType<RtsProjectile>();
                for (int projectileIndex = 0; projectileIndex < projectiles.Length; projectileIndex++)
                {
                    if (projectiles[projectileIndex] != null)
                    {
                        projectiles[projectileIndex].TickProjectileForTests(0.1f);
                    }
                }
            }
        }

        private static RtsStructure FindEnemyStructure(RtsGame game, StructureKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Enemy && structure.IsAlive && structure.StructureKind == kind)
                {
                    return structure;
                }
            }

            Assert.Fail("Missing enemy structure " + kind);
            return null;
        }

        private static int CountLivingEnemyStructures(RtsGame game, StructureKind kind)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Enemy && structure.IsAlive && structure.StructureKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountLivingEnemyUnits(RtsGame game)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Enemy && unit.IsAlive)
                {
                    count++;
                }
            }

            return count;
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

            Assert.Fail("Missing entity " + id);
            return null;
        }

        private static ResourceNode FindResourceById(RtsGame game, int id)
        {
            for (int i = 0; i < game.ResourceNodes.Count; i++)
            {
                if (game.ResourceNodes[i] != null && game.ResourceNodes[i].PersistentId == id)
                {
                    return game.ResourceNodes[i];
                }
            }

            Assert.Fail("Missing resource " + id);
            return null;
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
    }
}
#endif
