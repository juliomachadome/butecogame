using System.Collections;
using UnityEngine;
using ButecoDosDevs.Systems;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Ambient generic Buteco member (yellow shirt): wanders between waypoints, pauses
    /// to "look around", and occasionally drops a short dev-joke speech bubble prefixed
    /// with its nickname (e.g. "Zé do Commit: Quem mexeu na main?"). Purely cosmetic
    /// background flavour — no combat/Hurtbox involved, same spirit as NerfChaos but
    /// without the dart-throwing. Frozen while CutsceneMode.IsActive so it never talks
    /// or walks over a scripted story beat. A stuck/unreachable waypoint is bounded by
    /// walkTimeout, after which the member simply snaps to it (anti-soft-lock — this
    /// member can never hang the game, it's cosmetic only).
    /// </summary>
    public class GenericChatter : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string nickname = "Dev Anônimo";

        [Header("Waypoints (reuse the Nerf-safe ones: clear of walls/furniture)")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float arriveDistance = 0.15f;
        [SerializeField] private float walkTimeout = 6f;
        [SerializeField] private float pauseMin = 1.5f;
        [SerializeField] private float pauseMax = 4f;

        [Header("Chatter")]
        [SerializeField] private float chatChance = 0.6f;
        [SerializeField] private float chatDuration = 2.6f;

        [Header("Animação (folha Generic_Buteco_Sheet)")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] walkS;
        [SerializeField] private Sprite[] walkE;
        [SerializeField] private Sprite[] walkN;
        [SerializeField] private float walkFps = 9f;
        [SerializeField] private SpriteRenderer bodyRenderer;

        private static readonly string[] DevLines =
        {
            "Quem mexeu na main?",
            "E feature, nao bug.",
            "Deploy na sexta, confia.",
            "Funciona na minha maquina.",
            "Fiz o SaaS em 1 hora, falta so o cliente.",
            "Ja tentou desligar e ligar?",
            "Git push --force e fe.",
            "Isso ai e debito tecnico do futuro."
        };

        private Coroutine loop;

        private void OnEnable()
        {
            loop = StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            if (loop != null)
            {
                StopCoroutine(loop);
                loop = null;
            }
        }

        private IEnumerator Loop()
        {
            // Small random stagger so members don't all move/talk in lockstep.
            yield return new WaitForSeconds(Random.Range(0f, 2f));

            while (true)
            {
                if (CutsceneMode.IsActive || waypoints == null || waypoints.Length == 0)
                {
                    yield return null;
                    continue;
                }

                Transform target = waypoints[Random.Range(0, waypoints.Length)];
                yield return WalkTo(target);

                if (CutsceneMode.IsActive)
                {
                    continue;
                }

                yield return LookAround();

                if (!CutsceneMode.IsActive && Random.value < chatChance)
                {
                    SpeechBubble.Say(transform, nickname + ": " + DevLines[Random.Range(0, DevLines.Length)], chatDuration);
                }

                float pause = Random.Range(pauseMin, pauseMax);
                float waited = 0f;
                while (waited < pause)
                {
                    if (!CutsceneMode.IsActive)
                    {
                        waited += Time.deltaTime;
                    }
                    yield return null;
                }
            }
        }

        private IEnumerator WalkTo(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < walkTimeout)
            {
                if (target == null)
                {
                    yield break;
                }
                if (CutsceneMode.IsActive)
                {
                    yield return null;
                    continue;
                }

                Vector3 toTarget = target.position - transform.position;
                float dist = toTarget.magnitude;
                if (dist <= arriveDistance)
                {
                    yield break;
                }

                Vector3 dir = toTarget / Mathf.Max(dist, 0.0001f);
                transform.position += dir * moveSpeed * Time.deltaTime;
                AnimateWalk(dir);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Anti-soft-lock: an unreachable waypoint (edge case) just snaps instead of
            // hanging this member's loop forever.
            if (target != null)
            {
                transform.position = target.position;
            }
        }

        private void AnimateWalk(Vector2 dir)
        {
            if (bodyRenderer == null)
            {
                return;
            }
            Sprite[] frames;
            bool flip = false;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                frames = walkE;
                flip = dir.x < 0f;
            }
            else
            {
                frames = dir.y > 0f ? walkN : walkS;
            }
            if (frames == null || frames.Length == 0)
            {
                return;
            }
            int frame = (int)(Time.time * walkFps) % frames.Length;
            bodyRenderer.sprite = frames[frame];
            bodyRenderer.flipX = flip;
        }

        private IEnumerator LookAround()
        {
            if (bodyRenderer == null || walkE == null || walkE.Length == 0)
            {
                yield break;
            }

            bodyRenderer.sprite = walkE[0];
            bodyRenderer.flipX = false;
            yield return new WaitForSeconds(0.45f);
            bodyRenderer.flipX = true;
            yield return new WaitForSeconds(0.45f);
            bodyRenderer.flipX = false;

            if (idleSprite != null)
            {
                bodyRenderer.sprite = idleSprite;
            }
        }
    }
}
