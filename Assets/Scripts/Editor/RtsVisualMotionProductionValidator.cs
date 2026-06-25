#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestCommandRTS.Editor
{
    public static class RtsVisualMotionProductionValidator
    {
        [MenuItem("Command RTS/Validate Visual Motion And Production")]
        public static void ValidateVisualMotionAndProduction()
        {
            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.Desktop);
            GameObject root = new GameObject("Visual Motion Production Validation");
            try
            {
                RtsGame game = root.AddComponent<RtsGame>();
                game.Initialize();
                Physics.SyncTransforms();

                ValidateUnitMotionRigs(game);
                ValidateVisualAnimation(game);
                ValidatePaletteTinting(game);
                ValidateProductionExit(game);
                Debug.Log("[Command RTS Visuals] PASS - Visual motion rigs, palette tinting, and production exits validated.");
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

        private static void ValidateUnitMotionRigs(RtsGame game)
        {
            RtsUnit rifleman = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-54f, 0f, -52f));
            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-50f, 0f, -52f));
            RtsUnit harvester = game.CreateUnit(RtsTeam.Player, UnitKind.Harvester, new Vector3(-46f, 0f, -52f));

            RtsUnitVisualAnimator infantryAnimator = RequireAnimator(rifleman, "Infantry animator");
            RtsUnitVisualAnimator tankAnimator = RequireAnimator(tank, "Tank animator");
            RtsUnitVisualAnimator harvesterAnimator = RequireAnimator(harvester, "Harvester animator");

            Require(infantryAnimator.HasLegRigForTests, "Infantry leg rig", "Infantry should have procedural walk legs.");
            Require(tankAnimator.HasTrackRigForTests && tankAnimator.HasTurretRigForTests, "Tank rig", "Tanks should have procedural tracks and turret.");
            Require(harvesterAnimator.HasWheelRigForTests && !harvesterAnimator.HasTurretRigForTests, "Harvester wheel rig", "Harvesters should roll without a turret rig.");
        }

        private static void ValidateVisualAnimation(RtsGame game)
        {
            RtsUnit rifleman = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-54f, 0f, -56f));
            RtsUnitVisualAnimator infantryAnimator = RequireAnimator(rifleman, "Infantry animation");
            Transform leg = infantryAnimator.FirstLegForTests;
            Quaternion legStart = leg.localRotation;
            rifleman.transform.position += rifleman.transform.forward * 1.2f;
            infantryAnimator.TickVisualsForTests(0.2f);
            Require(Quaternion.Angle(legStart, leg.localRotation) > 1f, "Infantry leg animation", "Legs should swing when the unit moves.");

            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-50f, 0f, -56f));
            RtsUnit enemy = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, tank.transform.position + tank.transform.right * 6f);
            RtsUnitVisualAnimator tankAnimator = RequireAnimator(tank, "Tank animation");
            Transform track = tankAnimator.FirstTrackPadForTests;
            Transform turret = tankAnimator.TurretPivotForTests;
            Vector3 trackStart = track.localPosition;
            Quaternion turretStart = turret.localRotation;
            tank.transform.position += tank.transform.forward * 1.5f;
            tank.IssueAttack(enemy);
            tankAnimator.TickVisualsForTests(0.2f);
            Require((track.localPosition - trackStart).sqrMagnitude > 0.0001f, "Track animation", "Tank treads should move when the unit moves.");
            Require(Quaternion.Angle(turretStart, turret.localRotation) > 1f, "Turret animation", "Tank turret should yaw toward an attack target.");
        }

        private static void ValidatePaletteTinting(RtsGame game)
        {
            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.LightTank, new Vector3(-54f, 0f, -60f));
            Transform model = tank.transform.Find("Light Tank Model");
            Transform teamPlate = tank.transform.Find("Tank Team Roof Plate");
            Require(model != null, "Imported tank model", "Light tank model should be present.");
            Require(teamPlate != null, "Tank team plate", "Team plate should be present.");

            Renderer modelRenderer = model.GetComponentInChildren<Renderer>();
            Renderer teamRenderer = teamPlate.GetComponent<Renderer>();
            Require(modelRenderer != null, "Imported model renderer", "Imported model should have a renderer.");
            Require(teamRenderer != null, "Team plate renderer", "Team plate should have a renderer.");
            Require(modelRenderer.GetComponent<RtsTeamTintTarget>() == null, "Palette preserved", "Imported model renderers should not be team-tint targets.");
            Require(teamRenderer.GetComponent<RtsTeamTintTarget>() != null, "Team tint target", "Only explicit recognition plates should be team-tint targets.");
        }

        private static void ValidateProductionExit(RtsGame game)
        {
            ProductionStructure barracks = game.CreateStructure(RtsTeam.Player, StructureKind.Barracks, new Vector3(-76f, 0f, -62f)) as ProductionStructure;
            RtsUnit produced = barracks.SpawnProducedUnit(UnitKind.Rifleman, null);
            Require(produced != null, "Produced unit", "Producer should spawn a unit.");

            Vector3 spawnOffset = produced.transform.position - barracks.transform.position;
            spawnOffset.y = 0f;
            Require(spawnOffset.magnitude < barracks.FootprintRadius, "Production spawn interior", "Produced unit should start inside the producer footprint.");

            RtsUnitOrderSaveData order = produced.CaptureOrderState();
            Require(order.orderType == "Move", "Production exit order", "Produced unit should receive an immediate exit move order.");
            Vector3 exitOffset = order.destination.ToVector3() - barracks.transform.position;
            exitOffset.y = 0f;
            Require(exitOffset.magnitude > barracks.FootprintRadius + 1.5f, "Production exit destination", "Exit destination should be outside the producer footprint.");
        }

        private static RtsUnitVisualAnimator RequireAnimator(RtsUnit unit, string label)
        {
            RtsUnitVisualAnimator animator = unit != null ? unit.GetComponent<RtsUnitVisualAnimator>() : null;
            Require(animator != null, label, "Unit should have an RtsUnitVisualAnimator.");
            return animator;
        }

        private static ProductionStructure FindPlayerProducer(RtsGame game, StructureKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.StructureKind == kind)
                {
                    return producer;
                }
            }

            throw new InvalidOperationException("Missing player producer " + kind + ".");
        }

        private static void Require(bool condition, string label, string detail)
        {
            if (!condition)
            {
                throw new InvalidOperationException("[Command RTS Visuals] FAIL - " + label + ": " + detail);
            }

            Debug.Log("[Command RTS Visuals] PASS - " + label + ": " + detail);
        }
    }
}
#endif
