using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Generic multiple-choice prompt: a question header plus N selectable option
    /// buttons, navigable with W/S or arrow keys (or gamepad D-pad), confirmed with
    /// Enter/E/Space/gamepad South, or picked directly with the mouse via the buttons'
    /// own OnClick. Resolves exactly once via the onChosen callback (the picked option's
    /// index) then destroys itself. Self-contained (UIBuilder), same disposable-
    /// GameObject pattern as NewWarPrompt. Anti-soft-lock: auto-confirms whichever
    /// option is currently highlighted after timeoutSeconds, so it can never wait
    /// forever for input.
    /// </summary>
    public static class ChoicePrompt
    {
        private class Runner : MonoBehaviour
        {
            public System.Action<int> onChosen;
            public Button[] buttons;
            public Image[] backgrounds;
            public int selected;
            public float timeoutSeconds = 90f;

            private InputAction upAction;
            private InputAction downAction;
            private InputAction confirmAction;
            private bool resolved;

            private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.12f);
            private static readonly Color SelectedColor = new Color(1f, 0.75f, 0.25f, 0.55f);

            private void Awake()
            {
                upAction = new InputAction(name: "ChoiceUp", type: InputActionType.Button);
                upAction.AddBinding("<Keyboard>/w");
                upAction.AddBinding("<Keyboard>/upArrow");
                upAction.AddBinding("<Gamepad>/dpad/up");
                upAction.performed += _ => Move(-1);
                upAction.Enable();

                downAction = new InputAction(name: "ChoiceDown", type: InputActionType.Button);
                downAction.AddBinding("<Keyboard>/s");
                downAction.AddBinding("<Keyboard>/downArrow");
                downAction.AddBinding("<Gamepad>/dpad/down");
                downAction.performed += _ => Move(1);
                downAction.Enable();

                confirmAction = new InputAction(name: "ChoiceConfirm", type: InputActionType.Button);
                confirmAction.AddBinding("<Keyboard>/enter");
                confirmAction.AddBinding("<Keyboard>/e");
                confirmAction.AddBinding("<Keyboard>/space");
                confirmAction.AddBinding("<Gamepad>/buttonSouth");
                confirmAction.performed += _ => Resolve(selected);
                confirmAction.Enable();

                StartCoroutine(TimeoutWatch());
            }

            private IEnumerator TimeoutWatch()
            {
                float t = 0f;
                while (t < timeoutSeconds)
                {
                    if (resolved)
                    {
                        yield break;
                    }
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                Resolve(selected);
            }

            private void Move(int delta)
            {
                if (resolved || buttons == null || buttons.Length == 0)
                {
                    return;
                }
                selected = ((selected + delta) % buttons.Length + buttons.Length) % buttons.Length;
                Highlight();
            }

            public void Highlight()
            {
                if (backgrounds == null)
                {
                    return;
                }
                for (int i = 0; i < backgrounds.Length; i++)
                {
                    if (backgrounds[i] != null)
                    {
                        backgrounds[i].color = i == selected ? SelectedColor : NormalColor;
                    }
                }
            }

            public void Resolve(int index)
            {
                if (resolved)
                {
                    return;
                }
                resolved = true;

                upAction.Disable();
                downAction.Disable();
                confirmAction.Disable();

                onChosen?.Invoke(index);
                Destroy(gameObject);
            }

            private void OnDestroy()
            {
                upAction?.Dispose();
                downAction?.Dispose();
                confirmAction?.Dispose();
            }
        }

        /// <summary>Builds and shows the prompt; onChosen fires exactly once with the picked option's index.</summary>
        public static void Show(string question, string[] options, System.Action<int> onChosen)
        {
            if (options == null || options.Length == 0)
            {
                onChosen?.Invoke(0);
                return;
            }

            GameObject go = new GameObject("ChoicePrompt");
            Runner runner = go.AddComponent<Runner>();
            runner.onChosen = onChosen;

            Canvas canvas = UIBuilder.CreateOverlayCanvas("ChoicePromptCanvas", 3100, go.transform);

            float panelHalfHeight = 60f + options.Length * 28f;
            GameObject panel = UIBuilder.CreatePanel(canvas.transform, new Color(0.04f, 0.04f, 0.06f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -panelHalfHeight), new Vector2(320f, panelHalfHeight));

            UIBuilder.CreateText(panel.transform, question, 22f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(600f, 60f));

            Button[] buttons = new Button[options.Length];
            Image[] backgrounds = new Image[options.Length];

            float startY = -80f;
            for (int i = 0; i < options.Length; i++)
            {
                int captured = i;
                Button btn = UIBuilder.CreateButton(panel.transform, options[i], new Vector2(0f, startY - i * 56f), new Vector2(560f, 46f),
                    () => runner.Resolve(captured));
                buttons[i] = btn;
                backgrounds[i] = btn.GetComponent<Image>();
            }

            runner.buttons = buttons;
            runner.backgrounds = backgrounds;
            runner.selected = 0;
            runner.Highlight();
        }
    }
}
