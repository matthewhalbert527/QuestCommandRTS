using UnityEngine;

namespace QuestCommandRTS
{
    public struct RtsBuildOptionView
    {
        public StructureKind Kind;
        public string Name;
        public int Cost;
        public int PowerProvided;
        public int PowerUsed;
        public bool HasPrerequisite;
        public bool CanAfford;
        public bool IsAvailable;
        public bool WillCauseLowPower;
        public string DisabledReason;
        public string PowerText;
    }

    public struct RtsProductionOptionView
    {
        public UnitKind Kind;
        public string Name;
        public int Cost;
        public float BuildTime;
        public bool HasProducerSelected;
        public bool ProducerCanTrain;
        public bool CanAfford;
        public bool IsAvailable;
        public string DisabledReason;
    }

    public struct RtsSelectedEntityView
    {
        public string Name;
        public string EntityType;
        public int SelectedCount;
        public int UnitCount;
        public int StructureCount;
        public int Health;
        public int MaxHealth;
        public bool HasSingleEntity;
        public bool HasProduction;
        public bool HasRallyPoint;
        public string RallyText;
        public string QueueSummary;
        public bool CanRepair;
        public bool CanSell;
        public bool CanCancelQueue;
    }

    public sealed class RtsCommandConsoleModel
    {
        public static readonly StructureKind[] BuildKinds =
        {
            StructureKind.CommandCenter,
            StructureKind.PowerPlant,
            StructureKind.Barracks,
            StructureKind.Refinery,
            StructureKind.WarFactory,
            StructureKind.Turret,
            StructureKind.GunTower,
            StructureKind.AdvancedGunTower
        };

        public static readonly UnitKind[] UnitKinds =
        {
            UnitKind.Rifleman,
            UnitKind.Grenadier,
            UnitKind.RocketSoldier,
            UnitKind.FlameTrooper,
            UnitKind.Engineer,
            UnitKind.Harvester,
            UnitKind.Humvee,
            UnitKind.Apc,
            UnitKind.LightTank,
            UnitKind.MediumTank,
            UnitKind.HeavyTank
        };

        private RtsGame game;

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        public int BuildOptionCount => BuildKinds.Length;
        public int ProductionOptionCount => UnitKinds.Length;

        public RtsBuildOptionView GetBuildOption(int index)
        {
            StructureKind kind = BuildKinds[Mathf.Clamp(index, 0, BuildKinds.Length - 1)];
            StructureStats stats = RtsBalance.GetStructure(kind);

            bool hasPrerequisite = game != null && game.CanBuildStructure(kind);
            bool canAfford = game != null && game.Resources != null && game.Resources.CanAfford(stats.Cost);
            string disabledReason = string.Empty;
            bool available = game != null && game.PlayerCommands != null && game.PlayerCommands.CanRequestConstruction(kind, out disabledReason);
            bool lowPower = false;

            if (game != null && game.Resources != null)
            {
                int provided = game.Resources.PowerProvided + stats.PowerProvided;
                int used = game.Resources.PowerUsed + stats.PowerUsed;
                lowPower = used > provided;
            }

            if (!available && string.IsNullOrEmpty(disabledReason))
            {
                disabledReason = hasPrerequisite ? "Unavailable" : game.GetStructureRequirement(kind);
            }

            return new RtsBuildOptionView
            {
                Kind = kind,
                Name = stats.Name,
                Cost = stats.Cost,
                PowerProvided = stats.PowerProvided,
                PowerUsed = stats.PowerUsed,
                HasPrerequisite = hasPrerequisite,
                CanAfford = canAfford,
                IsAvailable = available,
                WillCauseLowPower = lowPower,
                DisabledReason = disabledReason,
                PowerText = GetPowerText(stats.PowerProvided, stats.PowerUsed)
            };
        }

