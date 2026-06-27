using UnityEngine;

namespace QuestCommandRTS
{
    public enum RtsTeam
    {
        Player,
        Enemy,
        Neutral
    }

    public enum RtsMatchState
    {
        Running,
        Victory,
        Defeat
    }

    public enum UnitKind
    {
        Rifleman,
        Harvester,
        Tank,
        LightTank,
        MediumTank,
        HeavyTank,
        Grenadier,
        RocketSoldier,
        FlameTrooper,
        Engineer,
        Humvee,
        Apc,
        Skyraider,
        OrcaLifter
    }

    public enum StructureKind
    {
        CommandCenter,
        Refinery,
        Barracks,
        WarFactory,
        PowerPlant,
        Turret,
        GunTower,
        AdvancedGunTower,
        DualHelipad
    }

    public enum RtsProjectileKind
    {
        RifleRound,
        TankShell,
        Grenade,
        Rocket,
        FlameBolt,
        DefenseShell
    }

    public enum RtsAiDifficulty
    {
        Recruit,
        Standard,
        Veteran,
        Brutal
    }

    public enum RtsStartingCreditsPreset
    {
        Low,
        Standard,
        High,
        Massive
    }

    public enum RtsPeaceTimePreset
    {
        None,
        TwoMinutes,
        ThreeMinutes,
        FiveMinutes
    }

    public enum RtsGameSpeedPreset
    {
        Slow,
        Normal,
        Fast
    }

    public enum RtsFogPreset
    {
        Enabled,
        Revealed
    }

    public enum RtsStartingForcesPreset
    {
        FabricationOnly,
        ScoutTeam,
        StrikeTeam
    }

    [System.Serializable]
    public sealed class RtsSkirmishOptions
    {
        public RtsAiDifficulty difficulty = RtsAiDifficulty.Standard;
        public RtsStartingCreditsPreset startingCredits = RtsStartingCreditsPreset.Standard;
        public RtsPeaceTimePreset peaceTime = RtsPeaceTimePreset.ThreeMinutes;
        public RtsGameSpeedPreset gameSpeed = RtsGameSpeedPreset.Normal;
        public RtsFogPreset fog = RtsFogPreset.Enabled;
        public RtsStartingForcesPreset startingForces = RtsStartingForcesPreset.FabricationOnly;

        public static RtsSkirmishOptions CreateDefault()
        {
            return new RtsSkirmishOptions();
        }

        public RtsSkirmishOptions Clone()
        {
            RtsSkirmishOptions clone = new RtsSkirmishOptions
            {
                difficulty = difficulty,
                startingCredits = startingCredits,
                peaceTime = peaceTime,
                gameSpeed = gameSpeed,
                fog = fog,
                startingForces = startingForces
            };
            clone.Normalize();
            return clone;
        }

        public void Normalize()
        {
            if (!System.Enum.IsDefined(typeof(RtsAiDifficulty), difficulty))
            {
                difficulty = RtsAiDifficulty.Standard;
            }

            if (!System.Enum.IsDefined(typeof(RtsStartingCreditsPreset), startingCredits))
            {
                startingCredits = RtsStartingCreditsPreset.Standard;
            }

            if (!System.Enum.IsDefined(typeof(RtsPeaceTimePreset), peaceTime))
            {
                peaceTime = RtsPeaceTimePreset.ThreeMinutes;
            }

            if (!System.Enum.IsDefined(typeof(RtsGameSpeedPreset), gameSpeed))
            {
                gameSpeed = RtsGameSpeedPreset.Normal;
            }

            if (!System.Enum.IsDefined(typeof(RtsFogPreset), fog))
            {
                fog = RtsFogPreset.Enabled;
            }

            if (!System.Enum.IsDefined(typeof(RtsStartingForcesPreset), startingForces))
            {
                startingForces = RtsStartingForcesPreset.FabricationOnly;
            }
        }

        public int PlayerStartingCredits
        {
            get
            {
                switch (startingCredits)
                {
                    case RtsStartingCreditsPreset.Low:
                        return 4000;
                    case RtsStartingCreditsPreset.High:
                        return 10000;
                    case RtsStartingCreditsPreset.Massive:
                        return 20000;
                    default:
                        return 6200;
                }
            }
        }

        public int EnemyStartingCredits
        {
            get
            {
                switch (difficulty)
                {
                    case RtsAiDifficulty.Recruit:
                        return 1700;
                    case RtsAiDifficulty.Veteran:
                        return 3400;
                    case RtsAiDifficulty.Brutal:
                        return 4600;
                    default:
                        return 2600;
                }
            }
        }

