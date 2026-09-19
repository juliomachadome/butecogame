using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Fase 8/9 battle-scene lives + defeat screen: the player has a small pool of
    /// "lives" per battle (Rua/BarRival). Each player KO (PlayerKO.Knocked) spends one;
    /// defeat triggers when lives hit 0 OR the player and every registered ally are down
    /// at the same time. Shows a small "lives" HUD and, on defeat, a full-screen panel
    /// with TENTAR DE NOVO (reloads the current battle scene, GameState.ChosenWeapon is
    /// untouched so the weapon carries over) and MENU. Builds its own UI at runtime
    /// (UIBuilder), same pattern as the rest of the project's code-built UI.
    /// </summary>
    public class BattleLives : MonoBehaviour
    {
        [SerializeField] private int startingLives = 3;
        [SerializeField] private string menuScenePath = "Menu";

        private PlayerKO playerKO;
        private int livesLeft;
        private bool defeatShown;

        private TextMeshProUGUI livesText;
        private GameObject defeatPanel;

        private void Awake()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                playerKO = playerGo.GetComponent<PlayerKO>();
            }
            livesLeft = Mathf.Max(startingLives, 1);
        }

        private void OnEnable()
        {
            if (playerKO != null)
            {
                playerKO.Knocked += OnPlayerKnocked;
            }
        }

        private void OnDisable()
        {
            if (playerKO != null)
            {
                playerKO.Knocked -= OnPlayerKnocked;
            }
        }

        private void Start()
        {
            BuildLivesHud();
            RefreshLivesText();
        }

        private void OnPlayerKnocked()
        {
            if (defeatShown)
            {
                return;
            }

            livesLeft = Mathf.Max(livesLeft - 1, 0);
            RefreshLivesText();

            if (livesLeft <= 0 || AllDown())
            {
                ShowDefeat();
            }
        }

        private bool AllDown()
        {
            if (playerKO == null || !playerKO.IsDown)
            {
                return false;
            }

            var allies = CombatantRegistry.Allies;
            if (allies.Count == 0)
            {
                return true;
            }
            for (int i = 0; i < allies.Count; i++)
            {
                Health ally = allies[i];
                if (ally != null && !ally.IsDead)
                {
                    return false;
                }
            }
            return true;
        }

        private void BuildLivesHud()
        {
            Canvas canvas = UIBuilder.CreateOverlayCanvas("BattleLivesCanvas", 150, transform);
            livesText = UIBuilder.CreateText(canvas.transform, "", 24f, TextAlignmentOptions.Left, new Color(1f, 0.35f, 0.35f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -110f), new Vector2(260f, 40f));
        }

        private void RefreshLivesText()
        {
            if (livesText != null)
            {
                livesText.text = "❤ x" + livesLeft;
            }
        }

        private void ShowDefeat()
        {
            if (defeatShown)
            {
                return;
            }
            defeatShown = true;

            HitStop.ForceReset();
            Time.timeScale = 0f;

            Canvas canvas = UIBuilder.CreateOverlayCanvas("DefeatCanvas", 300, transform);
            defeatPanel = UIBuilder.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.88f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UIBuilder.CreateText(defeatPanel.transform, "DERROTA", 48f, TextAlignmentOptions.Center, new Color(1f, 0.3f, 0.3f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(700f, 60f));
            UIBuilder.CreateText(defeatPanel.transform, "Os Script Kiddies venceram... por enquanto.", 22f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(700f, 50f));

            UIBuilder.CreateButton(defeatPanel.transform, "TENTAR DE NOVO", new Vector2(0f, 0f), new Vector2(320f, 56f), OnRetry);
            UIBuilder.CreateButton(defeatPanel.transform, "MENU", new Vector2(0f, -70f), new Vector2(320f, 56f), OnMenu);
        }

        private void OnRetry()
        {
            Time.timeScale = 1f;
            // GameState.ChosenWeapon is untouched: reloading applies it again via
            // RuaFlow/BarRivalFlow's Awake -> GameState.ApplyWeapon, same weapon carries over.
            SceneTransition.Load(SceneManager.GetActiveScene().name);
        }

        private void OnMenu()
        {
            Time.timeScale = 1f;
            SceneTransition.Load(menuScenePath);
        }

        private void OnDestroy()
        {
            if (defeatShown)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
