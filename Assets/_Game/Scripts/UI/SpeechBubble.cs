using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Lightweight world-space speech balloon: a single 3D TextMeshPro child (same
    /// no-Canvas approach as NPCNameTag), built lazily so it can be dropped on ANY
    /// character at runtime via the static Say() helper without pre-wiring every NPC
    /// in the Inspector. A simple FIFO queue lets several Show() calls stack instead
    /// of overwriting each other; a hard safety cap guarantees the balloon can never
    /// talk forever (anti-soft-lock).
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float defaultSeconds = 2.5f;
        [SerializeField] private float fontSize = 3f;
        [SerializeField] private Color textColor = Color.white;

        private TextMeshPro label;
        private readonly Queue<(string text, float seconds)> queue = new Queue<(string, float)>();
        private Coroutine playRoutine;

        private static readonly Dictionary<Transform, SpeechBubble> Cache = new Dictionary<Transform, SpeechBubble>();

        public bool IsTalking => playRoutine != null;

        private void Awake()
        {
            BuildLabel();
        }

        private void BuildLabel()
        {
            if (label != null)
            {
                return;
            }

            GameObject go = new GameObject("SpeechBubbleLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;

            label = go.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.color = textColor;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.rectTransform.sizeDelta = new Vector2(3.2f, 1f);
            label.text = string.Empty;
            label.sortingOrder = 60;

            go.SetActive(false);
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
                if (label != null)
                {
                    label.text = text;
                    label.gameObject.SetActive(true);
                }

                float t = 0f;
                while (t < seconds)
                {
                    t += Time.deltaTime;
                    safetyBudget -= Time.deltaTime;
                    yield return null;
                }
            }

            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
            playRoutine = null;
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
