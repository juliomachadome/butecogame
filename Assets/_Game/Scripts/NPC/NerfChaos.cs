using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ButecoDosDevs.Player;
using ButecoDosDevs.Systems;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// "Guerra de Nerf" background chaos: a generic Buteco member that runs between
    /// waypoints and, every so often, lobs a harmless foam dart at another chaos member
    /// (who does a cartoon "ai!" + hop) or occasionally at the player (0 damage — a
    /// disguised dash tutorial: dodging with a dash while a dart is close earns a
    /// "Boa, novato!" balloon and a little Coragem). No physics/Hurtbox involved — this
    /// never touches the real combat system, it's cosmetic.
    ///
    /// Self-registers in a small static list (same lightweight pattern as
    /// CombatantRegistry) purely so members can find each other without GameObject.Find
    /// in Update; frozen/resumed in bulk by ButecoFlow via SetChaosActive.
    /// </summary>
    public class NerfChaos : MonoBehaviour
    {
        [Header("Waypoints (placed clear of walls/furniture)")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float arriveDistance = 0.15f;

        [Header("Dart throwing")]
        [SerializeField] private Sprite dartSprite;
        [SerializeField] private float throwIntervalMin = 2.5f;
        [SerializeField] private float throwIntervalMax = 5f;
        [SerializeField] private float dartTravelTime = 0.35f;
        [SerializeField] private float dartScale = 0.12f;
        [SerializeField] private Color dartTint = new Color(1f, 0.55f, 0.15f, 1f);

        [Header("Player-tutorial dart (dodge-with-dash)")]
        [SerializeField, Range(0f, 1f)] private float chanceAtPlayer = 0.25f;
        [SerializeField] private float dodgeWindowFraction = 0.4f; // last X% of the flight checks IsDashing

        [Header("Reaction")]
        [SerializeField] private float hopHeight = 0.25f;
        [SerializeField] private float hopDuration = 0.25f;

        [Header("Animação (folha Generic_Buteco_Sheet)")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] walkS;
        [SerializeField] private Sprite[] walkE;
        [SerializeField] private Sprite[] walkN;
        [SerializeField] private float walkFps = 9f;

        [Header("Visual variation (tint skin/shirt slightly per instance)")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Color[] tintVariants;

        private static readonly List<NerfChaos> Members = new List<NerfChaos>();

        private Transform currentWaypoint;
        private bool chaosActive = true;
        private Coroutine throwLoop;

        private static Transform playerTransform;
        private static PlayerMovement playerMovement;
        private static CourageMeter playerCourage;

        private void Awake()
        {
            if (tintVariants != null && tintVariants.Length > 0 && bodyRenderer != null)
            {
                bodyRenderer.color = tintVariants[Random.Range(0, tintVariants.Length)];
            }

            if (playerTransform == null)
            {
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    playerTransform = playerGo.transform;
                    playerMovement = playerGo.GetComponent<PlayerMovement>();
                    playerCourage = playerGo.GetComponent<CourageMeter>();
                }
            }
        }

        private void OnEnable()
        {
            if (!Members.Contains(this))
            {
                Members.Add(this);
            }
            PickNextWaypoint();
            throwLoop = StartCoroutine(ThrowLoop());
        }

        private void OnDisable()
        {
            Members.Remove(this);
            if (throwLoop != null)
            {
                StopCoroutine(throwLoop);
                throwLoop = null;
            }
        }

        private void Update()
        {
            if (!chaosActive || waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            if (currentWaypoint == null)
            {
                PickNextWaypoint();
                if (currentWaypoint == null)
                {
                    return;
                }
            }

            Vector3 toTarget = currentWaypoint.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist <= arriveDistance)
            {
                PickNextWaypoint();
                return;
            }

            transform.position += toTarget.normalized * moveSpeed * Time.deltaTime;
            AnimateWalk(toTarget);
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

        private void LateUpdate()
        {
            if (!chaosActive && bodyRenderer != null && idleSprite != null)
            {
                bodyRenderer.sprite = idleSprite;
                bodyRenderer.flipX = false;
            }
        }

        private void PickNextWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                currentWaypoint = null;
                return;
            }
            currentWaypoint = waypoints[Random.Range(0, waypoints.Length)];
        }

        /// <summary>Called by ButecoFlow to freeze/resume the whole chaos loop (e.g. while Pedro yells "OPA!").</summary>
        public void SetChaosActive(bool active)
        {
            chaosActive = active;
        }

        public static void SetAllActive(bool active)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i] != null)
                {
                    Members[i].SetChaosActive(active);
                }
            }
        }

        /// <summary>Finds the closest chaos member to a point within maxDistance, or null.
        /// Used by the player's toy Nerf gun (PlayerNerf) to resolve which member a fired
        /// dart "hits" without needing a Collider2D on every wandering member.</summary>
        public static NerfChaos FindNearest(Vector3 point, float maxDistance)
        {
            NerfChaos best = null;
            float bestDist = maxDistance;
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i] == null)
                {
                    continue;
                }
                float dist = Vector2.Distance(Members[i].transform.position, point);
                if (dist <= bestDist)
                {
                    bestDist = dist;
                    best = Members[i];
                }
            }
            return best;
        }

        private IEnumerator ThrowLoop()
        {
            // Small random stagger so members in the same scene don't throw in lockstep.
            yield return new WaitForSeconds(Random.Range(0.2f, throwIntervalMax));

            while (true)
            {
                float wait = Random.Range(throwIntervalMin, throwIntervalMax);
                yield return new WaitForSeconds(wait);

                if (!chaosActive || dartSprite == null)
                {
                    continue;
                }

                bool throwAtPlayer = playerTransform != null && Random.value < chanceAtPlayer;
                if (throwAtPlayer)
                {
                    yield return ThrowDartAtPlayer();
                }
                else
                {
                    NerfChaos target = PickOtherMember();
                    if (target != null)
                    {
                        yield return ThrowDartAt(target.transform, target);
                    }
                }
            }
        }

        private NerfChaos PickOtherMember()
        {
            if (Members.Count <= 1)
            {
                return null;
            }
            for (int attempt = 0; attempt < 4; attempt++)
            {
                NerfChaos candidate = Members[Random.Range(0, Members.Count)];
                if (candidate != null && candidate != this)
                {
                    return candidate;
                }
            }
            return null;
        }

        private IEnumerator ThrowDartAt(Transform target, NerfChaos targetChaos)
        {
            GameObject dart = SpawnDartWithSfx();
            Vector3 from = transform.position;
            float t = 0f;
            while (t < dartTravelTime)
            {
                if (dart == null || target == null)
                {
                    if (dart != null) Destroy(dart);
                    yield break;
                }
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / dartTravelTime);
                dart.transform.position = Vector3.Lerp(from, target.position, t01);
                yield return null;
            }

            if (dart != null)
            {
                Destroy(dart);
            }
            if (targetChaos != null)
            {
                targetChaos.ReactHit();
            }
        }

        private GameObject SpawnDartWithSfx()
        {
            Sfx.Play(SoundId.DardoNerf, transform.position);
            return SpawnDart();
        }

        private IEnumerator ThrowDartAtPlayer()
        {
            if (playerTransform == null)
            {
                yield break;
            }

            GameObject dart = SpawnDartWithSfx();
            Vector3 from = transform.position;
            Vector3 to = playerTransform.position;
            bool dodged = false;
            float dodgeStartFraction = 1f - dodgeWindowFraction;

            float t = 0f;
            while (t < dartTravelTime)
            {
                if (dart == null)
                {
                    yield break;
                }
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / dartTravelTime);
                dart.transform.position = Vector3.Lerp(from, to, t01);

                if (t01 >= dodgeStartFraction && playerMovement != null && playerMovement.IsDashing)
                {
                    dodged = true;
                }
                yield return null;
            }

            if (dart != null)
            {
                Destroy(dart);
            }

            // 0 damage always — this is the disguised dash tutorial, not real combat.
            if (dodged)
            {
                SpeechBubble.Say(transform, "Boa, novato!", 2f);
                if (playerCourage != null)
                {
                    playerCourage.Add(3f);
                }
            }
            else
            {
                Sfx.Play(SoundId.DardoAcerta, to);
            }
        }

        private GameObject SpawnDart()
        {
            GameObject go = new GameObject("FX_NerfDart");
            go.transform.position = transform.position;
            go.transform.localScale = Vector3.one * dartScale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dartSprite;
            sr.color = dartTint;
            sr.sortingOrder = 25;
            return go;
        }

        /// <summary>Cartoon "ai!" reaction: a balloon plus a quick hop. Called when a dart lands on this member.</summary>
        public void ReactHit()
        {
            SpeechBubble.Say(transform, "Ai!", 1.2f);
            Sfx.Play(SoundId.DardoAcerta, transform.position);
            StartCoroutine(HopRoutine());
        }

        private IEnumerator HopRoutine()
        {
            Vector3 startPos = transform.position;
            float t = 0f;
            while (t < hopDuration)
            {
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / hopDuration);
                float height = Mathf.Sin(t01 * Mathf.PI) * hopHeight;
                transform.position = startPos + Vector3.up * height;
                yield return null;
            }
            transform.position = startPos;
        }
    }
}
