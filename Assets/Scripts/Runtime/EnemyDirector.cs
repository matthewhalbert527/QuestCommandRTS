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
            nextWaveTime = Time.time + 22f;
            nextIdleOrderTime = Time.time + 3f;
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            if (Time.time >= nextWaveTime)
            {
                SpawnWave();
            }

            if (Time.time >= nextIdleOrderTime)
            {
                nextIdleOrderTime = Time.time + 3.5f;
                OrderIdleEnemies();
            }
        }

        private void SpawnWave()
        {
            waveIndex++;
            nextWaveTime = Time.time + Mathf.Max(18f, 42f - waveIndex * 1.5f);
            RtsEntity target = game.FindPlayerPrimaryTarget();

            if (target == null)
            {
                return;
            }

            int infantryCount = 2 + Mathf.Min(5, waveIndex);
            for (int i = 0; i < infantryCount; i++)
            {
                Vector3 spawn = new Vector3(20f + Random.Range(-3f, 3f), 0f, 15f + Random.Range(-3f, 3f));
                RtsUnit unit = game.CreateUnit(RtsTeam.Enemy, UnitKind.Rifleman, spawn);
                unit.IssueAttack(target);
            }

            if (waveIndex % 2 == 0)
            {
                RtsUnit tank = game.CreateUnit(RtsTeam.Enemy, UnitKind.Tank, new Vector3(23f + Random.Range(-2f, 2f), 0f, 12f + Random.Range(-2f, 2f)));
                tank.IssueAttack(target);
            }

            if (waveIndex % 3 == 0)
            {
                RtsUnit skyraider = game.CreateUnit(RtsTeam.Enemy, UnitKind.Skyraider, new Vector3(23f + Random.Range(-3f, 3f), 0f, 20f + Random.Range(-2f, 2f)));
                skyraider.IssueAttack(target);
            }

            if (waveIndex % 5 == 0)
            {
                RtsUnit lifter = game.CreateUnit(RtsTeam.Enemy, UnitKind.OrcaLifter, new Vector3(27f + Random.Range(-2f, 2f), 0f, 20f + Random.Range(-2f, 2f)));
                lifter.IssueAttack(target);
            }

            game.SpawnFloatingText("Enemy wave", new Vector3(20f, 0f, 16f) + Vector3.up * 3f, new Color(1f, 0.5f, 0.35f));
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
