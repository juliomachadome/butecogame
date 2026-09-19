using System.Collections;
using UnityEngine;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Fase 9 story script for the "BarRival" scene: 1 enemy wave, then the Admin Rival
    /// boss rises from his "throne" with a taunt, fight, post-fight balloons, then
    /// GameState.WarFinished = true and a fade back to the Buteco (epilogue). Same
    /// timeout-guarded coroutine style as ButecoFlow/RuaFlow.
    /// </summary>
    public class BarRivalFlow : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerAttack playerAttack;

        [Header("UI")]
        [SerializeField] private ObjectiveUI objectiveUI;

        [Header("Waves (1 wave before the boss)")]
        [SerializeField] private WaveSpawner waveSpawner;

        [Header("Boss")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform bossThronePoint;
        [SerializeField] private Transform pedro;

        [Header("Next scene")]
        [SerializeField] private string nextScenePath = "Buteco";
        [SerializeField] private float bossSpawnTimeout = 60f;

        private GameObject bossInstance;
        private Health bossHealth;

        private void Awake()
        {
            GameState.ApplyWeapon(playerAttack, GameState.ChosenWeapon);
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
                SpeechBubble.Say(pedro, "Tiradentes lutou pela liberdade. Hoje a gente lutou pela verdade.", 3.4f);
                yield return new WaitForSeconds(3.4f);
                SpeechBubble.Say(pedro, "O Buteco é livre. E aqui ninguém é julgado sem provas.", 3.2f);
                yield return new WaitForSeconds(3.2f);
                SpeechBubble.Say(pedro, "Liberdade ainda que tardia... mas a cerveja não. Bora pro Buteco!", 3.2f);
                yield return new WaitForSeconds(3.2f);
            }
        }
    }
}
