using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsPlayerCommandService
    {
        private RtsGame game;

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        public bool RequestConstruction(StructureKind kind)
        {
            if (!CanAcceptCommands() || game.BuildManager == null)
            {
                return false;
            }

            return game.BuildManager.BeginPlacement(kind);
        }

        public bool UpdateConstructionPlacement(Ray ray)
        {
            return UpdateConstructionPlacement(ray, 250f);
        }

        public bool UpdateConstructionPlacement(Ray ray, float maxDistance)
        {
            if (!CanAcceptCommands() || game.BuildManager == null || !game.BuildManager.IsPlacing)
            {
                return false;
            }

            game.BuildManager.UpdatePlacement(ray, maxDistance);
            return true;
        }

        public bool ConfirmConstructionPlacement()
        {
            if (!CanAcceptCommands() || game.BuildManager == null || !game.BuildManager.IsPlacing)
            {
                return false;
            }

            return game.BuildManager.TryConfirmPlacement();
        }

        public bool CancelConstructionPlacement()
        {
            if (!CanAcceptCommands() || game.BuildManager == null || !game.BuildManager.IsPlacing)
            {
                return false;
            }

            game.BuildManager.CancelPlacement();
            return true;
        }

        public bool CanRequestConstruction(StructureKind kind, out string disabledReason)
        {
            disabledReason = string.Empty;
            if (game == null || game.Resources == null)
            {
                disabledReason = "No game";
                return false;
            }

            if (game.IsMatchOver)
            {
                disabledReason = "Match complete";
                return false;
            }

            if (!game.AcceptsPlayerInput)
            {
                disabledReason = "Paused";
                return false;
            }

            if (!game.CanBuildStructure(kind))
            {
                disabledReason = game.GetStructureRequirement(kind);
                return false;
            }

            StructureStats stats = RtsBalance.GetStructure(kind);
            if (!game.Resources.CanAfford(stats.Cost))
            {
                disabledReason = "Need credits";
                return false;
            }

            return true;
        }

        public bool QueueProduction(UnitKind kind)
        {
            if (!CanAcceptCommands() || game.IsMatchOver)
            {
                return false;
            }

            ProductionStructure producer = FindProducerForUnit(kind, true);
            if (producer == null)
            {
                producer = FindProducerForUnit(kind, false);
            }

            if (producer == null)
            {
                game.SpawnFloatingText("Need producer", game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                return false;
            }

            return producer.QueueUnit(kind);
        }

        public bool CanQueueProduction(UnitKind kind, out string disabledReason)
        {
            disabledReason = string.Empty;
            if (game == null || game.Resources == null)
            {
                disabledReason = "No game";
                return false;
            }

            if (game.IsMatchOver)
            {
                disabledReason = "Match complete";
                return false;
            }

            if (!game.AcceptsPlayerInput)
            {
                disabledReason = "Paused";
                return false;
            }

            ProductionStructure producer = FindProducerForUnit(kind, true);
            if (producer == null)
            {
                producer = FindProducerForUnit(kind, false);
            }

            if (producer == null)
            {
                disabledReason = GetUnitRequirement(kind);
                return false;
            }

            UnitStats stats = RtsBalance.GetUnit(kind);
            if (!game.Resources.CanAfford(stats.Cost))
            {
                disabledReason = "Need credits";
                return false;
            }

            return true;
        }

        public bool CanQueueProductionFromSelected(UnitKind kind, out string disabledReason)
        {
            disabledReason = string.Empty;
            if (game == null || game.Resources == null)
            {
                disabledReason = "No game";
                return false;
            }

            if (game.IsMatchOver)
            {
                disabledReason = "Match complete";
                return false;
            }

            if (!game.AcceptsPlayerInput)
            {
                disabledReason = "Paused";
                return false;
            }

            ProductionStructure producer = FindProducerForUnit(kind, true);
            if (producer == null)
            {
                disabledReason = GetUnitRequirement(kind);
                return false;
            }

            UnitStats stats = RtsBalance.GetUnit(kind);
            if (!game.Resources.CanAfford(stats.Cost))
            {
                disabledReason = "Need credits";
                return false;
            }

            return true;
        }

        public bool CancelLastQueuedProduction()
        {
            return CancelProduction();
        }

        public bool CancelProduction()
        {
            if (!CanAcceptCommands())
            {
                return false;
            }

            ProductionStructure producer = FindSelectedProductionWithCancelableItem();
            if (producer == null)
            {
                producer = FindAnyProductionWithCancelableItem();
            }

            if (producer == null)
            {
                if (game != null)
                {
                    game.SpawnFloatingText("No production", game.GetPlayerBaseCenter() + Vector3.up * 2f, Color.yellow);
                }

                return false;
            }

            UnitKind canceled;
            int refund;
            return producer.TryCancelProduction(out canceled, out refund);
        }

        public bool RepairSelectedStructures()
        {
            return CanAcceptCommands() && game.TryRepairSelectedStructures();
        }

        public bool SellSelectedStructures()
        {
            return CanAcceptCommands() && game.SellSelectedStructures();
        }

        public bool SetSelectedRallyPoint(Vector3 point)
        {
            if (!CanAcceptCommands() || game.Selection.Count == 0)
            {
                return false;
            }

            bool setAny = false;
            for (int i = 0; i < game.Selection.Count; i++)
            {
                ProductionStructure producer = game.Selection[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive)
                {
                    producer.SetRallyPoint(point);
                    setAny = true;
                }
            }

            if (setAny)
            {
                game.SpawnFloatingText("Rally set", point + Vector3.up * 1.4f, new Color(0.5f, 0.95f, 1f));
            }

            return setAny;
        }

        public bool CanRepairStructure(RtsStructure structure)
        {
            return structure != null &&
                structure.Team == RtsTeam.Player &&
                structure.IsAlive &&
                structure.Health < structure.MaxHealth - 0.5f &&
                game != null &&
                game.Resources != null &&
                game.Resources.Credits >= 25;
        }

        public bool CanSellStructure(RtsStructure structure)
        {
            return structure != null && structure.Team == RtsTeam.Player && structure.IsAlive;
        }

        public ProductionStructure FindSelectedProductionStructure()
        {
            if (game == null)
            {
                return null;
            }

            for (int i = 0; i < game.Selection.Count; i++)
            {
                ProductionStructure producer = game.Selection[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive)
                {
                    return producer;
                }
            }

            return null;
        }

        public ProductionStructure FindProducerForUnit(UnitKind kind, bool selectedOnly)
        {
            if (game == null)
            {
                return null;
            }

            int count = selectedOnly ? game.Selection.Count : game.Entities.Count;
            for (int i = 0; i < count; i++)
            {
                RtsEntity entity = selectedOnly ? game.Selection[i] : game.Entities[i];
                ProductionStructure producer = entity as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive && producer.CanTrain(kind))
                {
                    return producer;
                }
            }

            return null;
        }

        public string GetUnitRequirement(UnitKind kind)
        {
            ProductionStructure selectedProducer = FindSelectedProductionStructure();
            if (selectedProducer != null)
            {
                if (RtsBalance.IsInfantry(kind))
                {
                    return selectedProducer.CanTrain(kind) ? string.Empty : "Select Barracks";
                }

                if (RtsBalance.IsVehicle(kind))
                {
                    return selectedProducer.CanTrain(kind) ? string.Empty : "Select War Factory";
                }

                if (RtsBalance.IsAircraft(kind))
                {
                    return selectedProducer.CanTrain(kind) ? string.Empty : "Select Helipad";
                }
            }

            if (RtsBalance.IsAircraft(kind))
            {
                return HasAnyPlayerProducerFor(kind) ? "Select Helipad" : "Needs Helipad";
            }

            if (RtsBalance.IsVehicle(kind))
            {
                return HasAnyPlayerProducerFor(kind) ? "Select War Factory" : "Needs War Factory";
            }

            return HasAnyPlayerProducerFor(kind) ? "Select Barracks" : "Needs Barracks";
        }

        private bool HasAnyPlayerProducerFor(UnitKind kind)
        {
            return FindProducerForUnit(kind, false) != null;
        }

        private ProductionStructure FindSelectedProductionWithCancelableItem()
        {
            if (game == null)
            {
                return null;
            }

            for (int i = 0; i < game.Selection.Count; i++)
            {
                ProductionStructure producer = game.Selection[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive && producer.CanCancelProduction)
                {
                    return producer;
                }
            }

            return null;
        }

        private ProductionStructure FindAnyProductionWithCancelableItem()
        {
            if (game == null)
            {
                return null;
            }

            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player && producer.IsAlive && producer.CanCancelProduction)
                {
                    return producer;
                }
            }

            return null;
        }

        private bool CanAcceptCommands()
        {
            return game != null && game.AcceptsPlayerInput;
        }
    }
}
