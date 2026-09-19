using System.Collections;
using TMPro;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Thin floating HP bar above a combatant's head: hidden by default, shows on
    /// Health.Damaged (a clean two-rect bar, no numbers), and auto-hides again after
    /// visibleSeconds of no further damage. Built lazily in code (a background quad +
    /// a left-aligned fill quad, same "no prefab needed" approach as SpeechBubble),
    /// so it can just be added as a component to any ally/rival/boss prefab. The boss
    /// variant is wider/taller and shows a name label above the bar.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Color fillColor = Color.green;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.0f, 0f);
        [SerializeField] private float width = 0.9f;
        [SerializeField] private float height = 0.12f;
        [SerializeField] private float visibleSeconds = 3f;

        [Header("Boss variant (bigger bar + name label)")]
        [SerializeField] private bool isBoss;
        [SerializeField] private string bossName = "Admin Rival";
        [SerializeField] private float bossWidth = 2.2f;
        [SerializeField] private float bossHeight = 0.22f;

        private Health health;
        private GameObject root;
        private Transform fill;
        private SpriteRenderer fillRenderer;
        private Coroutine hideRoutine;

        private static Sprite pixelSprite;

        private float BarWidth => isBoss ? bossWidth : width;
        private float BarHeight => isBoss ? bossHeight : height;

        private void Awake()
        {
            health = GetComponent<Health>();
            BuildBar();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private static Sprite PixelSprite()
        {
            if (pixelSprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                pixelSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }
            return pixelSprite;
        }

        private void BuildBar()
        {
            root = new GameObject("HealthBarRoot");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = offset;

            GameObject bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(root.transform, false);
            SpriteRenderer bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = PixelSprite();
            bgSr.color = new Color(0f, 0f, 0f, 0.75f);
            bgSr.sortingOrder = 55;
            bgGo.transform.localScale = new Vector3(BarWidth + 0.04f, BarHeight + 0.04f, 1f);

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            fillRenderer = fillGo.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = PixelSprite();
            fillRenderer.color = fillColor;
            fillRenderer.sortingOrder = 56;
            fill = fillGo.transform;

            if (isBoss && !string.IsNullOrEmpty(bossName))
            {
                GameObject nameGo = new GameObject("Name");
                nameGo.transform.SetParent(root.transform, false);
                nameGo.transform.localPosition = new Vector3(0f, BarHeight * 0.5f + 0.22f, 0f);
                TextMeshPro label = nameGo.AddComponent<TextMeshPro>();
                label.text = bossName;
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 3f;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(1f, 0.35f, 0.3f, 1f);
                label.rectTransform.sizeDelta = new Vector2(4f, 0.5f);
                label.sortingOrder = 57;
            }

            UpdateFill(1f);
            root.SetActive(false);
        }

        private void UpdateFill(float fraction01)
        {
            if (fill == null)
            {
                return;
            }
            float w = BarWidth;
            float currentWidth = Mathf.Max(w * Mathf.Clamp01(fraction01), 0.0001f);
            fill.localScale = new Vector3(currentWidth, BarHeight, 1f);
            fill.localPosition = new Vector3(-(w - currentWidth) * 0.5f, 0f, 0f);
        }

        private void OnDamaged(DamageInfo info)
        {
            float maxHp = health != null ? Mathf.Max(health.MaxHP, 0.0001f) : 1f;
            float currentHp = health != null ? Mathf.Max(health.CurrentHP, 0f) : 0f;
            UpdateFill(currentHp / maxHp);
            Show();
        }

        private void OnDied()
        {
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void Show()
        {
            if (root == null)
            {
                return;
            }
            root.SetActive(true);
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(visibleSeconds);
            if (root != null)
            {
                root.SetActive(false);
            }
            hideRoutine = null;
        }
    }
}
