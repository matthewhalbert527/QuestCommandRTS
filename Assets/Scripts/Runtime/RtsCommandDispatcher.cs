using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    public enum RtsCommandResult
    {
        None,
        SelectionChanged,
        SelectionCleared,
        MoveIssued,
        AttackIssued,
        HarvestIssued,
        RallyPointSet,
        PlacementUpdated,
        PlacementConfirmed,
        PlacementCanceled
    }

    public enum RtsContextCommandKind
    {
        None,
        Attack,
        Harvest,
        Rally,
        Move
    }

    public sealed class RtsCommandDispatcher
    {
        private readonly List<RtsUnit> commandUnits = new List<RtsUnit>();
        private RtsGame game;

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        public RtsCommandResult ClearSelection()
        {
            if (game == null)
            {
                return RtsCommandResult.None;
            }

            game.ClearSelection();
            return RtsCommandResult.SelectionCleared;
        }

        public RtsCommandResult CancelPlacement()
        {
            if (game == null || game.PlayerCommands == null)
            {
                return RtsCommandResult.None;
            }

            return game.PlayerCommands.CancelConstructionPlacement() ? RtsCommandResult.PlacementCanceled : RtsCommandResult.None;
        }

        public RtsCommandResult CancelPlacementOrClearSelection()
        {
            RtsCommandResult result = CancelPlacement();
            return result == RtsCommandResult.None ? ClearSelection() : result;
        }

        public RtsCommandResult UpdatePlacement(Ray ray)
        {
            if (game == null || game.PlayerCommands == null)
            {
                return RtsCommandResult.None;
            }

            return game.PlayerCommands.UpdateConstructionPlacement(ray) ? RtsCommandResult.PlacementUpdated : RtsCommandResult.None;
        }

        public RtsCommandResult ConfirmPlacement()
        {
            if (game == null || game.PlayerCommands == null)
            {
                return RtsCommandResult.None;
            }

            return game.PlayerCommands.ConfirmConstructionPlacement() ? RtsCommandResult.PlacementConfirmed : RtsCommandResult.None;
        }

        public RtsCommandResult SelectFromRay(Ray ray, bool additive, float maxDistance)
        {
            if (game == null)
            {
                return RtsCommandResult.None;
            }

            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, maxDistance))
            {
                if (!additive)
                {
                    game.ClearSelection();
                    return RtsCommandResult.SelectionCleared;
                }

                return RtsCommandResult.None;
            }

            return SelectFromHit(hit, additive);
        }

        public RtsCommandResult SelectFromHit(RaycastHit hit, bool additive)
        {
            if (game == null || hit.collider == null)
            {
                return RtsCommandResult.None;
            }

            RtsEntity entity = hit.collider.GetComponentInParent<RtsEntity>();
            game.SelectEntity(entity, additive);
            return entity != null && entity.Team == RtsTeam.Player ? RtsCommandResult.SelectionChanged : RtsCommandResult.SelectionCleared;
        }

        public RtsContextCommandKind ResolveContextCommand(RaycastHit hit)
        {
            if (game == null || hit.collider == null)
            {
                return RtsContextCommandKind.None;
            }

            RtsEntity entity = hit.collider.GetComponentInParent<RtsEntity>();
            ResourceNode resource = hit.collider.GetComponentInParent<ResourceNode>();
            return ResolveContextCommand(entity, resource, GetGroundPoint(hit));
        }

        public RtsContextCommandKind ResolveContextCommand(RtsEntity entity, ResourceNode resource, Vector3 point)
        {
            if (game == null)
            {
                return RtsContextCommandKind.None;
            }

            if (entity != null && entity.Team == RtsTeam.Enemy && game.IsEntityVisible(entity))
            {
                return RtsContextCommandKind.Attack;
            }

            if (resource != null)
            {
                return RtsContextCommandKind.Harvest;
            }

            if (CanSetRallyPoint(point))
            {
                return RtsContextCommandKind.Rally;
            }

            return RtsContextCommandKind.Move;
        }

        public RtsCommandResult CommandFromRay(Ray ray, float maxDistance)
        {
            if (game == null)
            {
                return RtsCommandResult.None;
            }

            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, maxDistance))
            {
                return RtsCommandResult.None;
            }

            return CommandFromHit(hit);
        }

        public RtsCommandResult CommandFromHit(RaycastHit hit)
        {
            if (game == null || hit.collider == null)
            {
                return RtsCommandResult.None;
            }

            RtsContextCommandKind command = ResolveContextCommand(hit);
            switch (command)
            {
                case RtsContextCommandKind.Attack:
                    IssueAttack(hit.collider.GetComponentInParent<RtsEntity>());
                    return RtsCommandResult.AttackIssued;
                case RtsContextCommandKind.Harvest:
                    IssueHarvest(hit.collider.GetComponentInParent<ResourceNode>());
                    return RtsCommandResult.HarvestIssued;
                case RtsContextCommandKind.Rally:
                    return game.PlayerCommands != null && game.PlayerCommands.SetSelectedRallyPoint(GetGroundPoint(hit)) ? RtsCommandResult.RallyPointSet : RtsCommandResult.None;
                case RtsContextCommandKind.Move:
                    IssueMove(GetGroundPoint(hit));
                    return RtsCommandResult.MoveIssued;
                default:
                    return RtsCommandResult.None;
            }
        }

        public bool TryGetPointerHit(Ray ray, float maxDistance, out RaycastHit hit)
        {
            return Physics.Raycast(ray, out hit, maxDistance);
        }

        public void GatherSelectedControllableUnits(List<RtsUnit> results)
        {
            results.Clear();
            if (game == null)
            {
                return;
            }

            for (int i = 0; i < game.Selection.Count; i++)
            {
                RtsUnit unit = game.Selection[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Player)
                {
                    results.Add(unit);
                }
            }
        }

        private void IssueAttack(RtsEntity target)
        {
            GatherSelectedControllableUnits(commandUnits);
            for (int i = 0; i < commandUnits.Count; i++)
            {
                commandUnits[i].IssueAttack(target);
            }
        }

        private void IssueHarvest(ResourceNode resource)
        {
            if (resource == null)
            {
                return;
            }

            RefineryStructure refinery = game.FindNearestPlayerRefinery(resource.transform.position);
            GatherSelectedControllableUnits(commandUnits);

            bool assigned = false;
            for (int i = 0; i < commandUnits.Count; i++)
            {
                HarvesterUnit harvester = commandUnits[i] as HarvesterUnit;
                if (harvester != null)
                {
                    harvester.IssueHarvest(resource, refinery);
                    assigned = true;
                }
            }

            if (!assigned)
            {
                game.SpawnFloatingText("Select harvester", resource.transform.position + Vector3.up * 2f, Color.yellow);
            }
        }

        private void IssueMove(Vector3 point)
        {
            GatherSelectedControllableUnits(commandUnits);
            int count = commandUnits.Count;

            for (int i = 0; i < count; i++)
            {
                commandUnits[i].IssueMove(point + FormationOffset(i, count));
            }
        }

        private bool CanSetRallyPoint(Vector3 point)
        {
            if (game == null || game.Selection.Count == 0)
            {
                return false;
            }

            bool hasProducer = false;
            for (int i = 0; i < game.Selection.Count; i++)
            {
                RtsEntity entity = game.Selection[i];
                if (entity is RtsUnit && entity.Team == RtsTeam.Player)
                {
                    return false;
                }

                ProductionStructure producer = entity as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player)
                {
                    hasProducer = true;
                }
            }

            return hasProducer;
        }

        private static Vector3 GetGroundPoint(RaycastHit hit)
        {
            Vector3 point = hit.point;
            point.y = 0f;
            return point;
        }

        private static Vector3 FormationOffset(int index, int count)
        {
            int width = Mathf.Min(4, Mathf.Max(1, count));
            int row = index / width;
            int column = index % width;
            float center = (width - 1) * 0.5f;
            return new Vector3((column - center) * 1.45f, 0f, row * -1.45f);
        }
    }
}
