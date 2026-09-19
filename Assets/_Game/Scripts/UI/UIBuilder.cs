using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Tiny static helpers to build self-contained uGUI at runtime (same "no prefab,
    /// build-it-in-code" approach as TitleCard/SceneTransition/SpeechBubble), shared by
    /// MainMenuController, PauseMenu, BattleLives (defeat screen) and NewWarPrompt so
    /// each one doesn't reimplement Canvas/Button/Text scaffolding. Never used in a
    /// per-frame loop — only at construction time.
    /// </summary>
    public static class UIBuilder
    {
        public static Canvas CreateOverlayCanvas(string name, int sortingOrder, Transform parent = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        /// <summary>Finds the scene's EventSystem (one-time lookup, never in Update) or creates
        /// one wired to the new Input System (project is "Input System only").</summary>
        public static EventSystem EnsureEventSystem()
        {
            EventSystem existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject("EventSystem");
            EventSystem es = go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            return es;
        }

        public static GameObject CreatePanel(Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, TextAlignmentOptions align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject("Text_" + text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = align;
            label.color = color;
            RectTransform rt = label.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return label;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject("Button_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.12f);
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            CreateText(go.transform, label, 26f, TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Stretch the label to fill the button.
            RectTransform labelRt = go.transform.GetChild(0).GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }
            return btn;
        }
    }
}
