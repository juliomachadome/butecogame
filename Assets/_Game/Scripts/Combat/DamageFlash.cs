using System.Collections;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Flashes a SpriteRenderer's color briefly when the sibling Health takes damage.
    /// No custom material — just a color swap.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.08f;

        private Health health;
        private Color originalColor;
        private Coroutine flashRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            if (spriteRenderer == null)
            {
                return;
            }
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            flashRoutine = null;
        }
    }
}
