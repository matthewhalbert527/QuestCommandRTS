#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestCommandRTS.Editor
{
    public sealed class RtsFoundationPhase1EditModeTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void AssetsDoNotContainLegacyReferenceSourceFiles()
        {
            string[] blockedExtensions = { ".cpp", ".c", ".h", ".hpp", ".asm", ".mak", ".pas", ".rc" };
            List<string> matches = new List<string>();
            string assetsPath = Path.GetFullPath("Assets");
            if (!Directory.Exists(assetsPath))
            {
                Assert.Fail("Assets folder was not found.");
            }

            string[] files = Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                for (int e = 0; e < blockedExtensions.Length; e++)
                {
                    if (extension == blockedExtensions[e])
                    {
                        matches.Add(files[i]);
                        break;
                    }
                }
            }

            Assert.IsEmpty(matches, "Legacy reference source files must stay outside Assets.");
        }

        [Test]
        public void CommandResolverCoversCoreContextCommands()
        {
            RtsContextCommandResolver resolver = new RtsContextCommandResolver();
            RtsUnit rifleman = CreateUnit<RtsUnit>("Player Rifleman", RtsTeam.Player, UnitKind.Rifleman, Vector3.zero);
            RtsEntity enemy = CreateUnit<RtsUnit>("Enemy Rifleman", RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(4f, 0f, 0f));
            RtsEntity[] selection = { rifleman };

            Assert.AreEqual(
                RtsCommandKind.Attack,
                Resolve(resolver, selection, RtsCommandTarget.EntityTarget(enemy, enemy.GroundPosition, true, true)).Kind);

            Assert.AreEqual(
                RtsCommandKind.Move,
                Resolve(resolver, selection, RtsCommandTarget.Terrain(new Vector3(8f, 0f, 3f), true, true)).Kind);

            Assert.AreEqual(
                RtsCommandKind.AttackMove,
                Resolve(resolver, selection, RtsCommandTarget.Terrain(new Vector3(8f, 0f, 3f), true, true), attackMove: true).Kind);

            Assert.AreEqual(
                RtsCommandKind.Stop,
                Resolve(resolver, selection, RtsCommandTarget.None(Vector3.zero), stop: true).Kind);
        }

        [Test]
        public void CommandResolverCoversResourceRepairRallyAndInvalidStates()
        {
            RtsContextCommandResolver resolver = new RtsContextCommandResolver();
            HarvesterUnit harvester = CreateUnit<HarvesterUnit>("Harvester", RtsTeam.Player, UnitKind.Harvester, Vector3.zero);
            ResourceNode resource = CreateResource("Resource", new Vector3(3f, 0f, 3f), 1200);

            Assert.AreEqual(
                RtsCommandKind.Harvest,
                Resolve(resolver, new RtsEntity[] { harvester }, RtsCommandTarget.ResourceTarget(resource, resource.transform.position, true, true)).Kind);

            EngineerUnit engineer = CreateUnit<EngineerUnit>("Engineer", RtsTeam.Player, UnitKind.Engineer, Vector3.zero);
            RtsStructure damagedStructure = CreateStructure("Damaged Structure", RtsTeam.Player, StructureKind.PowerPlant, new Vector3(6f, 0f, 0f));
            damagedStructure.TakeDamage(25f, null);
            Assert.AreEqual(
                RtsCommandKind.Repair,
                Resolve(resolver, new RtsEntity[] { engineer }, RtsCommandTarget.EntityTarget(damagedStructure, damagedStructure.GroundPosition, true, true)).Kind);

            ProductionStructure producer = CreateStructure<ProductionStructure>("Producer", RtsTeam.Player, StructureKind.Barracks, new Vector3(-4f, 0f, 0f));
            Assert.AreEqual(
                RtsCommandKind.SetRallyPoint,
                Resolve(resolver, new RtsEntity[] { producer }, RtsCommandTarget.Terrain(new Vector3(-8f, 0f, 2f), true, true)).Kind);

            RtsCommandResolution noSelection = Resolve(resolver, Array.Empty<RtsEntity>(), RtsCommandTarget.Terrain(Vector3.zero, true, true));
            Assert.IsFalse(noSelection.IsValid);
            StringAssert.Contains("No selectable", noSelection.Reason);

            RtsEntity hiddenEnemy = CreateUnit<RtsUnit>("Hidden Enemy", RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(9f, 0f, 0f));
            RtsCommandResolution hidden = Resolve(
                resolver,
                new RtsEntity[] { harvester },
                RtsCommandTarget.EntityTarget(hiddenEnemy, hiddenEnemy.GroundPosition, false, true));
            Assert.IsFalse(hidden.IsValid);
            StringAssert.Contains("fog-hidden", hidden.Reason);
        }

        [Test]
        public void CommandResolverCanReturnSelectionCommandsWhenSelectionIsHandledThere()
        {
            RtsContextCommandResolver resolver = new RtsContextCommandResolver();
            RtsUnit friendly = CreateUnit<RtsUnit>("Friendly", RtsTeam.Player, UnitKind.Rifleman, Vector3.zero);

            RtsCommandResolution select = Resolve(
                resolver,
                Array.Empty<RtsEntity>(),
                RtsCommandTarget.EntityTarget(friendly, friendly.GroundPosition, true, true));
            Assert.AreEqual(RtsCommandKind.Select, select.Kind);

            RtsCommandResolution add = Resolve(
                resolver,
                Array.Empty<RtsEntity>(),
                RtsCommandTarget.EntityTarget(friendly, friendly.GroundPosition, true, true),
                additive: true);
            Assert.AreEqual(RtsCommandKind.AddToSelection, add.Kind);
        }

        [Test]
        public void GridConvertsWorldAndCells()
        {
            RtsGridService grid = new RtsGridService(8, 6, 2f, new Vector3(-8f, 0f, -6f));
            Vector2Int cell = new Vector2Int(3, 2);
            Vector3 center = grid.CellToWorldCenter(cell);

            Assert.AreEqual(cell, grid.WorldToCell(center));
            Assert.IsTrue(grid.IsInsideMap(cell));
            Assert.IsFalse(grid.IsInsideMap(new Vector2Int(-1, 2)));
            Assert.IsFalse(grid.IsInsideMap(new Vector2Int(8, 2)));
        }

        [Test]
        public void GridFootprintQueriesReportValidAndBlockedCells()
        {
            RtsGridService grid = new RtsGridService(8, 8, 2f, Vector3.zero);

            Assert.IsTrue(grid.CanPlaceFootprint(new Vector2Int(1, 1), 2, 2).Success);

            RtsGridQueryResult outside = grid.CanPlaceFootprint(new Vector2Int(7, 7), 2, 2);
            Assert.IsFalse(outside.Success);
            Assert.AreEqual(RtsGridQueryFailureReason.OutsideMap, outside.FailureReason);

            grid.SetBlocked(new Vector2Int(2, 2), true);
            RtsGridQueryResult blocked = grid.CanPlaceFootprint(new Vector2Int(1, 1), 2, 2);
            Assert.IsFalse(blocked.Success);
            Assert.AreEqual(RtsGridQueryFailureReason.Blocked, blocked.FailureReason);

            grid.SetBlocked(new Vector2Int(2, 2), false);
            grid.ReserveCells(new Vector2Int(1, 1), RtsBuildFootprint.Square(1));
            RtsGridQueryResult reserved = grid.CanPlaceFootprint(new Vector2Int(1, 1), 2, 2);
            Assert.IsFalse(reserved.Success);
            Assert.AreEqual(RtsGridQueryFailureReason.Reserved, reserved.FailureReason);

            grid.ReleaseReservation(new Vector2Int(1, 1), RtsBuildFootprint.Square(1));
            grid.SetOccupancy(new Vector2Int(1, 1), 1);
            RtsGridQueryResult occupied = grid.CanPlaceFootprint(new Vector2Int(1, 1), 2, 2);
            Assert.IsFalse(occupied.Success);
            Assert.AreEqual(RtsGridQueryFailureReason.Occupied, occupied.FailureReason);
        }

        [Test]
        public void GridTracksResourcesAndVisibilityFlags()
        {
            RtsGridService grid = new RtsGridService(8, 8, 2f, Vector3.zero);
            ResourceNode resource = CreateResource("Grid Resource", new Vector3(3f, 0f, 3f), 900);
            Vector2Int cell = grid.WorldToCell(resource.transform.position);

            grid.RegisterResourceNode(resource);
            Assert.IsTrue(grid.TryGetCell(cell, out RtsGridCell resourceCell));
            Assert.IsTrue(resourceCell.HasResource);
            Assert.AreEqual(900, resourceCell.ResourceAmount);
            Assert.AreSame(resource, resourceCell.ResourceNode);

            grid.SetVisible(cell, true);
            resourceCell = grid.GetCell(cell);
            Assert.IsTrue(resourceCell.IsVisible);
            Assert.IsTrue(resourceCell.IsExplored);

            grid.SetVisible(cell, false);
            resourceCell = grid.GetCell(cell);
            Assert.IsFalse(resourceCell.IsVisible);
            Assert.IsTrue(resourceCell.IsExplored);
        }

        private static RtsCommandResolution Resolve(
            RtsContextCommandResolver resolver,
            IReadOnlyList<RtsEntity> selection,
            RtsCommandTarget target,
            bool additive = false,
            bool attackMove = false,
            bool stop = false)
        {
            return resolver.Resolve(new RtsCommandRequest(
                selection,
                target,
                RtsTeam.Player,
                additive,
                attackMove,
                false,
                stop,
                true,
                false));
        }

        private T CreateUnit<T>(string name, RtsTeam team, UnitKind kind, Vector3 position) where T : RtsUnit
        {
            GameObject gameObject = CreateObject(name, position);
            T unit = gameObject.AddComponent<T>();
            unit.Initialize(team, kind);
            return unit;
        }

        private RtsStructure CreateStructure(string name, RtsTeam team, StructureKind kind, Vector3 position)
        {
            return CreateStructure<RtsStructure>(name, team, kind, position);
        }

        private T CreateStructure<T>(string name, RtsTeam team, StructureKind kind, Vector3 position) where T : RtsStructure
        {
            GameObject gameObject = CreateObject(name, position);
            T structure = gameObject.AddComponent<T>();
            structure.Initialize(team, kind);
            return structure;
        }

        private ResourceNode CreateResource(string name, Vector3 position, int amount)
        {
            GameObject gameObject = CreateObject(name, position);
            ResourceNode resource = gameObject.AddComponent<ResourceNode>();
            resource.Amount = amount;
            resource.MaxAmount = amount;
            return resource;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
#endif
