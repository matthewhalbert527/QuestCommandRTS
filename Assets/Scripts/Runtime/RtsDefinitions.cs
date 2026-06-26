using UnityEngine;

namespace QuestCommandRTS
{
    public enum RtsTeam
    {
        Player,
        Enemy,
        Neutral
    }

    public enum UnitKind
    {
        Rifleman,
        Harvester,
        Tank
    }

    public enum StructureKind
    {
        CommandCenter,
        Refinery,
        Barracks,
        WarFactory,
        PowerPlant,
        Turret
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

        public void SetPower(int provided, int used)
        {
            PowerProvided = Mathf.Max(0, provided);
            PowerUsed = Mathf.Max(0, used);
        }
    }

    public static class RtsBalance
    {
        public const float MapHalfSize = 34f;
        public const float BuildRadius = 16f;

        public static UnitStats GetUnit(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.Harvester:
                    return new UnitStats("Harvester", 900, 9f, 180f, 3.4f, 0f, 0f, 1f);
                case UnitKind.Tank:
                    return new UnitStats("Battle Tank", 800, 7.5f, 230f, 3.0f, 9f, 32f, 1.1f);
                default:
                    return new UnitStats("Rifleman", 120, 2.2f, 65f, 4.6f, 7f, 9f, 0.7f);
            }
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