        public float OpeningAttackGraceSeconds
        {
            get
            {
                switch (peaceTime)
                {
                    case RtsPeaceTimePreset.None:
                        return 0f;
                    case RtsPeaceTimePreset.TwoMinutes:
                        return 120f;
                    case RtsPeaceTimePreset.FiveMinutes:
                        return 300f;
                    default:
                        return 180f;
                }
            }
        }

        public float EnemyBuildDelaySeconds
        {
            get
            {
                switch (difficulty)
                {
                    case RtsAiDifficulty.Recruit:
                        return 45f;
                    case RtsAiDifficulty.Veteran:
                        return 22f;
                    case RtsAiDifficulty.Brutal:
                        return 16f;
                    default:
                        return 28f;
                }
            }
        }

        public float EnemyProductionDelaySeconds
        {
            get
            {
                switch (difficulty)
                {
                    case RtsAiDifficulty.Recruit:
                        return 95f;
                    case RtsAiDifficulty.Veteran:
                        return 58f;
                    case RtsAiDifficulty.Brutal:
                        return 42f;
                    default:
                        return 70f;
                }
            }
        }

        public float EnemyIncomeMultiplier
        {
            get
            {
                switch (difficulty)
                {
                    case RtsAiDifficulty.Recruit:
                        return 0.75f;
                    case RtsAiDifficulty.Veteran:
                        return 1.15f;
                    case RtsAiDifficulty.Brutal:
                        return 1.35f;
                    default:
                        return 1f;
                }
            }
        }

        public float EnemyBuildIntervalMultiplier
        {
            get
            {
                switch (difficulty)
                {
                    case RtsAiDifficulty.Recruit:
                        return 1.35f;
                    case RtsAiDifficulty.Veteran:
                        return 0.85f;
                    case RtsAiDifficulty.Brutal:
                        return 0.68f;
                    default:
                        return 1f;
                }
            }
        }

        public float EnemyProductionIntervalMultiplier
        {
            get
            {
                switch (difficulty)
                {
                    case RtsAiDifficulty.Recruit:
                        return 1.35f;
                    case RtsAiDifficulty.Veteran:
                        return 0.86f;
                    case RtsAiDifficulty.Brutal:
                        return 0.7f;
                    default:
                        return 1f;
                }
            }
        }

        public float EnemyWaveIntervalMultiplier
        {
            get
            {
                switch (difficulty)
                {
                    case RtsAiDifficulty.Recruit:
                        return 1.25f;
                    case RtsAiDifficulty.Veteran:
                        return 0.9f;
                    case RtsAiDifficulty.Brutal:
                        return 0.78f;
                    default:
                        return 1f;
                }
            }
        }

        public float GameSpeedMultiplier
        {
            get
            {
                switch (gameSpeed)
                {
                    case RtsGameSpeedPreset.Slow:
                        return 0.85f;
                    case RtsGameSpeedPreset.Fast:
                        return 1.2f;
                    default:
                        return 1f;
                }
            }
        }

        public bool FogOfWarEnabled => fog == RtsFogPreset.Enabled;
        public string DifficultyId => difficulty.ToString().ToLowerInvariant();
        public string ConfigId => "skirmish_" + DifficultyId + "_" + startingCredits.ToString().ToLowerInvariant() + "_" + peaceTime.ToString().ToLowerInvariant();

        public string StartingCreditsLabel => PlayerStartingCredits.ToString("N0");
        public string PeaceTimeLabel => peaceTime == RtsPeaceTimePreset.None ? "Off" : Mathf.RoundToInt(OpeningAttackGraceSeconds / 60f) + " min";
        public string GameSpeedLabel => gameSpeed == RtsGameSpeedPreset.Slow ? "Slow" : gameSpeed == RtsGameSpeedPreset.Fast ? "Fast" : "Normal";
        public string FogLabel => FogOfWarEnabled ? "On" : "Revealed";

        public string StartingForcesLabel
        {
            get
            {
                switch (startingForces)
                {
                    case RtsStartingForcesPreset.ScoutTeam:
                        return "Scout team";
                    case RtsStartingForcesPreset.StrikeTeam:
                        return "Strike team";
                    default:
                        return "Fabrication only";
                }
            }
        }
    }

    public struct UnitStats
    {
        public string Name;
        public int Cost;
        public float BuildTime;
        public float Health;
        public float MoveSpeed;
        public float AttackRange;
        public float Damage;
        public float AttackCooldown;

        public UnitStats(string name, int cost, float buildTime, float health, float moveSpeed, float attackRange, float damage, float attackCooldown)
        {
            Name = name;
            Cost = cost;
            BuildTime = buildTime;
            Health = health;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            Damage = damage;
            AttackCooldown = attackCooldown;
        }
    }

