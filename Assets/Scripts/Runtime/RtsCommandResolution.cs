using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    public enum RtsCommandKind
    {
        Invalid,
        Select,
        AddToSelection,
        Move,
        AttackMove,
        Attack,
        Harvest,
        Repair,
        SetRallyPoint,
        Stop,
        Board,
        Guard
    }

    public enum RtsCommandTargetKind
    {
        None,
        Terrain,
        Entity,
        Resource
    }

    public struct RtsCommandTarget
    {
        public RtsCommandTargetKind Kind { get; private set; }
        public RtsEntity Entity { get; private set; }
        public ResourceNode Resource { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public Vector2Int Cell { get; private set; }
        public bool HasCell { get; private set; }
        public bool IsVisible { get; private set; }
        public bool IsInsideMap { get; private set; }

        public static RtsCommandTarget None(Vector3 worldPosition)
        {
            return new RtsCommandTarget
            {
                Kind = RtsCommandTargetKind.None,
                WorldPosition = worldPosition,
                IsVisible = true,
                IsInsideMap = true
            };
        }

        public static RtsCommandTarget Terrain(Vector3 worldPosition, bool isVisible, bool isInsideMap)
        {
            return new RtsCommandTarget
            {
                Kind = RtsCommandTargetKind.Terrain,
                WorldPosition = worldPosition,
                IsVisible = isVisible,
                IsInsideMap = isInsideMap
            };
        }

        public static RtsCommandTarget EntityTarget(RtsEntity entity, Vector3 worldPosition, bool isVisible, bool isInsideMap)
        {
            return new RtsCommandTarget
            {
                Kind = RtsCommandTargetKind.Entity,
                Entity = entity,
                WorldPosition = worldPosition,
                IsVisible = isVisible,
                IsInsideMap = isInsideMap
            };
        }

        public static RtsCommandTarget ResourceTarget(ResourceNode resource, Vector3 worldPosition, bool isVisible, bool isInsideMap)
        {
            return new RtsCommandTarget
            {
                Kind = RtsCommandTargetKind.Resource,
                Resource = resource,
                WorldPosition = worldPosition,
                IsVisible = isVisible,
                IsInsideMap = isInsideMap
            };
        }

        public RtsCommandTarget WithCell(Vector2Int cell)
        {
            RtsCommandTarget target = this;
            target.Cell = cell;
            target.HasCell = true;
            return target;
        }
    }

    public struct RtsCommandRequest
    {
        public IReadOnlyList<RtsEntity> Selection { get; private set; }
        public RtsCommandTarget Target { get; private set; }
        public RtsTeam IssuingTeam { get; private set; }
        public bool AdditiveSelection { get; private set; }
        public bool AttackMoveModifier { get; private set; }
        public bool GuardModifier { get; private set; }
        public bool StopRequested { get; private set; }
        public bool AcceptsCommands { get; private set; }
        public bool AllowTargetHintsWithoutCapability { get; private set; }

        public RtsCommandRequest(
            IReadOnlyList<RtsEntity> selection,
            RtsCommandTarget target,
            RtsTeam issuingTeam,
            bool additiveSelection,
            bool attackMoveModifier,
            bool guardModifier,
            bool stopRequested,
            bool acceptsCommands,
            bool allowTargetHintsWithoutCapability)
        {
            Selection = selection;
            Target = target;
            IssuingTeam = issuingTeam;
            AdditiveSelection = additiveSelection;
            AttackMoveModifier = attackMoveModifier;
            GuardModifier = guardModifier;
            StopRequested = stopRequested;
            AcceptsCommands = acceptsCommands;
            AllowTargetHintsWithoutCapability = allowTargetHintsWithoutCapability;
        }
    }

    public struct RtsCommandResolution
    {
        public RtsCommandKind Kind { get; private set; }
        public RtsContextCommandKind ContextKind { get; private set; }
        public bool IsValid { get; private set; }
        public string Reason { get; private set; }

        public static RtsCommandResolution Valid(RtsCommandKind kind)
        {
            return new RtsCommandResolution
            {
                Kind = kind,
                ContextKind = ToContextKind(kind),
                IsValid = true,
                Reason = string.Empty
            };
        }

        public static RtsCommandResolution Invalid(string reason)
        {
            return new RtsCommandResolution
            {
                Kind = RtsCommandKind.Invalid,
                ContextKind = RtsContextCommandKind.None,
                IsValid = false,
                Reason = string.IsNullOrEmpty(reason) ? "Invalid command." : reason
            };
        }

        private static RtsContextCommandKind ToContextKind(RtsCommandKind kind)
        {
            switch (kind)
            {
                case RtsCommandKind.Attack:
                    return RtsContextCommandKind.Attack;
                case RtsCommandKind.Harvest:
                    return RtsContextCommandKind.Harvest;
                case RtsCommandKind.Repair:
                    return RtsContextCommandKind.Repair;
                case RtsCommandKind.Board:
                    return RtsContextCommandKind.Board;
                case RtsCommandKind.SetRallyPoint:
                    return RtsContextCommandKind.Rally;
                case RtsCommandKind.Move:
                    return RtsContextCommandKind.Move;
                default:
                    return RtsContextCommandKind.None;
            }
        }
    }

    public sealed class RtsContextCommandResolver
    {
        public RtsCommandResolution Resolve(RtsCommandRequest request)
        {
            if (!request.AcceptsCommands)
            {
                return RtsCommandResolution.Invalid("Commands are blocked.");
            }

            if (!request.Target.IsInsideMap)
            {
                return RtsCommandResolution.Invalid("Target outside map.");
            }

            SelectionSummary selection = SummarizeSelection(request);

            if (request.StopRequested)
            {
                return selection.MobileUnitCount > 0
                    ? RtsCommandResolution.Valid(RtsCommandKind.Stop)
                    : RtsCommandResolution.Invalid("No controllable unit selected.");
            }

            if (request.Target.Kind != RtsCommandTargetKind.Terrain && !request.Target.IsVisible)
            {
                return RtsCommandResolution.Invalid("Target is fog-hidden.");
            }

            if (!selection.HasPlayerSelection)
            {
                return ResolveWithoutSelection(request);
            }

            if (request.GuardModifier)
            {
                return selection.MobileUnitCount > 0
                    ? RtsCommandResolution.Valid(RtsCommandKind.Guard)
                    : RtsCommandResolution.Invalid("No controllable unit selected.");
            }

            if (request.AttackMoveModifier)
            {
                return request.Target.Kind == RtsCommandTargetKind.Terrain && selection.MobileUnitCount > 0
                    ? RtsCommandResolution.Valid(RtsCommandKind.AttackMove)
                    : RtsCommandResolution.Invalid("Unit cannot attack-move.");
            }

            RtsEntity targetEntity = request.Target.Entity;
            if (request.Target.Kind == RtsCommandTargetKind.Entity && targetEntity != null && targetEntity.Team != request.IssuingTeam)
            {
                if (selection.CombatUnitCount > 0 || request.AllowTargetHintsWithoutCapability)
                {
                    return RtsCommandResolution.Valid(RtsCommandKind.Attack);
                }

                return RtsCommandResolution.Invalid("Unit cannot attack.");
            }

            if (request.Target.Kind == RtsCommandTargetKind.Resource && request.Target.Resource != null)
            {
                if (selection.HarvesterCount > 0 || request.AllowTargetHintsWithoutCapability)
                {
                    return RtsCommandResolution.Valid(RtsCommandKind.Harvest);
                }

                return RtsCommandResolution.Invalid("Unit cannot harvest.");
            }

            if (CanRepairTarget(request, targetEntity))
            {
                return RtsCommandResolution.Valid(RtsCommandKind.Repair);
            }

            if (CanBoardTarget(request, targetEntity))
            {
                return RtsCommandResolution.Valid(RtsCommandKind.Board);
            }

            if (request.Target.Kind == RtsCommandTargetKind.Terrain && selection.ProductionStructureCount > 0 && selection.MobileUnitCount == 0)
            {
                return RtsCommandResolution.Valid(RtsCommandKind.SetRallyPoint);
            }

            if (request.Target.Kind == RtsCommandTargetKind.Terrain || request.Target.Kind == RtsCommandTargetKind.None)
            {
                if (selection.MobileUnitCount > 0 || request.AllowTargetHintsWithoutCapability)
                {
                    return RtsCommandResolution.Valid(RtsCommandKind.Move);
                }
            }

            return RtsCommandResolution.Invalid("No valid command for target.");
        }

        private static RtsCommandResolution ResolveWithoutSelection(RtsCommandRequest request)
        {
            RtsEntity entity = request.Target.Entity;
            if (!request.AllowTargetHintsWithoutCapability && entity != null && entity.Team == request.IssuingTeam && entity.IsAlive)
            {
                return RtsCommandResolution.Valid(request.AdditiveSelection ? RtsCommandKind.AddToSelection : RtsCommandKind.Select);
            }

            if (request.AllowTargetHintsWithoutCapability)
            {
                if (request.Target.Kind == RtsCommandTargetKind.Entity && entity != null && entity.Team != request.IssuingTeam)
                {
                    return RtsCommandResolution.Valid(RtsCommandKind.Attack);
                }

                if (request.Target.Kind == RtsCommandTargetKind.Entity && entity != null && entity.Team == request.IssuingTeam)
                {
                    return RtsCommandResolution.Valid(RtsCommandKind.Move);
                }

                if (request.Target.Kind == RtsCommandTargetKind.Resource && request.Target.Resource != null)
                {
                    return RtsCommandResolution.Valid(RtsCommandKind.Harvest);
                }

                if (request.Target.Kind == RtsCommandTargetKind.Terrain || request.Target.Kind == RtsCommandTargetKind.None)
                {
                    return RtsCommandResolution.Valid(RtsCommandKind.Move);
                }
            }

            return RtsCommandResolution.Invalid("No selectable friendly unit selected.");
        }

        private static bool CanRepairTarget(RtsCommandRequest request, RtsEntity target)
        {
            if (target == null || target.Team != request.IssuingTeam || !target.IsAlive || target.Health >= target.MaxHealth - 0.01f)
            {
                return false;
            }

            IReadOnlyList<RtsEntity> selection = request.Selection;
            if (selection == null)
            {
                return false;
            }

            for (int i = 0; i < selection.Count; i++)
            {
                RtsUnit unit = selection[i] as RtsUnit;
                if (unit != null && unit.Team == request.IssuingTeam && unit.IsAlive && unit.CanRepairTarget(target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanBoardTarget(RtsCommandRequest request, RtsEntity target)
        {
            MediumTankUnit mediumTank = target as MediumTankUnit;
            if (mediumTank == null || mediumTank.Team != request.IssuingTeam || !mediumTank.IsAlive)
            {
                return false;
            }

            IReadOnlyList<RtsEntity> selection = request.Selection;
            if (selection == null)
            {
                return false;
            }

            for (int i = 0; i < selection.Count; i++)
            {
                RtsUnit unit = selection[i] as RtsUnit;
                if (unit != null && unit.Team == request.IssuingTeam && unit.IsAlive && unit.CanBoardMediumTank(mediumTank))
                {
                    return true;
                }
            }

            return false;
        }

        private static SelectionSummary SummarizeSelection(RtsCommandRequest request)
        {
            SelectionSummary summary = new SelectionSummary();
            IReadOnlyList<RtsEntity> selection = request.Selection;
            if (selection == null)
            {
                return summary;
            }

            for (int i = 0; i < selection.Count; i++)
            {
                RtsEntity entity = selection[i];
                if (entity == null || entity.Team != request.IssuingTeam || !entity.IsAlive)
                {
                    continue;
                }

                summary.HasPlayerSelection = true;

                RtsUnit unit = entity as RtsUnit;
                if (unit != null)
                {
                    summary.MobileUnitCount++;
                    if (unit.Damage > 0f)
                    {
                        summary.CombatUnitCount++;
                    }

                    if (unit is HarvesterUnit)
                    {
                        summary.HarvesterCount++;
                    }

                    continue;
                }

                if (entity is ProductionStructure)
                {
                    summary.ProductionStructureCount++;
                }
            }

            return summary;
        }

        private struct SelectionSummary
        {
            public bool HasPlayerSelection;
            public int MobileUnitCount;
            public int CombatUnitCount;
            public int HarvesterCount;
            public int ProductionStructureCount;
        }
    }
}
