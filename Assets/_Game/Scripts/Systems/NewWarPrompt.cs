using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// "Nova Guerra Púnica?" yes/no prompt shown once the epilogue's Pedro conversation
    /// ends. Static helper that builds its own throwaway GameObject+UI (same pattern as
    /// TitleCard/SceneTransition) and resolves exactly once, via either button click or
    /// the [E] sim / [Esc] não shortcuts, then destroys itself — never left dangling.
    /// </summary>
    public static class NewWarPrompt
    {
        private class Runner : MonoBehaviour
        {
            public System.Action onYes;
            public System.Action onNo;

            private InputAction yesAction;
            private InputAction noAction;
            private bool resolved;

            private void Awake()
            {
                yesAction = new InputAction(name: "NewWarYes", type: InputActionType.Button);
                yesAction.AddBinding("<Keyboard>/e");
                yesAction.AddBinding("<Gamepad>/buttonSouth");
                yesAction.performed += _ => Resolve(true);
                yesAction.Enable();

                noAction = new InputAction(name: "NewWarNo", type: InputActionType.Button);
                noAction.AddBinding("<Keyboard>/escape");
                noAction.performed += _ => Resolve(false);
                noAction.Enable();
            }

            public void Resolve(bool yes)
            {
                if (resolved)
                {
                    return;
                }
                resolved = true;

                yesAction.Disable();
                noAction.Disable();

                if (yes)
                {
                    onYes?.Invoke();
                }
                else
                {
                    onNo?.Invoke();
                }

                Destroy(gameObject);
            }

            private void OnDestroy()
            {
                yesAction?.Dispose();
                noAction?.Dispose();
            }
        }

        public static void Show(System.Action onYes, System.Action onNo = null)
        {
            GameObject go = new GameObject("NewWarPrompt");
            Runner runner = go.AddComponent<Runner>();
            runner.onYes = onYes;
            runner.onNo = onNo;

            Canvas canvas = UIBuilder.CreateOverlayCanvas("NewWarPromptCanvas", 250, go.transform);
            GameObject panel = UIBuilder.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.85f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -110f), new Vector2(260f, 110f));

            UIBuilder.CreateText(panel.transform, "E aí, mais uma Guerra Púnica?", 22f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(480f, 60f));
            UIBuilder.CreateText(panel.transform, "[E] Sim     [Esc] Não", 15f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.6f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(480f, 30f));

            UIBuilder.CreateButton(panel.transform, "SIM", new Vector2(-90f, -15f), new Vector2(150f, 48f), () => runner.Resolve(true));
            UIBuilder.CreateButton(panel.transform, "NÃO", new Vector2(90f, -15f), new Vector2(150f, 48f), () => runner.Resolve(false));
        }
    }
}
