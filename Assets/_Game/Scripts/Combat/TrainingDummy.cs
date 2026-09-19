using System.Collections;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Stationary test target. On death it "falls" (rotates + darkens, hurtbox
    /// disabled) and after a delay resets HP and returns to its spawn position,
    /// so playtesting can continue without reloading the scene.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private float resetDelay = 3f;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Collider2D hurtboxCollider;
        [SerializeField] private Color downColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private Health health;
        private Rigidbody2D rb;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Color originalColor;
        private Coroutine resetRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            if (sprite == null)
            {
                sprite = GetComponentInChildren<SpriteRenderer>();
            }
            if (sprite != null)
            {
                originalColor = sprite.color;
            }
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
        }

        private void OnDied()
        {
            if (resetRoutine != null)
            {
                StopCoroutine(resetRoutine);
            }
            resetRoutine = StartCoroutine(FallAndReset());
        }

        private IEnumerator FallAndReset()
        {
            transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, 90f);
            if (sprite != null)
            {
                sprite.color = downColor;
            }
            if (hurtboxCollider != null)
            {
                hurtboxCollider.enabled = false;
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            float t = 0f;
            while (t < resetDelay)
            {
                t += Time.deltaTime;
                yield return null;
            }

            transform.position = originalPosition;
            transform.rotation = originalRotation;
            if (sprite != null)
            {
                sprite.color = originalColor;
            }
            if (hurtboxCollider != null)
            {
                hurtboxCollider.enabled = true;
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            health.ResetHealth();
            resetRoutine = null;
        }
    }
}
