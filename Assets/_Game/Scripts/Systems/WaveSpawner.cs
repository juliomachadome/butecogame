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
        [Tooltip("Optional stronger prefab used for the LAST wave only (e.g. wave 3's tougher rivals). Falls back to enemyPrefab if left empty.")]
        [SerializeField] private GameObject strongEnemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int[] waveSizes = { 3, 4, 5 };
        [SerializeField] private float delayBetweenSpawns = 0.35f;
        [SerializeField] private float delayBeforeFirstWave = 1f;
        [Tooltip("Pause between waves (after a wave clears) so the group can breathe/celebrate before the next one starts.")]
        [SerializeField] private float delayBetweenWaves = 0.5f;
        [SerializeField] private float perWaveTimeout = 90f;
        [SerializeField] private UnityEvent onAllWavesDone = new UnityEvent();
        [SerializeField] private UnityEvent onWaveStarted = new UnityEvent();
        [SerializeField] private UnityEvent onWaveCleared = new UnityEvent();

        public UnityEvent OnAllWavesDone => onAllWavesDone;
        public UnityEvent OnWaveStarted => onWaveStarted;
        public UnityEvent OnWaveCleared => onWaveCleared;

        /// <summary>1-based index of the wave currently running/just started (0 before StartWaves).
        /// Read by BarRivalFlow for the "onda X de N" respite balloon and by GameState checkpoint bookkeeping.</summary>
        public int CurrentWaveNumber { get; private set; }
        public int TotalWaves => waveSizes != null ? waveSizes.Length : 0;

        /// <summary>Wave index (0-based) to start from; set before StartWaves() to resume
        /// past already-cleared waves (anti-soft-lock: a mid-battle checkpoint retry
        /// shouldn't force the player through waves they already cleared).</summary>
        public int StartWaveIndex { get; set; }

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

            int startIndex = Mathf.Clamp(StartWaveIndex, 0, waveSizes.Length);
            for (int w = startIndex; w < waveSizes.Length; w++)
            {
                CurrentWaveNumber = w + 1;
                onWaveStarted?.Invoke();
                bool isLastWave = w == waveSizes.Length - 1;
                yield return SpawnWave(waveSizes[w], isLastWave);
                yield return WaitForWaveClear();
                onWaveCleared?.Invoke();
                if (!isLastWave)
                {
                    yield return new WaitForSeconds(delayBetweenWaves);
                }
            }

            onAllWavesDone?.Invoke();
        }

        private IEnumerator SpawnWave(int count, bool useStrongPrefab)
        {
            GameObject prefabToUse = useStrongPrefab && strongEnemyPrefab != null ? strongEnemyPrefab : enemyPrefab;
            if (prefabToUse == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                yield break;
            }

            for (int i = 0; i < count; i++)
            {
                // Round-robin across all spawn points so a multi-point setup spreads
                // enemies across different sides instead of clumping at one door.
                Transform point = spawnPoints[i % spawnPoints.Length];
                Instantiate(prefabToUse, point.position, Quaternion.identity);
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
