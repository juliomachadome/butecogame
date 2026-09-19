using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ButecoDosDevs.Systems;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Menu.unity's only script: builds the whole title screen in code at runtime
    /// (same pattern as TitleCard/SceneTransition — no prefab wiring needed) so the
    /// scene itself only needs this component on an empty GameObject. JOGAR resets
    /// GameState and loads the Buteco scene; CONTROLES toggles a controls panel; SAIR
    /// quits (no-op in the Editor, works in a build).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Sprite[] castPortraits;
        [SerializeField] private string firstScene = "Buteco";

        private GameObject controlsPanel;
        private OptionsMenu optionsMenu;

        private void Start()
        {
            Canvas canvas = UIBuilder.CreateOverlayCanvas("MainMenuCanvas", 100);

            UIBuilder.CreatePanel(canvas.transform, new Color(0.04f, 0.04f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UIBuilder.CreateText(canvas.transform, "BUTECO DOS DEVS", 64f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1400f, 100f));
            UIBuilder.CreateText(canvas.transform, "A GUERRA PÚNICA", 32f, TextAlignmentOptions.Center, new Color(1f, 0.55f, 0.15f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1200f, 60f));
            UIBuilder.CreateText(canvas.transform, "Dois bares. Uma comunidade.", 20f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.75f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -245f), new Vector2(1200f, 40f));

            BuildCastRow(canvas.transform);

            Button jogar = UIBuilder.CreateButton(canvas.transform, "JOGAR", new Vector2(0f, -85f), new Vector2(300f, 56f), OnJogar);
            UIBuilder.CreateButton(canvas.transform, "OPÇÕES", new Vector2(0f, -150f), new Vector2(300f, 56f), OnOpcoes);
            UIBuilder.CreateButton(canvas.transform, "CONTROLES", new Vector2(0f, -215f), new Vector2(300f, 56f), OnControles);
            UIBuilder.CreateButton(canvas.transform, "SAIR", new Vector2(0f, -280f), new Vector2(300f, 56f), OnSair);

            optionsMenu = gameObject.AddComponent<OptionsMenu>();

            UIBuilder.CreateText(canvas.transform, "Game Jam - Buteco dos Devs - Sons pela comunidade", 14f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.4f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1000f, 30f));

            BuildControlsPanel(canvas.transform);

            EventSystem es = UIBuilder.EnsureEventSystem();
            if (es != null && jogar != null)
            {
                es.SetSelectedGameObject(jogar.gameObject);
            }
        }

        private void BuildCastRow(Transform parent)
        {
            if (castPortraits == null || castPortraits.Length == 0)
            {
                return; // Decorative only: no soft-lock/error if sprites weren't wired yet.
            }

            float spacing = 130f;
            float startX = -((castPortraits.Length - 1) * spacing) / 2f;

            for (int i = 0; i < castPortraits.Length; i++)
            {
                if (castPortraits[i] == null)
                {
                    continue;
                }
                GameObject go = new GameObject("Cast_" + i, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                Image img = go.AddComponent<Image>();
                img.sprite = castPortraits[i];
                img.preserveAspect = true;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(startX + i * spacing, -300f);
                rt.sizeDelta = new Vector2(110f, 110f);
                // Mesma escala usada em jogo: sprites do Julio/Moe ocupam o quadro inteiro.
                string spriteName = castPortraits[i].name;
                float scale = spriteName.StartsWith("Julio") ? 0.75f : (spriteName.StartsWith("MoeSimpsons") ? 0.82f : 1f);
                rt.localScale = new Vector3(scale, scale, 1f);

                string displayName = spriteName.StartsWith("Dev") ? "Dev Novato"
                    : spriteName.StartsWith("Pedro") ? "Pedro Pietro"
                    : spriteName.StartsWith("ReiLuiz") ? "Rei Luiz"
                    : spriteName.StartsWith("Funnie") ? "Funnie"
                    : spriteName.StartsWith("Julio") ? "J Machado (Naldo)"
                    : spriteName.StartsWith("Moe") ? "Moe"
                    : "";
                UIBuilder.CreateText(parent, displayName, 15f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.55f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(startX + i * spacing, -368f), new Vector2(128f, 24f));
            }
        }

        private void BuildControlsPanel(Transform parent)
        {
            controlsPanel = UIBuilder.CreatePanel(parent, new Color(0f, 0f, 0f, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -220f), new Vector2(320f, 220f));

            UIBuilder.CreateText(controlsPanel.transform, "CONTROLES", 28f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(500f, 40f));

            string body =
                "WASD / setas — mover\n" +
                "Espaço — dash\n" +
                "J / clique esquerdo — atacar\n" +
                "Segurar clique direito / K — defender\n" +
                "E — interagir\n" +
                "Esc — pausar";
            UIBuilder.CreateText(controlsPanel.transform, body, 20f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 260f));

            UIBuilder.CreateButton(controlsPanel.transform, "FECHAR", new Vector2(0f, -170f), new Vector2(220f, 46f), () => controlsPanel.SetActive(false));

            controlsPanel.SetActive(false);
        }

        private void OnJogar()
        {
            GameState.ResetAll();
            SceneTransition.Load(firstScene);
        }

        private void OnControles()
        {
            if (controlsPanel != null)
            {
                controlsPanel.SetActive(true);
            }
        }

        private void OnSair()
        {
            Application.Quit();
        }

        private void OnOpcoes()
        {
            if (optionsMenu != null)
            {
                optionsMenu.Open();
            }
        }
    }
}