        public RtsProductionOptionView GetProductionOption(int index)
        {
            UnitKind kind = UnitKinds[Mathf.Clamp(index, 0, UnitKinds.Length - 1)];
            UnitStats stats = RtsBalance.GetUnit(kind);
            ProductionStructure selectedProducer = game != null && game.PlayerCommands != null ? game.PlayerCommands.FindSelectedProductionStructure() : null;
            bool hasProducer = selectedProducer != null;
            bool producerCanTrain = hasProducer && selectedProducer.CanTrain(kind);
            bool canAfford = game != null && game.Resources != null && game.Resources.CanAfford(stats.Cost);
            string disabledReason = string.Empty;
            bool available = game != null && game.PlayerCommands != null && game.PlayerCommands.CanQueueProductionFromSelected(kind, out disabledReason);

            if (!available && string.IsNullOrEmpty(disabledReason))
            {
                disabledReason = producerCanTrain || game == null || game.PlayerCommands == null ? "Unavailable" : game.PlayerCommands.GetUnitRequirement(kind);
            }

            return new RtsProductionOptionView
            {
                Kind = kind,
                Name = stats.Name,
                Cost = stats.Cost,
                BuildTime = stats.BuildTime,
                HasProducerSelected = hasProducer,
                ProducerCanTrain = producerCanTrain,
                CanAfford = canAfford,
                IsAvailable = available,
                DisabledReason = disabledReason
            };
        }

        public RtsSelectedEntityView GetSelectedEntityView()
        {
            RtsSelectedEntityView view = new RtsSelectedEntityView
            {
                Name = "No selection",
                EntityType = string.Empty,
                RallyText = "No rally",
                QueueSummary = "Idle"
            };

            if (game == null)
            {
                return view;
            }

            view.SelectedCount = game.Selection.Count;
            if (game.Selection.Count == 0)
            {
                return view;
            }

            for (int i = 0; i < game.Selection.Count; i++)
            {
                if (game.Selection[i] is RtsUnit)
                {
                    view.UnitCount++;
                }
                else if (game.Selection[i] is RtsStructure)
                {
                    view.StructureCount++;
                }
            }

            if (game.Selection.Count > 1)
            {
                view.Name = game.Selection.Count + " selected";
                view.EntityType = "Mixed";
                view.CanRepair = game.CanRepairSelectedStructures();
                view.CanSell = game.CanSellSelectedStructures();
                return view;
            }

            RtsEntity entity = game.Selection[0];
            if (entity == null)
            {
                return view;
            }

            view.HasSingleEntity = true;
            view.Name = entity.DisplayName;
            view.EntityType = entity is RtsStructure ? "Structure" : "Unit";
            view.Health = Mathf.RoundToInt(entity.Health);
            view.MaxHealth = Mathf.RoundToInt(entity.MaxHealth);
            view.CanRepair = game.CanRepairSelectedStructures();
            view.CanSell = game.CanSellSelectedStructures();

            ProductionStructure producer = entity as ProductionStructure;
            if (producer != null)
            {
                view.HasProduction = true;
                view.HasRallyPoint = producer.HasRallyPoint;
                view.RallyText = producer.HasRallyPoint ? "Rally set" : "No rally set";
                view.QueueSummary = producer.GetQueueSummary();
                view.CanCancelQueue = producer.CanCancelProduction;
            }

            MediumTankUnit mediumTank = entity as MediumTankUnit;
            if (mediumTank != null)
            {
                view.QueueSummary = mediumTank.HasPassenger ? "Passenger " + RtsBalance.GetUnit(mediumTank.LoadedPassengerKind).Name + " loaded" : "No passenger infantry";
            }

            return view;
        }

        public string GetProductionQueueLine(int queueIndex)
        {
            ProductionStructure producer = game != null && game.PlayerCommands != null ? game.PlayerCommands.FindSelectedProductionStructure() : null;
            if (producer == null)
            {
                return queueIndex == 0 ? "Select a production structure" : string.Empty;
            }

            if (queueIndex == 0)
            {
                if (!producer.HasActiveProduction)
                {
                    return producer.PendingQueueCount > 0 ? "Starting next item" : "Idle";
                }

                UnitStats stats = RtsBalance.GetUnit(producer.ActiveProductionKind);
                int percent = Mathf.RoundToInt(producer.ActiveProductionProgress * 100f);
                return "Now: " + stats.Name + " " + percent + "%";
            }

            UnitKind queuedKind;
            if (producer.TryGetQueuedUnit(queueIndex - 1, out queuedKind))
            {
                return queueIndex + ". " + RtsBalance.GetUnit(queuedKind).Name;
            }

            return string.Empty;
        }

        private static string GetPowerText(int provided, int used)
        {
            if (provided > 0)
            {
                return "Power +" + provided;
            }

            if (used > 0)
            {
                return "Power -" + used;
            }

            return "Power 0";
        }
    }
}
