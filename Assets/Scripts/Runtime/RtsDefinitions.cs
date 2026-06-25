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
        Engineer
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
        AdvancedGunTower
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
                    return new UnitStats("Grenadier", 220, 3.6f, 72f, 4.15f, 6.5f, 20f, 1.35f);
                case UnitKind.RocketSoldier:
                    return new UnitStats("Rocket Soldier", 320, 4.8f, 76f, 3.9f, 11.5f, 34f, 1.75f);
                case UnitKind.FlameTrooper:
                    return new UnitStats("Flame Trooper", 260, 4.0f, 86f, 3.75f, 4.2f, 12f, 0.5f);
                case UnitKind.Engineer:
                    return new UnitStats("Engineer", 300, 4.5f, 60f, 4.35f, 0f, 0f, 1f);
                case UnitKind.Harvester:
                    return new UnitStats("Harvester", 900, 9f, 180f, 3.4f, 0f, 0f, 1f);
                case UnitKind.LightTank:
                    return new UnitStats("Light Tank", 620, 6.2f, 170f, 3.75f, 8.4f, 22f, 0.85f);
                case UnitKind.MediumTank:
                case UnitKind.Tank:
                    return new UnitStats("Medium Tank", 860, 7.8f, 235f, 3.05f, 9.2f, 31f, 1.05f);
                case UnitKind.HeavyTank:
                    return new UnitStats("Heavy Tank", 1250, 10.5f, 360f, 2.35f, 10.5f, 48f, 1.35f);
                default:
                    return new UnitStats("Gunner", 140, 2.4f, 68f, 4.6f, 7f, 8f, 0.45f);
            }
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
