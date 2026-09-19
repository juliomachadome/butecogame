using System.Collections;
using UnityEngine;
using ButecoDosDevs.NPC;
using ButecoDosDevs.Player;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Fase 8 story script for the "Rua" scene (Passe 3a rewrite): the group walks out
    /// of the Buteco and crosses the street in formation -> camera pulls back to show
    /// both bars -> "A GUERRA PÚNICA" title card -> control back, no street battle
    /// anymore (that all moved to BarRival) -> objective becomes "invade the rival bar
    /// with the group" with a prompt over its door -> [E] on that door (SceneDoor,
    /// unlocked here) loads BarRival.
    ///
    /// The walk-out no longer hand-animates each ally's Transform (that produced a
    /// "clump" and no walk animation, since CharacterSpriteAnimator reads Rigidbody2D
    /// velocity, not raw position deltas). Instead it only drives the PLAYER's
    /// Rigidbody2D velocity toward the target; every Ally_* already in the scene keeps
    /// its own AllyController.Follow formation slot (backDistance/sideOffset per ally,
    /// set in the Inspector) and walks there on its own via rb.linearVelocity, which
    /// animates correctly and naturally staggers/spaces the group into a column instead
    /// of a blob. Every wait has a timeout/fallback (anti-soft-lock), matching
    /// ButecoFlow's pattern.
    /// </summary>
    public class RuaFlow : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Rigidbody2D playerRb;

        [Header("Walk-out (drives only the player; allies formation-follow on their own)")]
        [SerializeField] private Transform walkTargetPoint; // e.g. "MidStreetPoint"
        [SerializeField] private float groupWalkSpeed = 3f;
        [SerializeField] private float groupWalkTimeout = 8f;
        [SerializeField] private float arrivalDistance = 0.2f;

        [Header("UI")]
        [SerializeField] private ObjectiveUI objectiveUI;

        [Header("Camera (pulls back to show both bars during the crossing)")]
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private Transform bothBarsFocusPoint; // e.g. MidStreetPoint, between the two facades
        [SerializeField] private float bothBarsOrthoSize = 7f;

        [Header("Rival door (unlocked once the title finishes)")]
        [SerializeField] private SceneDoor rivalDoor;
        [SerializeField] private InteractPrompt rivalDoorPrompt;
        [SerializeField] private string rivalDoorLabelLocked = "[E] Trancado";
        [SerializeField] private string rivalDoorLabelReady = "[E] Invadir com a tropa";

        private void Awake()
        {
            GameState.ApplyWeapon(playerAttack, GameState.ChosenWeapon);

            if (playerRb == null && playerTransform != null)
            {
                playerRb = playerTransform.GetComponent<Rigidbody2D>();
            }

            // A "passeio" street visit (Fase 5, before the war) skips the walk-out/title
            // entirely: the player just came in on their own two feet and should stay
            // in full control.
            if (!GameState.StreetVisit)
            {
                if (playerMovement != null) playerMovement.enabled = false;
                if (playerAttack != null) playerAttack.enabled = false;
            }

            if (rivalDoor != null)
            {
                // Locked by default (Inspector) during war mode until the crossing
                // finishes; a street-visit trip leaves it exactly as authored (usually
                // still locked, since the rival bar opens only after the war).
                if (rivalDoorPrompt != null)
                {
                    rivalDoorPrompt.SetLabel(rivalDoorLabelLocked);
                }
            }
        }

        private void Start()
        {
            StartCoroutine(RunFlow());
        }

        private IEnumerator RunFlow()
        {
            if (GameState.StreetVisit)
            {
                objectiveUI?.SetObjective("Modo passeio. Volte pro Buteco quando quiser.");
                yield break;
            }

            objectiveUI?.SetObjective("...");

            yield return WalkPlayerOut();

            if (cameraFollow != null)
            {
                Vector3 focusPoint = bothBarsFocusPoint != null ? bothBarsFocusPoint.position : (playerTransform != null ? playerTransform.position : transform.position);
                cameraFollow.BeginFocus(focusPoint, bothBarsOrthoSize, 0.6f);
            }

            yield return TitleCard.Show(this, "A GUERRA PÚNICA", 2.2f, 0.5f);

            if (cameraFollow != null)
            {
                cameraFollow.EndFocus(0.6f);
            }

            if (playerMovement != null) playerMovement.enabled = true;
            if (playerAttack != null) playerAttack.enabled = true;

            objectiveUI?.SetObjective("Entre no bar inimigo com sua tropa");

            if (rivalDoor != null)
            {
                rivalDoor.SetLocked(false);
            }
            if (rivalDoorPrompt != null)
            {
                rivalDoorPrompt.SetLabel(rivalDoorLabelReady);
            }
        }

        /// <summary>
        /// Drives the player's Rigidbody2D toward walkTargetPoint at groupWalkSpeed so
        /// CharacterSpriteAnimator-driven allies can formation-follow with correct walk
        /// animation. Bounded by groupWalkTimeout (anti-soft-lock): if the player never
        /// gets within arrivalDistance in time, velocity is simply zeroed and the flow
        /// moves on rather than stalling forever.
        /// </summary>
        private IEnumerator WalkPlayerOut()
        {
            if (playerRb == null || playerTransform == null || walkTargetPoint == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < groupWalkTimeout)
            {
                Vector2 toTarget = (Vector2)walkTargetPoint.position - (Vector2)playerTransform.position;
                float dist = toTarget.magnitude;
                if (dist <= arrivalDistance)
                {
                    break;
                }

                playerRb.linearVelocity = (toTarget / Mathf.Max(dist, 0.0001f)) * groupWalkSpeed;

                elapsed += Time.deltaTime;
                yield return null;
            }

            playerRb.linearVelocity = Vector2.zero;
            // Snap the player exactly on the mark so downstream logic (camera focus
            // point, door distances) can rely on the position, same pattern as
            // ButecoFlow.WalkTo's timeout snap.
            playerTransform.position = walkTargetPoint.position;
        }
    }
}
