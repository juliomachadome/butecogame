using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;
using ButecoDosDevs.Systems;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Esc pause menu, one instance per gameplay scene (Buteco/Rua/BarRival). Builds its
    /// own UI lazily on first pause (UIBuilder, same code-built pattern as the rest of
    /// the project's runtime UI). Esc is ignored while a DialogueUI is open (that panel
    /// owns Escape for closing dialogue) so the two never fight over the same key.
    /// Time.timeScale is always restored to 1 on OnDestroy (scene unload) as a safety
    /// net, on top of the explicit reset before every scene change below.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerAttack attack;
        [SerializeField] private string butecoScenePath = "Buteco";
        [SerializeField] private string menuScenePath = "Menu";

        private InputAction pauseAction;
        private GameObject panelRoot;
        private OptionsMenu optionsMenu;
        private bool paused;
        private bool prevMovementEnabled;
        private bool prevAttackEnabled;

        private void Awake()
        {
            if (dialogueUI == null)
            {
                dialogueUI = Object.FindAnyObjectByType<DialogueUI>();
            }

            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                if (movement == null) movement = playerGo.GetComponent<PlayerMovement>();
                if (attack == null) attack = playerGo.GetComponent<PlayerAttack>();
            }

            pauseAction = new InputAction(name: "Pause", type: InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.performed += OnPausePerformed;
        }

        private void OnEnable()
        {
            pauseAction?.Enable();
        }

        private void OnDisable()
        {
            pauseAction?.Disable();
        }

        private void OnDestroy()
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction?.Dispose();
            if (paused)
            {
                Time.timeScale = 1f; // anti-soft-lock: never leave the game paused across a scene change
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            if (dialogueUI != null && dialogueUI.IsOpen)
            {
                return; // DialogueUI owns Escape while a conversation is open
            }

            if (paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private void Pause()
        {
            if (paused)
            {
                return;
            }
            paused = true;

            if (movement != null)
            {
                prevMovementEnabled = movement.enabled;
                movement.enabled = false;
            }
            if (attack != null)
            {
                prevAttackEnabled = attack.enabled;
                attack.enabled = false;
            }

            HitStop.ForceReset();
            Time.timeScale = 0f;

            if (panelRoot == null)
            {
                BuildUI();
            }
            panelRoot.SetActive(true);
        }

        private void Resume()
        {
            if (!paused)
            {
                return;
            }
            paused = false;

            Time.timeScale = 1f;

            if (movement != null) movement.enabled = prevMovementEnabled;
            if (attack != null) attack.enabled = prevAttackEnabled;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void BuildUI()
        {
            Canvas canvas = UIBuilder.CreateOverlayCanvas("PauseMenuCanvas", 200, transform);
            panelRoot = UIBuilder.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.75f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UIBuilder.CreateText(panelRoot.transform, "PAUSADO", 40f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(600f, 60f));

            UIBuilder.CreateButton(panelRoot.transform, "CONTINUAR", new Vector2(0f, 90f), new Vector2(320f, 56f), Resume);
            UIBuilder.CreateButton(panelRoot.transform, "OPÇÕES", new Vector2(0f, 20f), new Vector2(320f, 56f), OnOpcoes);
            UIBuilder.CreateButton(panelRoot.transform, "NOVA GUERRA PÚNICA", new Vector2(0f, -50f), new Vector2(320f, 56f), OnNewWar);
            UIBuilder.CreateButton(panelRoot.transform, "MENU", new Vector2(0f, -120f), new Vector2(320f, 56f), OnMenu);
            UIBuilder.CreateButton(panelRoot.transform, "SAIR", new Vector2(0f, -190f), new Vector2(320f, 56f), OnQuit);

            optionsMenu = gameObject.AddComponent<OptionsMenu>();

            panelRoot.SetActive(false);
        }

        private void OnNewWar()
        {
            Time.timeScale = 1f;
            GameState.ResetAll();
            SceneTransition.Load(butecoScenePath);
        }

        private void OnMenu()
        {
            Time.timeScale = 1f;
            SceneTransition.Load(menuScenePath);
        }

        private void OnQuit()
        {
            Time.timeScale = 1f;
            Application.Quit();
        }

        private void OnOpcoes()
        {
            if (optionsMenu != null)
            {
                optionsMenu.Open(); // stacks above the pause panel; timeScale stays 0 (pause owns it)
            }
        }
    }
}