    public struct StructureStats
    {
        public string Name;
        public int Cost;
        public float BuildTime;
        public float Health;
        public float FootprintRadius;
        public int PowerProvided;
        public int PowerUsed;

        public StructureStats(string name, int cost, float buildTime, float health, float footprintRadius, int powerProvided, int powerUsed)
        {
            Name = name;
            Cost = cost;
            BuildTime = buildTime;
            Health = health;
            FootprintRadius = footprintRadius;
            PowerProvided = powerProvided;
            PowerUsed = powerUsed;
        }
    }

    public sealed class ResourceBank
    {
        public int Credits { get; private set; }
        public int PowerProvided { get; private set; }
        public int PowerUsed { get; private set; }
        public bool HasLowPower => PowerUsed > PowerProvided;

        public ResourceBank(int startingCredits)
        {
            Credits = startingCredits;
        }

        public bool CanAfford(int amount)
        {
            return Credits >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            Credits -= amount;
            return true;
        }

        public void Add(int amount)
        {
            Credits += Mathf.Max(0, amount);
        }

        public void SetCreditsForRestore(int credits)
        {
            Credits = Mathf.Max(0, credits);
        }

        public void SetPower(int provided, int used)
        {
            PowerProvided = Mathf.Max(0, provided);
            PowerUsed = Mathf.Max(0, used);
        }
    }

    public static class RtsBalance
    {
        public const float MapHalfSize = 112f;
        public const float BuildRadius = 28f;

        public static UnitStats GetUnit(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.Grenadier:
                    return new UnitStats("Grenadier", 220, 3.6f, 72f, GetUnitMoveSpeed(UnitKind.Grenadier), 6.5f, 20f, 1.35f);
                case UnitKind.RocketSoldier:
                    return new UnitStats("Rocket Soldier", 320, 4.8f, 76f, GetUnitMoveSpeed(UnitKind.RocketSoldier), 11.5f, 34f, 1.75f);
                case UnitKind.FlameTrooper:
                    return new UnitStats("Flame Trooper", 260, 4.0f, 86f, GetUnitMoveSpeed(UnitKind.FlameTrooper), 4.2f, 12f, 0.5f);
                case UnitKind.Engineer:
                    return new UnitStats("Engineer", 300, 4.5f, 60f, GetUnitMoveSpeed(UnitKind.Engineer), 0f, 0f, 1f);
                case UnitKind.Harvester:
                    return new UnitStats("Harvester", 900, 9f, 180f, GetUnitMoveSpeed(UnitKind.Harvester), 0f, 0f, 1f);
                case UnitKind.Humvee:
                    return new UnitStats("Humvee", 520, 5.6f, 135f, GetUnitMoveSpeed(UnitKind.Humvee), 8.8f, 7f, 0.22f);
                case UnitKind.Apc:
                    return new UnitStats("APC", 720, 6.8f, 225f, GetUnitMoveSpeed(UnitKind.Apc), 8.6f, 12f, 0.34f);
                case UnitKind.LightTank:
                    return new UnitStats("Light Tank", 620, 6.2f, 170f, GetUnitMoveSpeed(UnitKind.LightTank), 8.4f, 22f, 0.85f);
                case UnitKind.MediumTank:
                case UnitKind.Tank:
                    return new UnitStats("Medium Tank", 860, 7.8f, 235f, GetUnitMoveSpeed(UnitKind.MediumTank), 9.2f, 31f, 1.05f);
                case UnitKind.HeavyTank:
                    return new UnitStats("Heavy Tank", 1250, 10.5f, 360f, GetUnitMoveSpeed(UnitKind.HeavyTank), 10.5f, 48f, 1.35f);
                case UnitKind.Skyraider:
                    return new UnitStats("Skyraider", 1250, 10.5f, 170f, GetUnitMoveSpeed(UnitKind.Skyraider), 8.8f, 24f, 0.7f);
                case UnitKind.OrcaLifter:
                    return new UnitStats("Orca Lifter", 1450, 12f, 280f, GetUnitMoveSpeed(UnitKind.OrcaLifter), 7.2f, 18f, 0.9f);
                default:
                    return new UnitStats("Gunner", 140, 2.4f, 68f, GetUnitMoveSpeed(UnitKind.Rifleman), 7f, 8f, 0.45f);
            }
        }

