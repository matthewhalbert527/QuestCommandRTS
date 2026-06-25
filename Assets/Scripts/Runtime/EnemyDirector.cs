using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class EnemyDirector : MonoBehaviour
    {
        private RtsGame game;
        private float nextWaveTime;
        private float nextIdleOrderTime;
        private int waveIndex;

        public void Initialize(RtsGame owner)
        {
            game = owner;
            nextWaveTime = game.Clock.SimulationTime + 22f;
            nextIdleOrderTime = game.Clock.SimulationTime + 3f;
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

                if (game.Clock.SimulationTime >= nextIdleOrderTime)
                {
                    nextIdleOrderTime = game.Clock.SimulationTime + 3.5f;
                    OrderIdleEnemies();
                }
            }
        }

        public RtsEnemyDirectorSaveData CaptureState()
        {
            return new RtsEnemyDirectorSaveData
            {
                waveIndex = waveIndex,
                nextWaveTime = nextWaveTime,
                nextIdleOrderTime = nextIdleOrderTime
            };
        }

        public void RestoreState(RtsEnemyDirectorSaveData data)
        {
            if (data == null)
            {
                nextWaveTime = game.Clock.SimulationTime + 22f;
                nextIdleOrderTime = game.Clock.SimulationTime + 3f;
                waveIndex = 0;
                return;
            }

            waveIndex = Mathf.Max(0, data.waveIndex);
            nextWaveTime = Mathf.Max(game.Clock.SimulationTime + 1f, data.nextWaveTime);
            nextIdleOrderTime = Mathf.Max(game.Clock.SimulationTime + 0.5f, data.nextIdleOrderTime);
        }

        private void SpawnWave()
        {
            waveIndex++;
            nextWaveTime = game.Clock.SimulationTime + Mathf.Max(18f, 42f - waveIndex * 1.5f);
            RtsEntity target = game.FindPlayerPrimaryTarget();

            if (target == null)
            {
                return;
            }

            Vector3 enemyBase = game.GetEnemyBaseCenter();
            int infantryCount = 2 + Mathf.Min(5, waveIndex);
            for (int i = 0; i < infantryCount; i++)
            {
                Vector3 spawn = enemyBase + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));
                RtsUnit unit = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, spawn);
                unit.IssueAttack(target);
            }

            if (waveIndex % 2 == 0)
            {
                RtsUnit tank = game.CreateUnit(RtsTeam.Enemy, UnitKind.Tank, enemyBase + new Vector3(Random.Range(-7f, 7f), 0f, Random.Range(-7f, 7f)));
                tank.IssueAttack(target);
            }

            game.SpawnFloatingText("Enemy wave", enemyBase + Vector3.up * 3f, new Color(1f, 0.5f, 0.35f));
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
    }
}
