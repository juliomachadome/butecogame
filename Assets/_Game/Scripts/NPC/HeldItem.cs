using System.Collections;
using UnityEngine;
using ButecoDosDevs.Player;
using ButecoDosDevs.Systems;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// A sprite held in a character's hand (cigarette, sword, ...): a child transform
    /// that repositions/re-sorts itself per facing direction, reading the current
    /// facing off whichever sprite animator is wired (CharacterSpriteAnimator /
    /// NPCSprite / PlayerSpriteAnimator — exactly one should be assigned). Mirrors in
    /// West; sits behind the body sprite when facing North, in front otherwise.
    /// Optionally emits a periodic FX_SmokePuff (e.g. a lit cigarette) — a single
    /// pooled sprite, no Instantiate per puff.
    /// </summary>
    public class HeldItem : MonoBehaviour
    {
        [Header("Facing source (assign exactly one)")]
        [SerializeField] private Color smokeTint = new Color(0.55f, 0.95f, 0.45f, 1f); // fumaça verde do Pedro
        [SerializeField] private CharacterSpriteAnimator characterAnimator;
        [SerializeField] private NPCSprite npcSprite;
        [SerializeField] private PlayerSpriteAnimator playerAnimator;

        [Header("Item sprite (child)")]
        [SerializeField] private SpriteRenderer itemRenderer;
        [SerializeField] private Vector2 offsetSouth = new Vector2(0.12f, -0.05f);
        [SerializeField] private Vector2 offsetEast = new Vector2(0.16f, 0f);
        [SerializeField] private Vector2 offsetNorth = new Vector2(-0.08f, 0.05f);
        [SerializeField] private int sortingOrderFront = 10;
        [SerializeField] private int sortingOrderBack = -10;

        [Header("Smoke puff (optional, e.g. a lit cigarette)")]
        [SerializeField] private bool emitsSmoke;
        [SerializeField] private Sprite smokeSprite;
        [SerializeField] private float smokeIntervalMin = 2f;
        [SerializeField] private float smokeIntervalMax = 4f;
        [SerializeField] private float smokeRiseDistance = 0.35f;
        [SerializeField] private float smokeDuration = 1.2f;
        [SerializeField] private Vector2 smokeTipOffset = new Vector2(0.08f, 0.02f);

        [Header("Lighter flavor (Pedro's cigarette; harmless on any other item)")]
        [SerializeField] private bool isqueiroFlavor = true;
        [SerializeField, Range(1, 5)] private int isqueiroFailEvery = 3;

        private enum Direction { South, East, North, West }

        private Transform smokeTransform;
        private SpriteRenderer smokeRenderer;
        private Coroutine smokeRoutine;
        private int puffCount;

        /// <summary>Runtime wiring hook for HeldItems built purely in code (e.g.
        /// ButecoFlow's arsenal step, equipping a sword on an ally NPC that has no
        /// hand-authored HeldItem child in the scene). Assigns the facing source and the
        /// item's own SpriteRenderer directly, since the Inspector fields are private.</summary>
        public void SetRefs(NPCSprite facingSource, SpriteRenderer renderer)
        {
            npcSprite = facingSource;
            itemRenderer = renderer;
        }

        private void Awake()
        {
            if (emitsSmoke && smokeSprite != null)
            {
                var go = new GameObject("FX_SmokePuff");
                // Unparented: the puff should drift upward in world space, independent
                // of the character's own movement while it plays out.
                smokeTransform = go.transform;
                smokeRenderer = go.AddComponent<SpriteRenderer>();
                smokeRenderer.sprite = smokeSprite;
                smokeRenderer.sortingOrder = sortingOrderFront + 1;
                go.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (emitsSmoke && smokeSprite != null)
            {
                smokeRoutine = StartCoroutine(SmokeLoop());
            }
        }

        private void OnDisable()
        {
            if (smokeRoutine != null)
            {
                StopCoroutine(smokeRoutine);
                smokeRoutine = null;
            }
            if (smokeTransform != null)
            {
                smokeTransform.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (smokeTransform != null)
            {
                Destroy(smokeTransform.gameObject);
            }
        }

        private void Update()
        {
            Vector2 facing = GetFacing();
            Direction dir = Quantize(facing);
            ApplyDirection(dir);
        }

        private Vector2 GetFacing()
        {
            if (characterAnimator != null)
            {
                return characterAnimator.CurrentFacing;
            }
            if (npcSprite != null)
            {
                return npcSprite.CurrentFacing;
            }
            if (playerAnimator != null)
            {
                return playerAnimator.CurrentFacing;
            }
            return Vector2.down;
        }

        private static Direction Quantize(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Direction.South;
            }
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0f ? Direction.East : Direction.West;
            }
            return direction.y >= 0f ? Direction.North : Direction.South;
        }

        private void ApplyDirection(Direction dir)
        {
            bool isWest = dir == Direction.West;
            Vector2 offset = dir switch
            {
                Direction.North => offsetNorth,
                Direction.South => offsetSouth,
                _ => offsetEast, // East and West share the East offset, mirrored below.
            };
            if (isWest)
            {
                offset.x = -offset.x;
            }

            transform.localPosition = offset;

            if (itemRenderer != null)
            {
                itemRenderer.flipX = isWest;
                itemRenderer.sortingOrder = dir == Direction.North ? sortingOrderBack : sortingOrderFront;
            }
        }

        private IEnumerator SmokeLoop()
        {
            // Small fallback wait before the first puff so several lit items in a scene
            // don't all puff in lockstep.
            yield return new WaitForSeconds(Random.Range(0.2f, smokeIntervalMax));

            while (true)
            {
                float wait = Random.Range(smokeIntervalMin, smokeIntervalMax);
                yield return new WaitForSeconds(wait);
                yield return PuffRoutine();
            }
        }

        private IEnumerator PuffRoutine()
        {
            if (smokeTransform == null || itemRenderer == null)
            {
                yield break;
            }

            puffCount++;
            if (isqueiroFlavor && isqueiroFailEvery > 0 && puffCount % isqueiroFailEvery == 0)
            {
                Sfx.Play(SoundId.IsqueiroFalha, transform.position);
                SpeechBubble.Say(transform, "*tsc tsc*", 1.2f);
                yield return new WaitForSeconds(0.5f);
            }
            else if (isqueiroFlavor)
            {
                Sfx.Play(SoundId.IsqueiroAcende, transform.position);
            }

            Vector3 tipWorld = itemRenderer.transform.TransformPoint(new Vector3(smokeTipOffset.x * (itemRenderer.flipX ? -1f : 1f), smokeTipOffset.y, 0f));
            smokeTransform.position = tipWorld;
            smokeTransform.localScale = Vector3.one * 0.6f;
            smokeTransform.gameObject.SetActive(true);
            if (smokeRenderer != null)
            {
                Color c = smokeTint;
                c.a = 1f;
                smokeRenderer.color = c;
            }

            float t = 0f;
            while (t < smokeDuration)
            {
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / smokeDuration);
                smokeTransform.position = tipWorld + Vector3.up * (smokeRiseDistance * t01);
                smokeTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.1f, t01);
                if (smokeRenderer != null)
                {
                    Color c = smokeRenderer.color;
                    c.a = Mathf.Lerp(1f, 0f, t01);
                    smokeRenderer.color = c;
                }
                yield return null;
            }

            smokeTransform.gameObject.SetActive(false);
        }
    }
}
