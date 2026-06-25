#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestCommandRTS.Editor
{
    public static class RtsCombatOpeningValidator
    {
        [MenuItem("Command RTS/Validate Combat Opening Pass")]
        public static void ValidateCombatOpeningPass()
        {
            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.Desktop);
            GameObject root = new GameObject("Combat Opening Validation");
            try
            {
                RtsGame game = root.AddComponent<RtsGame>();
                game.Initialize();
                Physics.SyncTransforms();

                ValidateOpeningState(game);
                ValidateProducerRouting(game);
                ValidateTankTracks(game);
                ValidateProjectileDamage(game);
                ValidateTankTurretDelayAndMobileFire(game);
                ValidateUnitBlocking(game);
                ValidateEnemyOpeningGrace(game);
                Debug.Log("[Command RTS Combat] PASS - Combat, production, tracks, unit blocking, and opening pacing validated.");
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
                Object.DestroyImmediate(root);
                GameObject eventSystem = GameObject.Find("EventSystem");
                if (eventSystem != null)
                {
                    Object.DestroyImmediate(eventSystem);
                }
            }
        }

        private static void ValidateOpeningState(RtsGame game)
        {
            Require(CountStructures(game, RtsTeam.Player, StructureKind.CommandCenter) == 1, "Player fabrication start", "Player should start with one fabrication/command structure.");
            Require(CountUnits(game, RtsTeam.Player) == 0, "No starting player units", "Player should build into infantry and vehicles.");
            Require(CountStructures(game, RtsTeam.Player, StructureKind.Barracks) == 0, "No starting barracks", "Barracks should be built by the player.");
            Require(CountStructures(game, RtsTeam.Player, StructureKind.WarFactory) == 0, "No starting war factory", "Vehicle production should be earned.");
            Require(CountStructures(game, RtsTeam.Enemy, StructureKind.CommandCenter) == 1, "Enemy fabrication start", "Enemy should also begin from a fabrication/command structure.");
            Require(CountUnits(game, RtsTeam.Enemy) == 0, "No starting enemy units", "Enemy attacks should not begin immediately.");
            Require(game.Resources.Credits >= 6000, "Bootstrap credits", "Opening credits should support a first build order from fabrication only.");
        }

        private static void ValidateProducerRouting(RtsGame game)
        {
            ProductionStructure command = FindProducer(game, RtsTeam.Player, StructureKind.CommandCenter);
            Require(!command.CanTrain(UnitKind.Rifleman), "Command cannot train soldiers", "Infantry should come from the barracks.");
            Require(!command.CanTrain(UnitKind.Harvester), "Command cannot train vehicles", "Vehicles should come from the war factory.");

            ProductionStructure barracks = game.CreateStructure(RtsTeam.Player, StructureKind.Barracks, new Vector3(-76f, 0f, -62f)) as ProductionStructure;
            ProductionStructure factory = game.CreateStructure(RtsTeam.Player, StructureKind.WarFactory, new Vector3(-68f, 0f, -62f)) as ProductionStructure;
            Require(barracks != null && barracks.CanTrain(UnitKind.Rifleman), "Barracks trains infantry", "Soldiers should spawn from the barracks.");
            Require(barracks != null && !barracks.CanTrain(UnitKind.MediumTank), "Barracks rejects vehicles", "Tanks should not come from barracks.");
            Require(factory != null && factory.CanTrain(UnitKind.MediumTank), "War factory trains tanks", "Tanks should spawn from the war factory.");
            Require(factory != null && factory.CanTrain(UnitKind.Harvester), "War factory trains harvesters", "Harvesters should be treated as vehicles.");
            Require(factory != null && !factory.CanTrain(UnitKind.Rifleman), "War factory rejects infantry", "Soldiers should not come from the war factory.");
        }

        private static void ValidateTankTracks(RtsGame game)
        {
            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-52f, 0f, -46f));
            RtsUnitVisualAnimator animator = tank.GetComponent<RtsUnitVisualAnimator>();
            Require(animator != null && animator.HasTrackRigForTests, "Tank track rig", "Tanks should use treaded track rigs instead of wheel primitives.");
            Require(tank.transform.Find("Track Belt L") != null && tank.transform.Find("Track Belt R") != null, "Track belts present", "Both side track belts should be visible.");
            Require(tank.transform.Find("Roll Wheel LF") == null, "No tank wheel rig", "Tanks should not add round wheel primitives.");

            Transform firstPad = animator.FirstTrackPadForTests;
            Vector3 start = firstPad.localPosition;
            tank.transform.position += tank.transform.forward * 1.4f;
            animator.TickVisualsForTests(0.2f);
            Require((firstPad.localPosition - start).sqrMagnitude > 0.0001f, "Track tread animation", "Tread pads should move when the tank rolls.");
        }

        private static void ValidateProjectileDamage(RtsGame game)
        {
            RtsUnit gunner = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-20f, 0f, -20f));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, new Vector3(-16f, 0f, -20f));
            float healthBefore = enemy.Health;

            gunner.IssueAttack(enemy);
            gunner.TickOrdersForTests(0.1f);
            Require(Mathf.Approximately(enemy.Health, healthBefore), "Projectile delays damage", "Target health should not change until the shot reaches it.");

            RtsProjectile projectile = FindProjectile();
            Require(projectile != null, "Projectile spawned", "Firing should create a moving projectile object.");
            for (int i = 0; i < 20 && enemy.Health >= healthBefore - 0.001f; i++)
            {
                projectile.TickProjectileForTests(0.08f);
            }

            Require(enemy.Health < healthBefore, "Projectile impact damage", "Target should take damage when the projectile arrives.");
        }

        private static void ValidateTankTurretDelayAndMobileFire(RtsGame game)
        {
            ClearProjectiles();
            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-42f, 0f, -42f));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, tank.transform.position + tank.transform.right * 7f);
            float healthBefore = enemy.Health;

            tank.IssueAttack(enemy);
            tank.TickOrdersForTests(0.05f);
            Require(FindProjectile() == null, "Tank turret aim delay", "Tank should wait briefly for the turret to slew before firing.");
            Require(Mathf.Approximately(enemy.Health, healthBefore), "No instant tank hit", "Tank damage should also wait for projectile impact.");

            for (int i = 0; i < 12 && FindProjectile() == null; i++)
            {
                tank.TickOrdersForTests(0.1f);
            }

            Require(FindProjectile() != null, "Tank shell spawned", "Tank should fire after the turret faces the target.");

            Vector3 beforeMove = tank.transform.position;
            tank.IssueMove(beforeMove - tank.transform.forward * 4f);
            tank.TickOrdersForTests(0.2f);
            Require((tank.transform.position - beforeMove).sqrMagnitude > 0.01f, "Tank moves while targeting", "Tank should be able to drive while keeping its turret target.");
            Require(tank.CurrentAttackTargetForVisuals == enemy, "Tank keeps turret target", "Move orders should not immediately clear a tank's active target.");
        }

        private static void ValidateUnitBlocking(RtsGame game)
        {
            RtsUnit blocker = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-30.8f, 0f, -34f));
            RtsUnit mover = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-32f, 0f, -34f));
            float minimumDistance = mover.BlockingRadius + blocker.BlockingRadius - 0.04f;

            mover.IssueMove(new Vector3(-26f, 0f, -34f));
            for (int i = 0; i < 12; i++)
            {
                mover.TickOrdersForTests(0.1f);
                Require(PlanarDistance(mover.transform.position, blocker.transform.position) >= minimumDistance, "Unit blocking spacing", "Moving units should maintain body spacing instead of overlapping another unit.");
            }
        }

        private static void ValidateEnemyOpeningGrace(RtsGame game)
        {
            EnemyDirector director = game.EnemyDirector;
            Require(director != null, "Enemy director present", "Skirmish should have an enemy director.");
            game.CreateStructure(RtsTeam.Enemy, StructureKind.Barracks, new Vector3(74f, 0f, 64f));
            director.SetEconomyForTests(10000, 0f, 0f, 0f, 0f, 0f, 0);
            Require(director.TryProduceUnitForTests(), "Enemy can produce during buildup", "Enemy should be able to build up its army.");

            RtsUnit produced = FindNewestEnemyUnit(game);
            Require(produced != null, "Enemy produced unit", "Enemy production should create a unit.");
            RtsUnitOrderSaveData order = produced.CaptureOrderState();
            Require(order.orderType == "Move", "Opening grace holds attacks", "Enemy-produced units should exit but not attack before the grace window ends.");
        }

        private static RtsProjectile FindProjectile()
        {
            RtsProjectile[] projectiles = Object.FindObjectsOfType<RtsProjectile>();
            return projectiles.Length > 0 ? projectiles[0] : null;
        }

        private static void ClearProjectiles()
        {
            RtsProjectile[] projectiles = Object.FindObjectsOfType<RtsProjectile>();
            for (int i = projectiles.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(projectiles[i].gameObject);
            }
        }

        private static ProductionStructure FindProducer(RtsGame game, RtsTeam team, StructureKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == team && producer.StructureKind == kind)
                {
                    return producer;
                }
            }

            throw new InvalidOperationException("Missing producer " + team + " " + kind + ".");
        }

        private static RtsUnit FindNewestEnemyUnit(RtsGame game)
        {
            for (int i = game.Entities.Count - 1; i >= 0; i--)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Enemy)
                {
                    return unit;
                }
            }

            return null;
        }

        private static int CountStructures(RtsGame game, RtsTeam team, StructureKind kind)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == team && structure.StructureKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountUnits(RtsGame game, RtsTeam team)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == team)
                {
                    count++;
                }
            }

            return count;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static void Require(bool condition, string label, string detail)
        {
            if (!condition)
            {
                throw new InvalidOperationException("[Command RTS Combat] FAIL - " + label + ": " + detail);
            }

            Debug.Log("[Command RTS Combat] PASS - " + label + ": " + detail);
        }
    }
}
#endif
