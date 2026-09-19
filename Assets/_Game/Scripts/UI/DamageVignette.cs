using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ButecoDosDevs.Combat;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Full-screen red flash (~0.25s, fading out) when the PLAYER's Health takes
    /// damage &gt; 0. Lives on a full-rect Image in Canvas_UI with raycastTarget=false
    /// so it never blocks clicks.
    /// </summary>
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Image image;
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private Color flashColor = new Color(0.8f, 0f, 0f, 0.35f);

        private Coroutine routine;

        private void Awake()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged += OnPlayerDamaged;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= OnPlayerDamaged;
            }
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            SetAlpha(0f);
        }

        private void OnPlayerDamaged(DamageInfo info)
        {
            if (info.amount <= 0f)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }
            routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(1f - Mathf.Clamp01(t / duration));
                yield return null;
            }
            SetAlpha(0f);
            routine = null;
        }

        private void SetAlpha(float t)
        {
            if (image == null)
            {
                return;
            }
            Color c = flashColor;
            c.a = flashColor.a * t;
            image.color = c;
        }
    }
}
