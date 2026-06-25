using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    [Serializable]
    public sealed class RtsRuntimeDiagnosticsSnapshot
    {
        public string capturedAtUtc = string.Empty;
        public string runtimeMode = string.Empty;
        public string matchState = string.Empty;
        public string statusMessage = string.Empty;
        public float matchTime;
        public bool acceptsSystemInput;
        public bool acceptsPlayerInput;
        public bool isPaused;
        public bool isSavingOrLoading;
        public int entityCount;
        public int aliveEntityCount;
        public int selectedEntityCount;
        public int playerEntityCount;
        public int enemyEntityCount;
        public int neutralEntityCount;
        public int unitCount;
        public int structureCount;
        public RtsDiagnosticsResourceSnapshot resources = new RtsDiagnosticsResourceSnapshot();
        public RtsDiagnosticsProductionSnapshot production = new RtsDiagnosticsProductionSnapshot();
        public RtsDiagnosticsBuildPlacementSnapshot buildPlacement = new RtsDiagnosticsBuildPlacementSnapshot();
        public RtsDiagnosticsTabletopSnapshot tabletop = new RtsDiagnosticsTabletopSnapshot();
        public RtsFogCoverageSnapshot fog = new RtsFogCoverageSnapshot();
        public int saveSlotCount;
        public string[] saveSlots = new string[0];
        public string saveSlotError = string.Empty;
        public List<RtsDiagnosticsTeamSnapshot> teams = new List<RtsDiagnosticsTeamSnapshot>();
        public List<RtsDiagnosticsUnitKindSnapshot> unitKinds = new List<RtsDiagnosticsUnitKindSnapshot>();
        public List<RtsDiagnosticsStructureKindSnapshot> structureKinds = new List<RtsDiagnosticsStructureKindSnapshot>();

        public static RtsRuntimeDiagnosticsSnapshot Capture(RtsGame game)
        {
            RtsRuntimeDiagnosticsSnapshot snapshot = new RtsRuntimeDiagnosticsSnapshot
            {
                capturedAtUtc = DateTime.UtcNow.ToString("o")
            };

            if (game == null)
            {
                snapshot.runtimeMode = "None";
                snapshot.matchState = "None";
                return snapshot;
            }

            snapshot.runtimeMode = game.RuntimeMode.ToString();
            snapshot.matchState = game.MatchState.ToString();
            snapshot.statusMessage = game.StatusMessage;
            snapshot.matchTime = game.MatchTime;
            snapshot.acceptsSystemInput = game.AcceptsSystemInput;
            snapshot.acceptsPlayerInput = game.AcceptsPlayerInput;
            snapshot.isPaused = game.Clock != null && game.Clock.IsPaused;
            snapshot.isSavingOrLoading = game.Lifecycle != null && game.Lifecycle.IsSavingOrLoading;
            snapshot.selectedEntityCount = game.Selection != null ? game.Selection.Count : 0;

            CaptureResources(snapshot, game);
            CaptureEntities(snapshot, game);
            CaptureProduction(snapshot, game);
            CaptureBuildPlacement(snapshot, game);
            CaptureTabletop(snapshot, game);
            CaptureFog(snapshot, game);
            CaptureSaveSlots(snapshot, game);

            return snapshot;
        }

        public string ToJson(bool prettyPrint)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        private static void CaptureResources(RtsRuntimeDiagnosticsSnapshot snapshot, RtsGame game)
        {
            if (game.Resources != null)
            {
                snapshot.resources.credits = game.Resources.Credits;
                snapshot.resources.powerProvided = game.Resources.PowerProvided;
                snapshot.resources.powerUsed = game.Resources.PowerUsed;
                snapshot.resources.lowPower = game.Resources.HasLowPower;
            }

            if (game.ResourceNodes == null)
            {
                return;
            }

            snapshot.resources.nodeCount = game.ResourceNodes.Count;
            for (int i = 0; i < game.ResourceNodes.Count; i++)
            {
                ResourceNode node = game.ResourceNodes[i];
                if (node == null)
                {
                    continue;
                }

                snapshot.resources.remainingAmount += Mathf.Max(0, node.Amount);
                snapshot.resources.maxAmount += Mathf.Max(0, node.MaxAmount);
                if (node.IsDepleted)
                {
                    snapshot.resources.depletedNodeCount++;
                }
            }
        }

        private static void CaptureEntities(RtsRuntimeDiagnosticsSnapshot snapshot, RtsGame game)
        {
            if (game.Entities == null)
            {
                return;
            }

            snapshot.entityCount = game.Entities.Count;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsEntity entity = game.Entities[i];
                if (entity == null)
                {
                    continue;
                }

                if (entity.IsAlive)
                {
                    snapshot.aliveEntityCount++;
                    IncrementTeamCount(snapshot, entity.Team);

                    if (entity is RtsUnit)
                    {
                        snapshot.unitCount++;
                        IncrementUnitKind(snapshot, (RtsUnit)entity);
                    }

                    if (entity is RtsStructure)
                    {
                        snapshot.structureCount++;
                        IncrementStructureKind(snapshot, (RtsStructure)entity);
                    }
                }

                IncrementTeamDetail(snapshot, entity);
            }
        }

        private static void CaptureProduction(RtsRuntimeDiagnosticsSnapshot snapshot, RtsGame game)
        {
            if (game.Entities == null)
            {
                return;
            }

            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer == null || !producer.IsAlive)
                {
                    continue;
                }

                snapshot.production.producerCount++;
                snapshot.production.totalQueueItems += producer.QueueCount;
                snapshot.production.pendingQueueItems += producer.PendingQueueCount;

                if (producer.HasActiveProduction)
                {
                    snapshot.production.activeProducerCount++;
                    snapshot.production.activeItems++;
                }
            }
        }

        private static void CaptureBuildPlacement(RtsRuntimeDiagnosticsSnapshot snapshot, RtsGame game)
        {
            BuildManager buildManager = game.BuildManager;
            if (buildManager == null)
            {
                return;
            }

            snapshot.buildPlacement.active = buildManager.IsPlacing;
            snapshot.buildPlacement.kind = buildManager.IsPlacing ? buildManager.PendingKind.ToString() : string.Empty;
            snapshot.buildPlacement.valid = buildManager.PlacementValid;
            snapshot.buildPlacement.hasPoint = buildManager.HasPlacementPoint;
            snapshot.buildPlacement.failureReason = buildManager.LastFailureReason.ToString();
            snapshot.buildPlacement.x = buildManager.PlacementPoint.x;
            snapshot.buildPlacement.z = buildManager.PlacementPoint.z;
        }

        private static void CaptureTabletop(RtsRuntimeDiagnosticsSnapshot snapshot, RtsGame game)
        {
            snapshot.tabletop.mapHalfSize = RtsBalance.MapHalfSize;
            snapshot.tabletop.simulationWidth = RtsBalance.MapHalfSize * 2f;

            QuestTabletopSettings settings = game.GetComponent<QuestTabletopSettings>();
            if (settings == null)
            {
                return;
            }

            snapshot.tabletop.simulationUnitsPerMeter = settings.SimulationUnitsPerMeter;
            snapshot.tabletop.battlefieldWidthMeters = settings.BattlefieldWidthMeters;
            snapshot.tabletop.boardHeightMeters = settings.BoardHeightMeters;
        }

        private static void CaptureFog(RtsRuntimeDiagnosticsSnapshot snapshot, RtsGame game)
        {
            if (game.FogOfWar != null)
            {
                snapshot.fog = game.FogOfWar.CaptureCoverageSnapshot();
            }
        }

        private static void CaptureSaveSlots(RtsRuntimeDiagnosticsSnapshot snapshot, RtsGame game)
        {
            if (game.SaveService == null)
            {
                return;
            }

            try
            {
                List<string> slots = game.SaveService.ListSlots();
                snapshot.saveSlotCount = slots.Count;
                snapshot.saveSlots = slots.ToArray();
            }
            catch (Exception exception)
            {
                snapshot.saveSlotError = exception.Message;
            }
        }

        private static void IncrementTeamCount(RtsRuntimeDiagnosticsSnapshot snapshot, RtsTeam team)
        {
            switch (team)
            {
                case RtsTeam.Player:
                    snapshot.playerEntityCount++;
                    break;
                case RtsTeam.Enemy:
                    snapshot.enemyEntityCount++;
                    break;
                default:
                    snapshot.neutralEntityCount++;
                    break;
            }
        }

        private static void IncrementTeamDetail(RtsRuntimeDiagnosticsSnapshot snapshot, RtsEntity entity)
        {
            RtsDiagnosticsTeamSnapshot team = GetTeam(snapshot, entity.Team);
            team.entities++;

            if (entity.IsAlive)
            {
                team.aliveEntities++;
            }

            if (entity.IsSelected)
            {
                team.selectedEntities++;
            }

            RtsUnit unit = entity as RtsUnit;
            if (unit != null)
            {
                team.units++;
                if (entity.IsAlive && unit.IsIdle())
                {
                    team.idleUnits++;
                }
            }

            if (entity is RtsStructure)
            {
                team.structures++;
            }
        }

        private static void IncrementUnitKind(RtsRuntimeDiagnosticsSnapshot snapshot, RtsUnit unit)
        {
            RtsDiagnosticsUnitKindSnapshot kind = GetUnitKind(snapshot, unit.UnitKind);
            IncrementByTeam(kind, unit.Team);
        }

        private static void IncrementStructureKind(RtsRuntimeDiagnosticsSnapshot snapshot, RtsStructure structure)
        {
            RtsDiagnosticsStructureKindSnapshot kind = GetStructureKind(snapshot, structure.StructureKind);
            IncrementByTeam(kind, structure.Team);
        }

        private static RtsDiagnosticsTeamSnapshot GetTeam(RtsRuntimeDiagnosticsSnapshot snapshot, RtsTeam team)
        {
            string label = team.ToString();
            for (int i = 0; i < snapshot.teams.Count; i++)
            {
                if (snapshot.teams[i].team == label)
                {
                    return snapshot.teams[i];
                }
            }

            RtsDiagnosticsTeamSnapshot item = new RtsDiagnosticsTeamSnapshot { team = label };
            snapshot.teams.Add(item);
            return item;
        }

        private static RtsDiagnosticsUnitKindSnapshot GetUnitKind(RtsRuntimeDiagnosticsSnapshot snapshot, UnitKind kind)
        {
            string label = kind.ToString();
            for (int i = 0; i < snapshot.unitKinds.Count; i++)
            {
                if (snapshot.unitKinds[i].kind == label)
                {
                    return snapshot.unitKinds[i];
                }
            }

            RtsDiagnosticsUnitKindSnapshot item = new RtsDiagnosticsUnitKindSnapshot { kind = label };
            snapshot.unitKinds.Add(item);
            return item;
        }

        private static RtsDiagnosticsStructureKindSnapshot GetStructureKind(RtsRuntimeDiagnosticsSnapshot snapshot, StructureKind kind)
        {
            string label = kind.ToString();
            for (int i = 0; i < snapshot.structureKinds.Count; i++)
            {
                if (snapshot.structureKinds[i].kind == label)
                {
                    return snapshot.structureKinds[i];
                }
            }

            RtsDiagnosticsStructureKindSnapshot item = new RtsDiagnosticsStructureKindSnapshot { kind = label };
            snapshot.structureKinds.Add(item);
            return item;
        }

        private static void IncrementByTeam(RtsDiagnosticsKindSnapshot kind, RtsTeam team)
        {
            kind.total++;
            switch (team)
            {
                case RtsTeam.Player:
                    kind.player++;
                    break;
                case RtsTeam.Enemy:
                    kind.enemy++;
                    break;
                default:
                    kind.neutral++;
                    break;
            }
        }
    }

    [Serializable]
    public sealed class RtsDiagnosticsResourceSnapshot
    {
        public int credits;
        public int powerProvided;
        public int powerUsed;
        public bool lowPower;
        public int nodeCount;
        public int depletedNodeCount;
        public int remainingAmount;
        public int maxAmount;
    }

    [Serializable]
    public sealed class RtsDiagnosticsProductionSnapshot
    {
        public int producerCount;
        public int activeProducerCount;
        public int activeItems;
        public int pendingQueueItems;
        public int totalQueueItems;
    }

    [Serializable]
    public sealed class RtsDiagnosticsBuildPlacementSnapshot
    {
        public bool active;
        public string kind = string.Empty;
        public bool valid;
        public bool hasPoint;
        public string failureReason = string.Empty;
        public float x;
        public float z;
    }

    [Serializable]
    public sealed class RtsDiagnosticsTabletopSnapshot
    {
        public float mapHalfSize;
        public float simulationWidth;
        public float simulationUnitsPerMeter;
        public float battlefieldWidthMeters;
        public float boardHeightMeters;
    }

    [Serializable]
    public sealed class RtsFogCoverageSnapshot
    {
        public int totalCells;
        public int exploredCells;
        public int visibleCells;
        public float exploredPercent;
        public float visiblePercent;
    }

    [Serializable]
    public sealed class RtsDiagnosticsTeamSnapshot
    {
        public string team = string.Empty;
        public int entities;
        public int aliveEntities;
        public int units;
        public int structures;
        public int selectedEntities;
        public int idleUnits;
    }

    [Serializable]
    public abstract class RtsDiagnosticsKindSnapshot
    {
        public int total;
        public int player;
        public int enemy;
        public int neutral;
    }

    [Serializable]
    public sealed class RtsDiagnosticsUnitKindSnapshot : RtsDiagnosticsKindSnapshot
    {
        public string kind = string.Empty;
    }

    [Serializable]
    public sealed class RtsDiagnosticsStructureKindSnapshot : RtsDiagnosticsKindSnapshot
    {
        public string kind = string.Empty;
    }
}
