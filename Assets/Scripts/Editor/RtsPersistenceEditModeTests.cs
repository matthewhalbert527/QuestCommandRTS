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
                File.WriteAllText(settingsPath, "{ \"schemaVersion\": 1, \"masterVolume\": 2.5, \"musicVolume\": -1, \"effectsVolume\": 4, \"pointerLength\": -9, \"tabletopScale\": 2, \"tabletopHeight\": -3, \"uiScale\": 0.1, \"qualityPreset\": \"Ultra\" }");

                RtsProfileSettings settings = new RtsProfileSettings(settingsPath);
                Assert.IsTrue(settings.TryLoad(out string error), error);
                Assert.AreEqual(1f, settings.Data.masterVolume, 0.001f);
                Assert.AreEqual(0f, settings.Data.musicVolume, 0.001f);
                Assert.AreEqual(1f, settings.Data.effectsVolume, 0.001f);
                Assert.AreEqual(3.2f, settings.Data.pointerLength, 0.001f);
                Assert.AreEqual(1.5f, settings.Data.tabletopScale, 0.001f);
                Assert.AreEqual(0.82f, settings.Data.tabletopHeight, 0.001f);
                Assert.AreEqual(0.75f, settings.Data.uiScale, 0.001f);
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
            Assert.AreEqual(0.624f, settings.CommandConsoleSizeMeters.y, 0.001f);
        }

        [Test]
        public void SaveServiceWritesAndLoadsManualSlotFromFileStore()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "QuestCommandRTS-" + Guid.NewGuid().ToString("N"));
            try
            {
                RtsGame game = CreateInitializedGame();
                RtsSaveService service = new RtsSaveService(game, new RtsSaveFileStore(tempPath));
                RtsEntity entity = FindPlayerEntity(game, typeof(RtsUnit));

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
        public void DispatcherIssuesAttackMoveAndStopOrders()
        {
            RtsGame game = CreateInitializedGame();
            RtsUnit unit = (RtsUnit)FindPlayerEntity(game, typeof(RtsUnit));
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
            RtsUnit unit = (RtsUnit)FindPlayerEntity(game, typeof(RtsUnit));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, unit.transform.position + new Vector3(3.5f, 0f, 0f));
            Physics.SyncTransforms();

            int unitId = unit.PersistentId;
            int enemyId = enemy.PersistentId;
            float enemyHealth = enemy.Health;

            unit.IssueAttackMove(unit.transform.position + new Vector3(18f, 0f, 0f));
            unit.TickOrdersForTests(0.2f);

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
            RtsEntity entity = FindPlayerEntity(game, typeof(RtsUnit));
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
