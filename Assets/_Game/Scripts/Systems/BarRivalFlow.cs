using System.Collections;
using UnityEngine;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Fase 9 story script for the "BarRival" scene, where the whole fight now happens
    /// (Passe 3a): 3 escalating enemy waves (poucos -> mais, de lados diferentes ->
    /// mais fortes) with a short crowd-cheer respite between each, then the Admin
    /// Rival boss rises from his "throne" with a taunt, fight, post-fight balloons,
    /// then GameState.WarFinished = true and a fade back to the Buteco (epilogue).
    /// GameState.BarRivalWaveIndex is updated as each wave starts, so a defeat + retry
    /// (BattleLives reloading this scene) resumes from the current wave instead of
    /// replaying already-cleared ones. Same timeout-guarded coroutine style as
    /// ButecoFlow/RuaFlow.
    /// </summary>
    public class BarRivalFlow : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerAttack playerAttack;

        [Header("UI")]
        [SerializeField] private ObjectiveUI objectiveUI;

        [Header("Waves (3 escalating waves before the boss)")]
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private Transform[] crowdCheerAnchors; // e.g. the allies, for the between-waves balloons
        [SerializeField] private float respiteBalloonSeconds = 2.4f;

        [Header("Boss")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform bossThronePoint;
        [SerializeField] private Transform pedro;

        [Header("Next scene")]
        [SerializeField] private string nextScenePath = "Buteco";
        [SerializeField] private float bossSpawnTimeout = 60f;

        private static readonly string[] CheerLines =
        {
            "Boa! Próxima leva!",
            "Segura a linha!",
            "É nóis!",
            "Vamo que vamo!"
        };

        private GameObject bossInstance;
        private Health bossHealth;

        private void Awake()
        {
            GameState.ApplyWeapon(playerAttack, GameState.ChosenWeapon);

            if (waveSpawner != null)
            {
                // Checkpoint resume: retrying after a KO reloads this scene, so pick up
                // from the wave already reached instead of forcing a full replay.
                waveSpawner.StartWaveIndex = Mathf.Max(GameState.BarRivalWaveIndex, 0);
            }
        }

        private void Start()
        {
            StartCoroutine(RunFlow());
        }

        private IEnumerator RunFlow()
        {
            objectiveUI?.SetObjective("Limpe o bar rival");

            if (waveSpawner != null)
            {
                bool waveDone = false;
                waveSpawner.OnAllWavesDone.AddListener(() => waveDone = true);
                waveSpawner.OnWaveStarted.AddListener(OnWaveStarted);
                waveSpawner.OnWaveCleared.AddListener(OnWaveCleared);
                waveSpawner.StartWaves();

                float elapsed = 0f;
                while (!waveDone && elapsed < bossSpawnTimeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            yield return SpawnBoss();
            yield return WaitForBossDefeat();
            yield return PostFightDialogue();

            GameState.WarFinished = true;
            SceneTransition.Load(nextScenePath);
        }

        private void OnWaveStarted()
        {
            if (waveSpawner == null)
            {
                return;
            }
            // 0-based checkpoint: CurrentWaveNumber is 1-based.
            GameState.BarRivalWaveIndex = Mathf.Max(waveSpawner.CurrentWaveNumber - 1, 0);
            objectiveUI?.SetObjective($"Onda {waveSpawner.CurrentWaveNumber} de {waveSpawner.TotalWaves}");
        }

        private void OnWaveCleared()
        {
            // Short breather with the crowd celebrating in balloons; purely cosmetic
            // (WaveSpawner's own delayBetweenWaves already pauses the next spawn), so a
            // missing anchor just no-ops instead of stalling the fight.
            if (crowdCheerAnchors == null || crowdCheerAnchors.Length == 0)
            {
                return;
            }
            foreach (Transform anchor in crowdCheerAnchors)
            {
                if (anchor != null && anchor.gameObject.activeInHierarchy)
                {
                    string line = CheerLines[Random.Range(0, CheerLines.Length)];
                    SpeechBubble.Say(anchor, line, respiteBalloonSeconds);
                }
            }
        }

        private IEnumerator SpawnBoss()
        {
            if (bossPrefab == null)
            {
                yield break;
            }

            Vector3 pos = bossThronePoint != null ? bossThronePoint.position : transform.position;
            bossInstance = Instantiate(bossPrefab, pos, Quaternion.identity);
            bossHealth = bossInstance.GetComponent<Health>();
            if (bossHealth == null)
            {
                Debug.LogWarning("[BarRivalFlow] Boss sem Health — a luta será pulada. Confira o bossPrefab.");
            }

            SpeechBubble.Say(bossInstance.transform, "Link com vírus? Aqui quem decide sou EU.", 2.6f);
            yield return new WaitForSeconds(2.6f);
            SpeechBubble.Say(bossInstance.transform, "sudo rm -rf Buteco.", 2.2f);
            yield return new WaitForSeconds(2.2f);

            objectiveUI?.SetObjective("Derrote o Admin Rival");
        }

        private IEnumerator WaitForBossDefeat()
        {
            // Anti-soft-lock: if the boss never spawned (no prefab wired), don't hang the story.
            float safetyTimeout = 600f;
            float elapsed = 0f;
            while (bossHealth != null && !bossHealth.IsDead && elapsed < safetyTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator PostFightDialogue()
        {
            Transform anchor = bossInstance != null ? bossInstance.transform : transform;
            SpeechBubble.Say(anchor, "Tá bom, tá bom... o link era de um evento de jam.", 3f);
            yield return new WaitForSeconds(3f);
            if (pedro != null)
            {
                SpeechBubble.Say(pedro, "EU DISSE.", 1.8f);
                yield return new WaitForSeconds(1.8f);
                SpeechBubble.Say(pedro, "Tá vendo? Era só um convite pra jam. Ninguém lê o link antes de gritar vírus.", 3.4f);
                yield return new WaitForSeconds(3.4f);
                SpeechBubble.Say(pedro, "Aqui no Buteco a regra é simples: antes de acusar, abre o link.", 3.2f);
                yield return new WaitForSeconds(3.2f);
                SpeechBubble.Say(pedro, "Agora bora pro Buteco. A primeira rodada é por conta deles.", 3.2f);
                yield return new WaitForSeconds(3.2f);
            }
        }
    }
}
