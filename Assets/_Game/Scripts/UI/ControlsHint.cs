using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ButecoDosDevs.Systems;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Small bottom-left "how to play" panel shown for up to maxDuration seconds at the
    /// very start of the Buteco scene (first playthrough only — skipped during the
    /// post-war "modo livre" epilogue, since GameState.WarFinished is true then), and
    /// dismissed early the moment the player moves or attacks. Self-built UI (UIBuilder),
    /// no scene wiring needed beyond dropping this component in the scene.
    /// </summary>
    public class ControlsHint : MonoBehaviour
    {
        [SerializeField] private float maxDuration = 10f;

        private GameObject panel;
        private float timer;
        private bool dismissed;

        private void Start()
        {
            if (GameState.WarFinished)
            {
                enabled = false;
                return;
            }

            BuildPanel();
        }

        private void BuildPanel()
        {
            Canvas canvas = UIBuilder.CreateOverlayCanvas("ControlsHintCanvas", 50, transform);

            panel = new GameObject("HintPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.65f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(20f, 20f);
            rt.sizeDelta = new Vector2(420f, 140f);

            string body = "WASD / setas — mover\nEspaço — dash\nJ / clique — atacar\nSegurar clique dir. / K — defender\nE — interagir";
            TextMeshProUGUI label = UIBuilder.CreateText(panel.transform, body, 16f, TextAlignmentOptions.Left, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform labelRt = label.rectTransform;
            labelRt.offsetMin = new Vector2(16f, 12f);
            labelRt.offsetMax = new Vector2(-16f, -12f);
        }

        private void Update()
        {
            if (dismissed || panel == null)
            {
                return;
            }

            timer += Time.deltaTime;

            bool moved = false;
            if (Keyboard.current != null)
            {
                moved = Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
                        Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed ||
                        Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed ||
                        Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            }

            bool attacked = false;
            if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            {
                attacked = true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                attacked = true;
            }

            if (moved || attacked || timer >= maxDuration)
            {
                Dismiss();
            }
        }

        private void Dismiss()
        {
            dismissed = true;
            if (panel != null)
            {
                Destroy(panel.transform.parent.gameObject); // the canvas
            }
        }
    }
}
