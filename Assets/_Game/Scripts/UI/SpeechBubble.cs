using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ButecoDosDevs.NPC;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Lightweight world-space speech balloon: a 3D TextMeshPro child + a semi-transparent
    /// background quad sized to the text, plus an optional name label above (resolved from
    /// an Interactable on the same GameObject, if any) — same no-Canvas approach as
    /// NPCNameTag, built lazily so it can be dropped on ANY character at runtime via the
    /// static Say() helper without pre-wiring every NPC in the Inspector. A simple FIFO
    /// queue lets several Show() calls stack instead of overwriting each other; a hard
    /// safety cap guarantees the balloon can never talk forever (anti-soft-lock).
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float defaultSeconds = 2.5f;
        [SerializeField] private float fontSize = 3f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.07f, 0.72f);
        [SerializeField] private Color nameColor = new Color(1f, 0.82f, 0.4f, 1f);
        [SerializeField] private float maxWidth = 5.5f;
        [SerializeField] private Vector3 belowOffset = new Vector3(0f, -1.1f, 0f);

        private GameObject root;
        private TextMeshPro label;
        private TextMeshPro nameLabel;
        private SpriteRenderer background;
        private string resolvedName;
        private readonly Queue<(string text, float seconds)> queue = new Queue<(string, float)>();
        private Coroutine playRoutine;

        private static readonly Dictionary<Transform, SpeechBubble> Cache = new Dictionary<Transform, SpeechBubble>();
        private static Sprite pixelSprite;

        public bool IsTalking => playRoutine != null;

        private void Awake()
        {
            BuildLabel();
            Interactable interactable = GetComponent<Interactable>();
            resolvedName = interactable != null ? interactable.DisplayName : null;
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

        private void BuildLabel()
        {
            if (root != null)
            {
                return;
            }

            root = new GameObject("SpeechBubbleRoot");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = offset;

            GameObject bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(root.transform, false);
            background = bgGo.AddComponent<SpriteRenderer>();
            background.sprite = PixelSprite();
            background.color = backgroundColor;
            background.sortingOrder = 58;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            label = labelGo.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.color = textColor;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.rectTransform.sizeDelta = new Vector2(maxWidth, 1f);
            label.text = string.Empty;
            label.sortingOrder = 60;

            GameObject nameGo = new GameObject("Name");
            nameGo.transform.SetParent(root.transform, false);
            nameGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            nameLabel = nameGo.AddComponent<TextMeshPro>();
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.fontSize = fontSize * 0.72f;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.color = nameColor;
            nameLabel.rectTransform.sizeDelta = new Vector2(maxWidth, 0.5f);
            nameLabel.sortingOrder = 61;

            root.SetActive(false);
        }

        /// <summary>Queues a line (FIFO). seconds &lt;= 0 uses defaultSeconds.</summary>
        public void Show(string text, float seconds = -1f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            float duration = seconds > 0f ? seconds : defaultSeconds;
            queue.Enqueue((text, duration));

            if (playRoutine == null)
            {
                playRoutine = StartCoroutine(PlayQueue());
            }
        }

        private IEnumerator PlayQueue()
        {
            // Anti-soft-lock: even if Show() keeps getting called, cap total talk time.
            float safetyBudget = 30f;

            while (queue.Count > 0 && safetyBudget > 0f)
            {
                (string text, float seconds) = queue.Dequeue();
                ApplyLine(text);

                float t = 0f;
                while (t < seconds)
                {
                    t += Time.deltaTime;
                    safetyBudget -= Time.deltaTime;
                    yield return null;
                }
            }

            if (root != null)
            {
                root.SetActive(false);
            }
            playRoutine = null;
        }

        private void ApplyLine(string text)
        {
            if (root == null)
            {
                return;
            }
            root.SetActive(true);
            ClampToCamera();

            label.text = text;
            Vector2 pref = label.GetPreferredValues(text, maxWidth, 0f);
            float padX = 0.3f;
            float padY = 0.22f;
            if (background != null)
            {
                background.transform.localScale = new Vector3(Mathf.Max(pref.x + padX, 0.5f), Mathf.Max(pref.y + padY, 0.4f), 1f);
            }

            if (nameLabel != null)
            {
                bool hasName = !string.IsNullOrEmpty(resolvedName);
                nameLabel.gameObject.SetActive(hasName);
                if (hasName)
                {
                    nameLabel.text = resolvedName;
                    // Sit just above the (possibly resized) background.
                    float bgTop = background != null ? background.transform.localScale.y * 0.5f : 0.3f;
                    nameLabel.transform.localPosition = new Vector3(0f, bgTop + 0.22f, 0f);
                }
            }
        }

        /// <summary>If the balloon's usual above-the-head spot would be clipped by the top of
        /// the camera's view, draw it below the character instead (anti-clip, cheap check —
        /// only needs Camera.main, no per-frame cost since it only runs when a line starts).</summary>
        private void ClampToCamera()
        {
            if (root == null)
            {
                return;
            }
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }
            Vector3 worldPos = transform.position + offset;
            Vector3 viewport = cam.WorldToViewportPoint(worldPos);
            bool offscreenTop = viewport.z > 0f && viewport.y > 0.92f;
            root.transform.localPosition = offscreenTop ? belowOffset : offset;
        }

        /// <summary>Gets (or lazily adds) a SpeechBubble on the anchor and shows text on it.</summary>
        public static void Say(Transform anchor, string text, float seconds = -1f)
        {
            if (anchor == null)
            {
                return;
            }

            if (!Cache.TryGetValue(anchor, out SpeechBubble bubble) || bubble == null)
            {
                bubble = anchor.GetComponent<SpeechBubble>();
                if (bubble == null)
                {
                    bubble = anchor.gameObject.AddComponent<SpeechBubble>();
                }
                Cache[anchor] = bubble;
            }

            bubble.Show(text, seconds);
        }
    }
}
