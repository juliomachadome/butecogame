using System.Collections;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Reusable "cartoon" attack juice shared by player, allies and enemies: a
    /// directional slash FX with squash&amp;stretch, a tiny visual-only lunge applied to
    /// a dedicated "Visual" child sprite (never the Rigidbody/root transform — see the
    /// 'visual' field), and a pooled impact spark. The slash/spark GameObjects are
    /// created once in Awake and reused every attack (no Instantiate per hit).
    /// </summary>
    public class AttackFX : MonoBehaviour
    {
        [Header("Visual (child holding the character SpriteRenderer, NOT the root/Rigidbody)")]
        [SerializeField] private Transform visual;

        [Header("Slash FX")]
        [SerializeField] private Sprite slashSprite;
        [SerializeField] private Color slashColor = Color.white;
        [SerializeField] private float slashDistance = 0.55f;
        [SerializeField] private float slashTrunkHeight = 0.3f;
        [SerializeField] private Vector2 slashScaleRange = new Vector2(0.8f, 1.1f);
        [SerializeField] private int slashSortingOrder = 50;

        [Header("Spark FX (impact)")]
        [SerializeField] private Sprite sparkSprite;
        [SerializeField] private float sparkDuration = 0.15f;
        [SerializeField] private Vector2 sparkScaleRange = new Vector2(0.5f, 1.2f);
        [SerializeField] private int sparkSortingOrder = 60;

        [Header("Lunge / squash & stretch (Visual only, never touches Rigidbody)")]
        [SerializeField] private float lungeDistance = 0.15f;
        [SerializeField] private Vector2 windupSquash = new Vector2(1.15f, 0.88f);
        [SerializeField] private Vector2 hitStretch = new Vector2(0.9f, 1.1f);

        private Transform slashTransform;
        private SpriteRenderer slashRenderer;
        private Transform sparkTransform;
        private SpriteRenderer sparkRenderer;

        private Vector3 visualRestLocalPosition = Vector3.zero;
        private Vector3 visualRestLocalScale = Vector3.one;

        private Coroutine windupRoutine;
        private Coroutine swingRoutine;
        private Coroutine impactRoutine;

        /// <summary>True while the active-window slash FX/lunge is playing (test/animation hook).</summary>
        public bool IsSwinging => swingRoutine != null;

        private void Awake()
        {
            if (visual != null)
            {
                visualRestLocalPosition = visual.localPosition;
                visualRestLocalScale = visual.localScale;
            }

            slashTransform = CreateFxChild("FX_Slash", slashSprite, slashColor, slashSortingOrder, parentToSelf: true, out slashRenderer);

            // Spark is unparented (world-space): a hit lands on the TARGET, which may keep
            // moving after the swing; a child of the attacker would drag the spark along.
            sparkTransform = CreateFxChild("FX_Spark", sparkSprite, Color.white, sparkSortingOrder, parentToSelf: false, out sparkRenderer);
        }

        private void OnDestroy()
        {
            if (sparkTransform != null)
            {
                Destroy(sparkTransform.gameObject);
            }
        }

        private Transform CreateFxChild(string childName, Sprite sprite, Color color, int sortingOrder, bool parentToSelf, out SpriteRenderer renderer)
        {
            var go = new GameObject(childName);
            if (parentToSelf)
            {
                go.transform.SetParent(transform, false);
            }
            renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            go.SetActive(false);
            return go.transform;
        }

        /// <summary>Squashes the Visual sprite during windup (default 1.15/0.88). No-op without a Visual ref.</summary>
        public void PlayWindupSquash(float duration)
        {
            if (visual == null)
            {
                return;
            }
            if (windupRoutine != null)
            {
                StopCoroutine(windupRoutine);
            }
            windupRoutine = StartCoroutine(WindupSquashRoutine(Mathf.Max(duration, 0.01f)));
        }

        private IEnumerator WindupSquashRoutine(float duration)
        {
            Vector3 from = visual.localScale;
            Vector3 to = new Vector3(visualRestLocalScale.x * windupSquash.x, visualRestLocalScale.y * windupSquash.y, visualRestLocalScale.z);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (visual == null)
                {
                    yield break;
                }
                visual.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            if (visual != null)
            {
                visual.localScale = to;
            }
            windupRoutine = null;
        }

        /// <summary>
        /// Active-window swing: shows the directional slash FX, lunges the Visual sprite
        /// out and back, and stretches then relaxes its scale. Call once when the hitbox
        /// opens; duration should match the attack's active window.
        /// </summary>
        public void PlaySwing(Vector2 dir, float duration)
        {
            duration = Mathf.Max(duration, 0.01f);
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector2.down;
            }
            dir.Normalize();

            if (swingRoutine != null)
            {
                StopCoroutine(swingRoutine);
            }
            swingRoutine = StartCoroutine(SwingRoutine(dir, duration));
        }

        private IEnumerator SwingRoutine(Vector2 dir, float duration)
        {
            // FX_Slash art points toward +X (east). West is mirrored (flipX) instead of
            // rotated 180 so the crescent keeps opening the same way, just facing left.
            bool isWest = dir.x < -0.0001f && Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
            Vector2 rotDir = isWest ? new Vector2(-dir.x, dir.y) : dir;
            float angle = Mathf.Atan2(rotDir.y, rotDir.x) * Mathf.Rad2Deg;

            if (slashTransform != null)
            {
                slashTransform.localPosition = (Vector3)(dir * slashDistance) + Vector3.up * slashTrunkHeight;
                slashTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                if (slashRenderer != null)
                {
                    slashRenderer.flipX = isWest;
                }
                slashTransform.gameObject.SetActive(slashSprite != null);
            }

            Vector3 stretchTarget = visual != null
                ? new Vector3(visualRestLocalScale.x * hitStretch.x, visualRestLocalScale.y * hitStretch.y, visualRestLocalScale.z)
                : Vector3.one;
            Vector3 squashStart = visual != null ? visual.localScale : Vector3.one;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / duration);

                if (slashRenderer != null)
                {
                    Color c = slashColor;
                    c.a = Mathf.Lerp(1f, 0f, t01);
                    slashRenderer.color = c;
                    float scale = Mathf.Lerp(slashScaleRange.x, slashScaleRange.y, t01);
                    slashTransform.localScale = new Vector3(scale, scale, 1f);
                }

                if (visual != null)
                {
                    // Lunge out over the first half, back over the second half.
                    float lungePhase = t01 < 0.5f ? t01 / 0.5f : 1f - (t01 - 0.5f) / 0.5f;
                    visual.localPosition = visualRestLocalPosition + (Vector3)(dir * lungeDistance * lungePhase);

                    // Stretch toward hitStretch then ease back toward rest scale.
                    Vector3 scaleTarget = t01 < 0.5f
                        ? Vector3.Lerp(squashStart, stretchTarget, t01 / 0.5f)
                        : Vector3.Lerp(stretchTarget, visualRestLocalScale, (t01 - 0.5f) / 0.5f);
                    visual.localScale = scaleTarget;
                }

                yield return null;
            }

            if (slashTransform != null)
            {
                slashTransform.gameObject.SetActive(false);
            }
            if (visual != null)
            {
                visual.localPosition = visualRestLocalPosition;
                visual.localScale = visualRestLocalScale;
            }
            swingRoutine = null;
        }

        /// <summary>Impact spark pop at a world position. Reuses a single pooled spark object.</summary>
        public void PlayImpact(Vector3 worldPos)
        {
            PlayImpact(worldPos, 1f);
        }

        /// <summary>Impact spark pop at a world position with a size multiplier (e.g. a bigger parry spark).</summary>
        public void PlayImpact(Vector3 worldPos, float scaleMultiplier)
        {
            if (sparkTransform == null || sparkSprite == null)
            {
                return;
            }
            if (impactRoutine != null)
            {
                StopCoroutine(impactRoutine);
            }
            impactRoutine = StartCoroutine(ImpactRoutine(worldPos, Mathf.Max(scaleMultiplier, 0.01f)));
        }

        private IEnumerator ImpactRoutine(Vector3 worldPos, float scaleMultiplier)
        {
            sparkTransform.position = worldPos;
            sparkTransform.localScale = Vector3.one * sparkScaleRange.x * scaleMultiplier;
            sparkTransform.gameObject.SetActive(true);
            if (sparkRenderer != null)
            {
                Color c = sparkRenderer.color;
                c.a = 1f;
                sparkRenderer.color = c;
            }

            float t = 0f;
            while (t < sparkDuration)
            {
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / sparkDuration);
                float scale = Mathf.Lerp(sparkScaleRange.x, sparkScaleRange.y, t01) * scaleMultiplier;
                sparkTransform.localScale = new Vector3(scale, scale, 1f);
                if (sparkRenderer != null)
                {
                    Color c = sparkRenderer.color;
                    c.a = Mathf.Lerp(1f, 0f, t01);
                    sparkRenderer.color = c;
                }
                yield return null;
            }

            sparkTransform.gameObject.SetActive(false);
            impactRoutine = null;
        }

        private void OnDisable()
        {
            if (windupRoutine != null) { StopCoroutine(windupRoutine); windupRoutine = null; }
            if (swingRoutine != null) { StopCoroutine(swingRoutine); swingRoutine = null; }
            if (impactRoutine != null) { StopCoroutine(impactRoutine); impactRoutine = null; }

            if (visual != null)
            {
                visual.localPosition = visualRestLocalPosition;
                visual.localScale = visualRestLocalScale;
            }
            if (slashTransform != null)
            {
                slashTransform.gameObject.SetActive(false);
            }
            if (sparkTransform != null)
            {
                sparkTransform.gameObject.SetActive(false);
            }
        }
    }
}