        public static float GetUnitMoveSpeed(UnitKind kind)
        {
            switch (NormalizeUnitKind(kind))
            {
                case UnitKind.Engineer:
                    return 4.25f;
                case UnitKind.Rifleman:
                    return 4.05f;
                case UnitKind.Grenadier:
                    return 3.75f;
                case UnitKind.RocketSoldier:
                    return 3.45f;
                case UnitKind.FlameTrooper:
                    return 3.25f;
                case UnitKind.Humvee:
                    return 6.55f;
                case UnitKind.Apc:
                    return 4.85f;
                case UnitKind.LightTank:
                    return 3.8f;
                case UnitKind.MediumTank:
                    return 3.05f;
                case UnitKind.HeavyTank:
                    return 2.28f;
                case UnitKind.Harvester:
                    return 2.85f;
                case UnitKind.Skyraider:
                    return 6.2f;
                case UnitKind.OrcaLifter:
                    return 4.5f;
                default:
                    return 4.05f;
            }
        }

        public static float GetUnitBuildTime(UnitKind kind)
        {
            UnitStats stats = GetUnit(kind);
            return Mathf.Round(Mathf.Clamp(1.2f + stats.Cost / 135f, 2.2f, 11.5f) * 10f) / 10f;
        }

        public static UnitKind NormalizeUnitKind(UnitKind kind)
        {
            return kind == UnitKind.Tank ? UnitKind.MediumTank : kind;
        }

        public static bool IsTank(UnitKind kind)
        {
            UnitKind normalized = NormalizeUnitKind(kind);
            return normalized == UnitKind.LightTank || normalized == UnitKind.MediumTank || normalized == UnitKind.HeavyTank;
        }

        public static bool IsWheeledCombatVehicle(UnitKind kind)
        {
            UnitKind normalized = NormalizeUnitKind(kind);
            return normalized == UnitKind.Humvee || normalized == UnitKind.Apc;
        }

        public static bool IsVehicle(UnitKind kind)
        {
            UnitKind normalized = NormalizeUnitKind(kind);
            return normalized == UnitKind.Harvester || IsTank(normalized) || IsWheeledCombatVehicle(normalized);
        }

        public static bool IsAircraft(UnitKind kind)
        {
            UnitKind normalized = NormalizeUnitKind(kind);
            return normalized == UnitKind.Skyraider || normalized == UnitKind.OrcaLifter;
        }

        public static bool HasTurretedWeapon(UnitKind kind)
        {
            return IsTank(kind) || IsWheeledCombatVehicle(kind);
        }

        public static bool IsInfantry(UnitKind kind)
        {
            switch (NormalizeUnitKind(kind))
            {
                case UnitKind.Rifleman:
                case UnitKind.Grenadier:
                case UnitKind.RocketSoldier:
                case UnitKind.FlameTrooper:
                case UnitKind.Engineer:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsEngineer(UnitKind kind)
        {
            return NormalizeUnitKind(kind) == UnitKind.Engineer;
        }

        public static StructureStats GetStructure(StructureKind kind)
        {
            switch (kind)
            {
                case StructureKind.Refinery:
                    return new StructureStats("Refinery", 1400, 6f, 650f, 3.2f, 0, 5);
                case StructureKind.Barracks:
                    return new StructureStats("Barracks", 500, 4f, 420f, 2.2f, 0, 4);
                case StructureKind.WarFactory:
                    return new StructureStats("War Factory", 1600, 8f, 780f, 3.5f, 0, 8);
                case StructureKind.PowerPlant:
                    return new StructureStats("Power Plant", 300, 3f, 360f, 2.0f, 24, 0);
                case StructureKind.Turret:
                    return new StructureStats("Guard Turret", 600, 4.5f, 320f, 1.5f, 0, 6);
                case StructureKind.GunTower:
                    return new StructureStats("Gun Tower", 950, 6.5f, 460f, 1.9f, 0, 8);
                case StructureKind.AdvancedGunTower:
                    return new StructureStats("Advanced Gun Tower", 1400, 8.5f, 620f, 2.35f, 0, 12);
                case StructureKind.DualHelipad:
                    return new StructureStats("Dual Helipad", 1800, 7.5f, 720f, 4.4f, 0, 9);
                default:
                    return new StructureStats("Command Center", 2500, 0f, 1100f, 3.8f, 12, 0);
            }
        }

        public static Color TeamColor(RtsTeam team)
        {
            switch (team)
            {
                case RtsTeam.Enemy:
                    return new Color(0.86f, 0.18f, 0.12f);
                case RtsTeam.Neutral:
                    return new Color(0.62f, 0.58f, 0.48f);
                default:
                    return new Color(0.16f, 0.48f, 0.92f);
            }
        }
    }
}
