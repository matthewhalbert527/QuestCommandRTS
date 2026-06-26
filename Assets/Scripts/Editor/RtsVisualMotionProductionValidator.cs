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
                ValidateContrastDetailing(game);
                ValidateProductionExit(game);
                Debug.Log("[Command RTS Visuals] PASS - Visual motion rigs, harvester harvesting motion, palette tinting, contrast detailing, and production exits validated.");
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
            Require(harvesterAnimator.HasTrackRigForTests && harvesterAnimator.HasHarvestRigForTests && harvesterAnimator.HasCargoFillRigForTests && !harvesterAnimator.HasTurretRigForTests, "Harvester motion rig", "Harvesters should use treads, a harvesting rig, and a visible cargo-fill rig without a turret rig.");
            Require(!harvesterAnimator.HasRoundWheelRigForTests && harvester.transform.Find("Roll Wheel LF") == null, "Harvester tread rig", "Harvesters should not use the old rotating round wheel primitives.");
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

            HarvesterUnit harvester = game.CreateUnit(RtsTeam.Player, UnitKind.Harvester, new Vector3(-46f, 0f, -56f)) as HarvesterUnit;
            RtsUnitVisualAnimator harvesterAnimator = RequireAnimator(harvester, "Harvester harvest animation");
            Transform harvesterTrack = harvesterAnimator.FirstTrackPadForTests;
            Require(harvesterTrack != null, "Harvester track part", "Harvester should expose animated tread pads.");
            Vector3 harvesterTrackStart = harvesterTrack.localPosition;
            harvester.transform.position += harvester.transform.forward * 1.1f;
            harvesterAnimator.TickVisualsForTests(0.2f);
            Require((harvesterTrack.localPosition - harvesterTrackStart).sqrMagnitude > 0.0001f, "Harvester track animation", "Harvester tread pads should move when the unit rolls.");

            Transform harvestPart = harvesterAnimator.FirstHarvestPartForTests;
            Require(harvestPart != null, "Harvester harvest part", "Harvester should expose a visible harvest motion part.");
            Quaternion harvestStartRotation = harvestPart.localRotation;
            Vector3 harvestStartPosition = harvestPart.localPosition;
            RefineryStructure refinery = game.CreateStructure(RtsTeam.Player, StructureKind.Refinery, new Vector3(-42f, 0f, -56f)) as RefineryStructure;
            Require(game.ResourceNodes.Count > 0 && refinery != null, "Harvest animation setup", "A resource and refinery should be available for harvest animation validation.");
            harvester.SetHarvestingForVisualsForTests(game.ResourceNodes[0], refinery);
            harvesterAnimator.TickVisualsForTests(0.25f);
            Require(
                Quaternion.Angle(harvestStartRotation, harvestPart.localRotation) > 1f || (harvestPart.localPosition - harvestStartPosition).sqrMagnitude > 0.0001f,
                "Harvester active harvesting motion",
                "Harvester collector/intake parts should animate while actively harvesting.");

            Transform cargoFill = harvesterAnimator.FirstCargoFillPartForTests;
            Require(cargoFill != null, "Harvester cargo fill part", "Harvester should expose an ore load that can grow as cargo is gathered.");
            float emptyFillScale = cargoFill.localScale.y;
            harvester.SetCargoForTests(harvester.CargoCapacity / 2);
            harvesterAnimator.TickVisualsForTests(0.25f);
            Require(harvester.CargoFillForTests > 0.45f && cargoFill.gameObject.activeSelf && cargoFill.localScale.y > emptyFillScale + 0.05f, "Harvester cargo fills", "The harvester ore load should visibly rise as cargo is gathered.");
            harvester.SetCargoForTests(0);
            harvesterAnimator.TickVisualsForTests(0.25f);
            Require(!cargoFill.gameObject.activeSelf || cargoFill.localScale.y < emptyFillScale + 0.02f, "Harvester cargo empties", "The harvester ore load should hide again after dumping at a refinery.");
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

        private static void ValidateContrastDetailing(RtsGame game)
        {
            RtsUnit rifleman = game.CreateUnit(RtsTeam.Player, UnitKind.Rifleman, new Vector3(-54f, 0f, -62f));
            RtsUnit tank = game.CreateUnit(RtsTeam.Player, UnitKind.MediumTank, new Vector3(-50f, 0f, -62f));
            RtsUnit harvester = game.CreateUnit(RtsTeam.Player, UnitKind.Harvester, new Vector3(-46f, 0f, -62f));
            RtsStructure command = game.CreateStructure(RtsTeam.Player, StructureKind.CommandCenter, new Vector3(-62f, 0f, -62f));

            Require(rifleman.transform.Find("Infantry Left Shoulder Highlight") != null, "Infantry highlights", "Infantry should have bright shoulder edge highlights.");
            Require(rifleman.transform.Find("Infantry Left Boot Shadow") != null, "Infantry shadows", "Infantry should have dark boot shadows for leg separation.");
            Require(tank.transform.Find("Tank Front Edge Highlight") != null, "Tank edge highlights", "Tanks should have visible hull edge highlights.");
            Require(tank.transform.Find("Tank Rear Recess Shadow") != null, "Tank shadow panels", "Tanks should have dark recessed panels.");
            Require(harvester.transform.Find("Harvester Caution Intake Strip") != null, "Harvester contrast strip", "Harvesters should have caution-color detail contrast.");
            Require(command.transform.Find("Structure Base Shadow Plinth") != null, "Structure base contrast", "Structures should have dark base plinths.");
            Require(command.transform.Find("Structure Roof Lip Highlight") != null, "Structure roof highlight", "Structures should have bright roof lip highlights.");
            Require(command.transform.Find("Structure Caution Threshold") != null, "Structure caution detail", "Major structures should have caution accent strips.");
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
