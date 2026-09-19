using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Fade-to-black scene transition. Self-contained: builds its own throwaway
    /// Canvas+Image, DontDestroyOnLoad's it just long enough to survive the scene
    /// load, then destroys itself — no persistent singleton left behind. The internal
    /// MonoBehaviour ("Runner") exists purely so a static class can host a coroutine.
    /// Anti-soft-lock: the scene-load wait and both fades are timeout-bounded, so a
    /// stalled/missing scene can never leave the screen stuck black forever.
    /// </summary>
    public static class SceneTransition
    {
        private class Runner : MonoBehaviour { }

        /// <summary>Fades to black, loads sceneName (by name; must be in Build Settings), then fades back in.</summary>
        public static void Load(string sceneName, float fadeSeconds = 0.4f, float loadTimeout = 8f)
        {
            GameObject go = new GameObject("SceneFadeCanvas");
            Object.DontDestroyOnLoad(go);
            Runner runner = go.AddComponent<Runner>();
            runner.StartCoroutine(Routine(go, sceneName, fadeSeconds, loadTimeout));
        }

        private static IEnumerator Routine(GameObject host, string sceneName, float fadeSeconds, float loadTimeout)
        {
            Canvas canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            GameObject imgGo = new GameObject("Fade");
            imgGo.transform.SetParent(host.transform, false);
            Image img = imgGo.AddComponent<Image>();
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img.color = new Color(0f, 0f, 0f, 0f);

            yield return FadeTo(img, 1f, fadeSeconds);

            AsyncOperation op = null;
            // Scene might not be in Build Settings yet (e.g. mid-development) — don't hang forever.
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                op = SceneManager.LoadSceneAsync(sceneName);
            }
            else
            {
                Debug.LogWarning("[SceneTransition] Cena '" + sceneName + "' não está em Build Settings.");
            }

            float elapsed = 0f;
            while (op != null && !op.isDone && elapsed < loadTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (img != null)
            {
                yield return FadeTo(img, 0f, fadeSeconds);
            }

            if (host != null)
            {
                Object.Destroy(host);
            }
        }

        private static IEnumerator FadeTo(Image img, float targetAlpha, float duration)
        {
            if (img == null)
            {
                yield break;
            }
            float startAlpha = img.color.a;
            float t = 0f;
            duration = Mathf.Max(duration, 0.01f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                if (img == null)
                {
                    yield break;
                }
                float a = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                img.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }
            if (img != null)
            {
                img.color = new Color(0f, 0f, 0f, targetAlpha);
            }
        }
    }
}
