using System.Collections;
using UnityEngine;
using ButecoDosDevs.Player;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Fase 8 story script for the "Rua" scene: opening walk-out (input disabled) ->
    /// "A GUERRA PÚNICA" title card -> control back -> 3 enemy waves from the rival
    /// bar's door -> objective flips to "entre no bar rival" -> door trigger loads
    /// BarRival. Every wait has a timeout/fallback (anti-soft-lock), matching
    /// ButecoFlow's pattern.
    /// </summary>
    public class RuaFlow : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private Transform playerTransform;

        [Header("Group (walks out of the Buteco door at the start)")]
        [SerializeField] private Transform[] groupMembers; // allies only; player walks too but via its own transform
        [SerializeField] private Vector3 walkTargetOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] private Transform walkTargetPoint; // e.g. "MidStreetPoint"
        [SerializeField] private float groupWalkSpeed = 3f;
        [SerializeField] private float groupWalkTimeout = 6f;

        [Header("UI")]
        [SerializeField] private ObjectiveUI objectiveUI;

        [Header("Waves")]
        [SerializeField] private WaveSpawner waveSpawner;

        [Header("Next scene")]
        [SerializeField] private string nextScenePath = "BarRival";
        [SerializeField] private Transform rivalDoorPoint;
        [SerializeField] private float doorEnterRadius = 1f;

        private bool doorTriggered;
        private bool wavesDone;

        private void Awake()
        {
            GameState.ApplyWeapon(playerAttack, GameState.ChosenWeapon);

            if (playerMovement != null) playerMovement.enabled = false;
            if (playerAttack != null) playerAttack.enabled = false;
        }

        private void Start()
        {
            StartCoroutine(RunFlow());
        }

        private IEnumerator RunFlow()
        {
            objectiveUI?.SetObjective("...");

            yield return WalkGroupOut();

            yield return TitleCard.Show(this, "A GUERRA PÚNICA", 2.2f, 0.5f);

            if (playerMovement != null) playerMovement.enabled = true;
            if (playerAttack != null) playerAttack.enabled = true;

            objectiveUI?.SetObjective("Vença os rivais na rua");

            if (waveSpawner != null)
            {
                waveSpawner.OnAllWavesDone.AddListener(OnWavesDone);
                waveSpawner.StartWaves();
            }
            else
            {
                // No spawner wired: don't soft-lock the story, just move straight on.
                OnWavesDone();
            }
        }

        private IEnumerator WalkGroupOut()
        {
            Vector3 target = walkTargetPoint != null ? walkTargetPoint.position : (playerTransform != null ? playerTransform.position + Vector3.right * 4f : Vector3.zero);
            float elapsed = 0f;

            while (elapsed < groupWalkTimeout)
            {
                bool anyStillMoving = false;

                if (playerTransform != null && Vector2.Distance(playerTransform.position, target) > 0.15f)
                {
                    playerTransform.position = Vector3.MoveTowards(playerTransform.position, target, groupWalkSpeed * Time.deltaTime);
                    anyStillMoving = true;
                }

                if (groupMembers != null)
                {
                    foreach (Transform member in groupMembers)
                    {
                        if (member == null) continue;
                        Vector3 memberTarget = target + (member.position - (playerTransform != null ? playerTransform.position : target)).normalized * 1.2f;
                        if (Vector2.Distance(member.position, memberTarget) > 0.15f)
                        {
                            member.position = Vector3.MoveTowards(member.position, memberTarget, groupWalkSpeed * Time.deltaTime);
                            anyStillMoving = true;
                        }
                    }
                }

                if (!anyStillMoving)
                {
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void OnWavesDone()
        {
            objectiveUI?.SetObjective("Entre no bar rival");
            wavesDone = true;
        }

        private void Update()
        {
            if (doorTriggered || !wavesDone || rivalDoorPoint == null || playerTransform == null)
            {
                return;
            }

            if (Vector2.Distance(playerTransform.position, rivalDoorPoint.position) <= doorEnterRadius)
            {
                doorTriggered = true;
                Sfx.Play(SoundId.PortaAbre, rivalDoorPoint.position);
                SceneTransition.Load(nextScenePath);
            }
        }
    }
}
