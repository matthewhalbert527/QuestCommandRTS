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
        public void DesktopInitializationCreatesDesktopCameraInputAndHud()
        {
            RtsGame game = CreateInitializedGame(RtsRuntimeMode.Desktop);

            Assert.AreEqual(RtsRuntimeMode.Desktop, game.RuntimeMode);
            Assert.IsNotNull(game.CommandCamera);
            Assert.IsNotNull(game.GetComponent<RtsInputController>());
            Assert.IsNotNull(game.GetComponent<RtsHud>());
            Assert.IsNull(game.GetComponent<QuestRtsInputController>());
            Assert.IsNull(game.QuestRig);
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
            Assert.IsNotNull(game.QuestRig);
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
    }
}
#endif
