#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

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
