using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestCommandRTS
{
    [Serializable]
    public sealed class RtsSaveEnvelope
    {
        public int schemaVersion;
        public string gameVersion;
        public string createdUtc;
        public string slotId;
        public string checksum;
        public string payloadJson;
    }

    [Serializable]
    public sealed class RtsMatchSaveData
    {
        public int schemaVersion = 1;
        public string applicationVersion;
        public string saveSlotId;
        public string skirmishConfigId = "default_skirmish_v1";
        public string difficultyId = "standard";
        public string mapId = "room_tabletop_v1";
        public int mapSeed = 527;
        public string savedUtc;
        public float matchTime;
        public string matchState;
        public string statusMessage;
        public int nextEntityId;
        public int nextResourceNodeId;
        public RtsSkirmishOptions skirmishOptions = RtsSkirmishOptions.CreateDefault();
        public RtsResourceBankSaveData resources = new RtsResourceBankSaveData();
        public List<RtsEntitySaveData> entities = new List<RtsEntitySaveData>();
        public List<RtsResourceNodeSaveData> resourceNodes = new List<RtsResourceNodeSaveData>();
        public RtsFogSaveData fog = new RtsFogSaveData();
        public RtsBuildPlacementSaveData buildPlacement = new RtsBuildPlacementSaveData();
        public RtsEnemyDirectorSaveData enemyDirector = new RtsEnemyDirectorSaveData();
    }

    [Serializable]
    public sealed class RtsSaveMetadata
    {
        public string slotId;
        public int schemaVersion;
        public string gameVersion;
        public string createdUtc;
        public string applicationVersion;
        public string skirmishConfigId;
        public string difficultyId;
        public string mapId;
        public int mapSeed;
        public string savedUtc;
        public float matchTime;
        public string matchState;
        public string statusMessage;
        public int playerCredits;
        public int entityCount;
        public int resourceNodeCount;
        public bool readFromBackup;
    }

    [Serializable]
    public sealed class RtsResourceBankSaveData
    {
        public int credits;
        public int powerProvided;
        public int powerUsed;
    }

    [Serializable]
    public sealed class RtsEntitySaveData
    {
        public int id;
        public string entityType;
        public string team;
        public string unitKind;
        public string structureKind;
        public Vector3Data position;
        public float rotationY;
        public float health;
        public float maxHealth;
        public int carriedRiflemen;
        public string carriedInfantryKind;
        public RtsUnitOrderSaveData order;
        public RtsHarvesterSaveData harvester;
        public RtsProductionSaveData production;
    }

    [Serializable]
    public sealed class RtsUnitOrderSaveData
    {
        public string orderType;
        public Vector3Data destination;
        public int targetEntityId;
    }

    [Serializable]
    public sealed class RtsHarvesterSaveData
    {
        public int state;
        public int cargo;
        public int targetResourceNodeId;
        public int homeRefineryEntityId;
        public Vector3Data productionExitPoint;
        public float harvestAccumulator;
    }

    [Serializable]
    public sealed class RtsProductionSaveData
    {
        public bool hasActiveProduction;
        public string activeKind;
        public float activeRemaining;
        public float activeDuration;
        public List<string> queue = new List<string>();
        public bool hasRallyPoint;
        public Vector3Data rallyPoint;
    }

    [Serializable]
    public sealed class RtsResourceNodeSaveData
    {
        public int id;
        public Vector3Data position;
        public int amount;
        public int maxAmount;
    }

    [Serializable]
    public sealed class RtsFogSaveData
    {
        public int gridSize;
        public bool[] explored;
    }

    [Serializable]
    public sealed class RtsBuildPlacementSaveData
    {
        public bool isPlacing;
        public string structureKind;
        public bool hasPlacementPoint;
        public Vector3Data placementPoint;
        public List<RtsPendingConstructionSaveData> constructions = new List<RtsPendingConstructionSaveData>();
    }

    [Serializable]
    public sealed class RtsPendingConstructionSaveData
    {
        public string team;
        public string structureKind;
        public Vector3Data position;
        public float remaining;
        public float duration;
    }

    [Serializable]
    public sealed class RtsEnemyDirectorSaveData
    {
        public bool hasEconomyState;
        public int waveIndex;
        public int enemyCredits;
        public float nextWaveTime;
        public float nextIdleOrderTime;
        public float nextIncomeTime;
        public float nextBuildTime;
        public float nextProductionTime;
    }

    [Serializable]
    public struct Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
