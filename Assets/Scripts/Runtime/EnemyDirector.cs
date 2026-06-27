using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class EnemyDirector : MonoBehaviour
    {
        private const int MaximumEnemyCredits = 12000;
        private const int BaseIncome = 180;
        private const int RefineryIncomeBonus = 160;
        private const float IncomeInterval = 7.5f;
        private const float BuildInterval = 12f;
        private const float ProductionInterval = 8f;
        private const float IdleOrderInterval = 3.5f;

        private RtsGame game;
        private float nextWaveTime;
        private float nextIdleOrderTime;
        private float nextIncomeTime;
        private float nextBuildTime;
        private float nextProductionTime;
        private int enemyCredits;
        private int waveIndex;

        public void Initialize(RtsGame owner)
        {
            game = owner;
            ResetEconomy(game.Clock.SimulationTime);
        }

        private void Update()
        {
            if (game == null || game.IsMatchOver || game.Clock.IsPaused)
            {
                return;
            }

            using (RtsProfilerMarkers.EnemyDirector.Auto())
            {
                if (game.Clock.SimulationTime >= nextWaveTime)
                {
                    SpawnWave();
                }

                if (game.Clock.SimulationTime >= nextIncomeTime)
                {
                    nextIncomeTime = game.Clock.SimulationTime + IncomeInterval;
                    GrantIncome();
                }

                if (game.Clock.SimulationTime >= nextBuildTime)
                {
                    nextBuildTime = game.Clock.SimulationTime + GetBuildInterval();
                    TryBuildBaseStructure();
                }

                if (game.Clock.SimulationTime >= nextProductionTime)
                {
                    nextProductionTime = game.Clock.SimulationTime + GetProductionInterval();
                    TryProduceUnit();
                }

                if (game.Clock.SimulationTime >= nextIdleOrderTime)
                {
                    nextIdleOrderTime = game.Clock.SimulationTime + IdleOrderInterval;
                    OrderIdleEnemies();
                }
            }
        }

        public RtsEnemyDirectorSaveData CaptureState()
        {
            return new RtsEnemyDirectorSaveData
            {
                hasEconomyState = true,
                waveIndex = waveIndex,
                enemyCredits = enemyCredits,
                nextWaveTime = nextWaveTime,
                nextIdleOrderTime = nextIdleOrderTime,
                nextIncomeTime = nextIncomeTime,
                nextBuildTime = nextBuildTime,
                nextProductionTime = nextProductionTime
            };
        }

        public void RestoreState(RtsEnemyDirectorSaveData data)
        {
            if (data == null)
            {
                ResetEconomy(game.Clock.SimulationTime);
                return;
            }

            float now = game.Clock.SimulationTime;
            waveIndex = Mathf.Max(0, data.waveIndex);
            enemyCredits = data.hasEconomyState ? Mathf.Clamp(data.enemyCredits, 0, MaximumEnemyCredits) : GetOptions().EnemyStartingCredits;
            nextWaveTime = Mathf.Max(now + 1f, data.nextWaveTime);
            nextIdleOrderTime = Mathf.Max(now + 0.5f, data.nextIdleOrderTime);
            nextIncomeTime = data.hasEconomyState ? Mathf.Max(now + 0.5f, data.nextIncomeTime) : now + 5f;
            nextBuildTime = data.hasEconomyState ? Mathf.Max(now + 0.5f, data.nextBuildTime) : now + GetOptions().EnemyBuildDelaySeconds;
            nextProductionTime = data.hasEconomyState ? Mathf.Max(now + 0.5f, data.nextProductionTime) : now + GetOptions().EnemyProductionDelaySeconds;
        }

        public void ResetForNewMatch()
        {
            ResetEconomy(game != null && game.Clock != null ? game.Clock.SimulationTime : 0f);
        }

#if UNITY_EDITOR
        public int EnemyCreditsForTests => enemyCredits;

        public void SetEconomyForTests(int credits, float nextIncome, float nextBuild, float nextProduction, float nextWave, float nextIdleOrder, int wave)
        {
            enemyCredits = Mathf.Clamp(credits, 0, MaximumEnemyCredits);
            nextIncomeTime = Mathf.Max(0f, nextIncome);
            nextBuildTime = Mathf.Max(0f, nextBuild);
            nextProductionTime = Mathf.Max(0f, nextProduction);
            nextWaveTime = Mathf.Max(0f, nextWave);
            nextIdleOrderTime = Mathf.Max(0f, nextIdleOrder);
            waveIndex = Mathf.Max(0, wave);
        }

        public void SetEnemyCreditsForTests(int credits)
        {
            enemyCredits = Mathf.Clamp(credits, 0, MaximumEnemyCredits);
        }

        public bool TryBuildBaseStructureForTests()
        {
            return TryBuildBaseStructure();
        }

        public bool TryProduceUnitForTests()
        {
            return TryProduceUnit();
        }
#endif

        private void SpawnWave()
        {
            RtsEntity target = game.FindPlayerPrimaryTarget();
            if (target == null || IsOpeningGraceActive())
            {
                nextWaveTime = game.Clock.SimulationTime + 8f;
                return;
            }

            int nextWaveIndex = waveIndex + 1;
            Vector3 enemyBase = game.GetEnemyBaseCenter();
            int infantryCount = 2 + Mathf.Min(5, nextWaveIndex);
            int spawned = 0;
            for (int i = 0; i < infantryCount; i++)
            {
                UnitKind infantryKind = GetWaveInfantryKind(nextWaveIndex, i);
                if (!TrySpendEnemyCredits(RtsBalance.GetUnit(infantryKind).Cost))
                {
                    break;
                }

                Vector3 spawn = enemyBase + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
                RtsUnit unit = game.CreateUnit(RtsTeam.Enemy, infantryKind, spawn);
                unit.IssueAttack(target);
                spawned++;
            }

            if (nextWaveIndex % 2 == 0 && HasLivingEnemyStructure(StructureKind.WarFactory) && TrySpendEnemyCredits(RtsBalance.GetUnit(UnitKind.MediumTank).Cost))
            {
                RtsUnit tank = game.CreateUnit(RtsTeam.Enemy, UnitKind.MediumTank, enemyBase + new Vector3(Random.Range(-7f, 7f), 0f, Random.Range(-7f, 7f)));
                tank.IssueAttack(target);
                spawned++;
            }

            if (spawned <= 0)
            {
                nextWaveTime = game.Clock.SimulationTime + 8f;
                return;
            }

            waveIndex = nextWaveIndex;
            nextWaveTime = game.Clock.SimulationTime + Mathf.Max(18f, 42f - waveIndex * 1.5f) * GetOptions().EnemyWaveIntervalMultiplier;
            game.SpawnFloatingText("Enemy attack", enemyBase + Vector3.up * 3f, new Color(1f, 0.5f, 0.35f));
        }

        private void OrderIdleEnemies()
        {
            RtsEntity target = game.FindPlayerPrimaryTarget();
            if (target == null)
            {
                return;
            }

            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Enemy && unit.IsIdle())
                {
                    unit.IssueAttack(target);
                }
            }
        }

        private void ResetEconomy(float now)
        {
            RtsSkirmishOptions options = GetOptions();
            enemyCredits = Mathf.Clamp(options.EnemyStartingCredits, 0, MaximumEnemyCredits);
            waveIndex = 0;
            nextWaveTime = now + options.OpeningAttackGraceSeconds;
            nextIdleOrderTime = now + options.OpeningAttackGraceSeconds;
            nextIncomeTime = now + 5f;
            nextBuildTime = now + options.EnemyBuildDelaySeconds;
            nextProductionTime = now + options.EnemyProductionDelaySeconds;
        }

        private void GrantIncome()
        {
            if (game.CountLivingStructures(RtsTeam.Enemy) <= 0)
            {
                return;
            }

            int income = Mathf.RoundToInt((BaseIncome + CountLivingEnemyStructures(StructureKind.Refinery) * RefineryIncomeBonus) * GetOptions().EnemyIncomeMultiplier);
            enemyCredits = Mathf.Clamp(enemyCredits + income, 0, MaximumEnemyCredits);
        }

        private bool TryBuildBaseStructure()
        {
            if (game.CountLivingStructures(RtsTeam.Enemy) <= 0)
            {
                return false;
            }

            StructureKind? targetKind = ChooseStructureToBuild();
            if (!targetKind.HasValue)
            {
                return false;
            }

            StructureStats stats = RtsBalance.GetStructure(targetKind.Value);
            if (!TrySpendEnemyCredits(stats.Cost))
            {
                return false;
            }

            int slotIndex = CountLivingEnemyStructures(targetKind.Value);
            RtsStructure structure = game.CreateStructure(RtsTeam.Enemy, targetKind.Value, GetEnemyBuildSlot(targetKind.Value, slotIndex));
            game.SpawnFloatingText("Enemy " + stats.Name, structure.transform.position + Vector3.up * 2.5f, new Color(1f, 0.46f, 0.28f));
            return true;
        }

        private bool TryProduceUnit()
        {
            UnitKind kind = ChooseUnitToProduce();
            ProductionStructure producer = FindEnemyProducer(kind);
            if (producer == null)
            {
                return false;
            }

            UnitStats stats = RtsBalance.GetUnit(kind);
            if (!TrySpendEnemyCredits(stats.Cost))
            {
                return false;
            }

            RtsUnit unit = producer.SpawnProducedUnit(kind, null);
            RtsEntity target = game.FindPlayerPrimaryTarget();
            if (unit != null && target != null && !IsOpeningGraceActive())
            {
                unit.IssueAttackMove(target.transform.position + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f)));
            }

            return true;
        }

        private StructureKind? ChooseStructureToBuild()
        {
            if (!HasLivingEnemyStructure(StructureKind.CommandCenter) && enemyCredits >= RtsBalance.GetStructure(StructureKind.CommandCenter).Cost)
            {
                return StructureKind.CommandCenter;
            }

            if (!HasLivingEnemyStructure(StructureKind.PowerPlant) || EnemyPowerUsed() > EnemyPowerProvided())
            {
                return StructureKind.PowerPlant;
            }

            if (!HasLivingEnemyStructure(StructureKind.Barracks))
            {
                return StructureKind.Barracks;
            }

            if (!HasLivingEnemyStructure(StructureKind.Refinery))
            {
                return StructureKind.Refinery;
            }

            if (!HasLivingEnemyStructure(StructureKind.WarFactory))
            {
                return StructureKind.WarFactory;
            }

            if (CountLivingEnemyStructures(StructureKind.DualHelipad) < 1)
            {
                return StructureKind.DualHelipad;
            }

            if (CountLivingEnemyStructures(StructureKind.Turret) < 2)
            {
                return StructureKind.Turret;
            }

            if (CountLivingEnemyStructures(StructureKind.GunTower) < 1)
            {
                return StructureKind.GunTower;
            }

            if (CountLivingEnemyStructures(StructureKind.AdvancedGunTower) < 1)
            {
                return StructureKind.AdvancedGunTower;
            }

            return null;
        }

        private UnitKind ChooseUnitToProduce()
        {
            bool hasWarFactory = HasLivingEnemyStructure(StructureKind.WarFactory);
            bool hasHelipad = HasLivingEnemyStructure(StructureKind.DualHelipad);
            if (hasHelipad && enemyCredits >= RtsBalance.GetUnit(UnitKind.Skyraider).Cost && CountLivingEnemyUnits(UnitKind.Skyraider) < 2)
            {
                return UnitKind.Skyraider;
            }

            if (hasHelipad && enemyCredits >= RtsBalance.GetUnit(UnitKind.OrcaLifter).Cost && CountLivingEnemyUnits(UnitKind.OrcaLifter) < 1)
            {
                return UnitKind.OrcaLifter;
            }

            if (hasWarFactory && enemyCredits >= RtsBalance.GetUnit(UnitKind.HeavyTank).Cost && CountLivingEnemyUnits(UnitKind.HeavyTank) < 2)
            {
                return UnitKind.HeavyTank;
            }

            if (hasWarFactory && enemyCredits >= RtsBalance.GetUnit(UnitKind.MediumTank).Cost && CountLivingEnemyUnits(UnitKind.MediumTank) < 4)
            {
                return UnitKind.MediumTank;
            }

            if (hasWarFactory && enemyCredits >= RtsBalance.GetUnit(UnitKind.Apc).Cost && CountLivingEnemyUnits(UnitKind.Apc) < 3)
            {
                return UnitKind.Apc;
            }

            if (hasWarFactory && enemyCredits >= RtsBalance.GetUnit(UnitKind.Humvee).Cost && CountLivingEnemyUnits(UnitKind.Humvee) < 4)
            {
                return UnitKind.Humvee;
            }

            if (hasWarFactory && enemyCredits >= RtsBalance.GetUnit(UnitKind.LightTank).Cost && CountLivingEnemyUnits(UnitKind.LightTank) < 5)
            {
                return UnitKind.LightTank;
            }

            if (enemyCredits >= RtsBalance.GetUnit(UnitKind.RocketSoldier).Cost && CountLivingEnemyUnits(UnitKind.RocketSoldier) < 3)
            {
                return UnitKind.RocketSoldier;
            }

            if (enemyCredits >= RtsBalance.GetUnit(UnitKind.FlameTrooper).Cost && CountLivingEnemyUnits(UnitKind.FlameTrooper) < 3)
            {
                return UnitKind.FlameTrooper;
            }

            if (enemyCredits >= RtsBalance.GetUnit(UnitKind.Grenadier).Cost && CountLivingEnemyUnits(UnitKind.Grenadier) < 4)
            {
                return UnitKind.Grenadier;
            }

            return UnitKind.Rifleman;
        }

        private static UnitKind GetWaveInfantryKind(int wave, int index)
        {
            int pattern = (wave + index) % 5;
            switch (pattern)
            {
                case 1:
                    return UnitKind.Grenadier;
                case 2:
                    return UnitKind.RocketSoldier;
                case 3:
                    return UnitKind.FlameTrooper;
                default:
                    return UnitKind.Rifleman;
            }
        }

        private ProductionStructure FindEnemyProducer(UnitKind kind)
        {
            for (int i = 0; i < game.Entities.Count; i++)
            {
                ProductionStructure producer = game.Entities[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Enemy && producer.IsAlive && producer.CanTrain(kind))
                {
                    return producer;
                }
            }

            return null;
        }

        private bool TrySpendEnemyCredits(int amount)
        {
            if (enemyCredits < amount)
            {
                return false;
            }

            enemyCredits -= amount;
            return true;
        }

        private int CountLivingEnemyStructures(StructureKind kind)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Enemy && structure.IsAlive && structure.StructureKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountLivingEnemyUnits(UnitKind kind)
        {
            int count = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsUnit unit = game.Entities[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Enemy && unit.IsAlive && unit.UnitKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasLivingEnemyStructure(StructureKind kind)
        {
            return CountLivingEnemyStructures(kind) > 0;
        }

        private bool IsOpeningGraceActive()
        {
            return game != null && game.Clock != null && game.Clock.SimulationTime < GetOptions().OpeningAttackGraceSeconds;
        }

        private float GetBuildInterval()
        {
            return BuildInterval * GetOptions().EnemyBuildIntervalMultiplier;
        }

        private float GetProductionInterval()
        {
            return ProductionInterval * GetOptions().EnemyProductionIntervalMultiplier;
        }

        private RtsSkirmishOptions GetOptions()
        {
            return game != null && game.SkirmishOptions != null ? game.SkirmishOptions : RtsSkirmishOptions.CreateDefault();
        }

        private int EnemyPowerProvided()
        {
            int total = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Enemy && structure.IsAlive)
                {
                    total += structure.PowerProvided;
                }
            }

            return total;
        }

        private int EnemyPowerUsed()
        {
            int total = 0;
            for (int i = 0; i < game.Entities.Count; i++)
            {
                RtsStructure structure = game.Entities[i] as RtsStructure;
                if (structure != null && structure.Team == RtsTeam.Enemy && structure.IsAlive)
                {
                    total += structure.PowerUsed;
                }
            }

            return total;
        }

        private static Vector3 GetEnemyBuildSlot(StructureKind kind, int slotIndex)
        {
            switch (kind)
            {
                case StructureKind.CommandCenter:
                    return new Vector3(86f, 0f, 78f);
                case StructureKind.PowerPlant:
                    return slotIndex == 0 ? new Vector3(96f, 0f, 62f) : new Vector3(104f, 0f, 82f);
                case StructureKind.Barracks:
                    return new Vector3(74f, 0f, 64f);
                case StructureKind.Refinery:
                    return new Vector3(82f, 0f, 96f);
                case StructureKind.WarFactory:
                    return new Vector3(100f, 0f, 82f);
                case StructureKind.DualHelipad:
                    return new Vector3(94f, 0f, 96f);
                case StructureKind.Turret:
                    return slotIndex == 0 ? new Vector3(66f, 0f, 84f) : new Vector3(96f, 0f, 48f);
                case StructureKind.GunTower:
                    return new Vector3(70f, 0f, 96f);
                case StructureKind.AdvancedGunTower:
                    return new Vector3(104f, 0f, 58f);
                default:
                    return new Vector3(82f, 0f, 72f);
            }
        }
    }
}
