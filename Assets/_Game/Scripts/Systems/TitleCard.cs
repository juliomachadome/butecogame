using System.Collections;
using TMPro;
using UnityEngine;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Big centered title text ("A GUERRA PÚNICA") that fades in, holds, fades out,
    /// then cleans up after itself. Static helper — call from any MonoBehaviour's
    /// coroutine with `yield return TitleCard.Show(host, "...", 2.5f);`.
    /// </summary>
    public static class TitleCard
    {
        public static IEnumerator Show(MonoBehaviour host, string text, float holdSeconds = 2.5f, float fadeSeconds = 0.5f)
        {
            GameObject go = new GameObject("TitleCardCanvas");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4000;

            GameObject textGo = new GameObject("TitleText");
            textGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 72;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 1f, 1f, 0f);
            RectTransform rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1400f, 200f);
            rt.anchoredPosition = Vector2.zero;

            yield return Fade(label, 0f, 1f, fadeSeconds);
            yield return new WaitForSeconds(Mathf.Max(holdSeconds, 0f));
            yield return Fade(label, 1f, 0f, fadeSeconds);

            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        private static IEnumerator Fade(TextMeshProUGUI label, float from, float to, float duration)
        {
            duration = Mathf.Max(duration, 0.01f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (label == null)
                {
                    yield break;
                }
                float a = Mathf.Lerp(from, to, t / duration);
                label.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
            if (label != null)
            {
                label.color = new Color(1f, 1f, 1f, to);
            }
        }
    }
}
