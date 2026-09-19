using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using ButecoDosDevs.Combat;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Spawns enemy waves at one or more points (round-robin), waiting for every
    /// Team.Enemy in CombatantRegistry to be dead before starting the next wave.
    /// Call StartWaves() once (e.g. from RuaFlow/BarRivalFlow). Each wave's wait has a
    /// timeout fallback so a stray enemy that never dies (bug, edge case) can never
    /// stall the story forever — it just moves on.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int[] waveSizes = { 3, 4, 5 };
        [SerializeField] private float delayBetweenSpawns = 0.35f;
        [SerializeField] private float delayBeforeFirstWave = 1f;
        [SerializeField] private float perWaveTimeout = 90f;
        [SerializeField] private UnityEvent onAllWavesDone = new UnityEvent();
        [SerializeField] private UnityEvent onWaveStarted = new UnityEvent();

        public UnityEvent OnAllWavesDone => onAllWavesDone;
        public UnityEvent OnWaveStarted => onWaveStarted;

        private bool started;

        public void StartWaves()
        {
            if (started)
            {
                return;
            }
            started = true;
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            yield return new WaitForSeconds(delayBeforeFirstWave);

            for (int w = 0; w < waveSizes.Length; w++)
            {
                onWaveStarted?.Invoke();
                yield return SpawnWave(waveSizes[w]);
                yield return WaitForWaveClear();
            }

            onAllWavesDone?.Invoke();
        }

        private IEnumerator SpawnWave(int count)
        {
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                yield break;
            }

            for (int i = 0; i < count; i++)
            {
                Transform point = spawnPoints[i % spawnPoints.Length];
                Instantiate(enemyPrefab, point.position, Quaternion.identity);
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }

        private IEnumerator WaitForWaveClear()
        {
            float elapsed = 0f;
            // A frame of grace so just-spawned enemies register with CombatantRegistry
            // before we check the list (Combatant registers in OnEnable, same frame it
            // spawns, but this keeps the check robust either way).
            yield return null;

            while (elapsed < perWaveTimeout)
            {
                if (!AnyEnemyAlive())
                {
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private static bool AnyEnemyAlive()
        {
            var enemies = CombatantRegistry.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Health h = enemies[i];
                if (h != null && !h.IsDead)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
