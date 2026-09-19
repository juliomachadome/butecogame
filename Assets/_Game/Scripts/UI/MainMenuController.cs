using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ButecoDosDevs.Systems;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Menu.unity's only script: builds the whole title screen in code at runtime
    /// (same pattern as TitleCard/SceneTransition — no prefab wiring needed) so the
    /// scene itself only needs this component on an empty GameObject. JOGAR plays a
    /// short intro narration (skipped if this is a "nova Guerra Púnica" restart, i.e.
    /// GameState.WarFinished was already true) then resets GameState and loads the
    /// Buteco scene; CONTROLES toggles a controls panel; SAIR quits (no-op in the Editor).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Sprite[] castPortraits;
        [SerializeField] private string firstScene = "Buteco";
        [SerializeField] private float introCharDelay = 0.018f;
        [SerializeField] private float introAutoAdvanceTimeout = 25f;

        private GameObject controlsPanel;
        private OptionsMenu optionsMenu;
        private bool introSkipped;

        private static readonly string[] IntroLines =
        {
            "Existe um lugar onde devs se encontram depois do expediente: o Buteco dos Devs.",
            "A galera conversa muito. Fala muita besteira. Às vezes, até de código.",
            "Lá estão o Moe, o bartender; Pedro Pietro — o mais velho do mundo, 500 anos, e moderador do Buteco; o Rei Luiz, que inventou a game jam; o Funnie, que faz um SaaS em 1 segundo; e J Machado (Naldo), o criador deste jogo.",
            "Do outro lado da rua fica o SCRIPT KIDDIES: o bar dos hackers de tutorial, que se acham sênior.",
            "Você é o Dev Novato. Hoje é seu primeiro dia no Buteco. Um dia normal... por enquanto."
        };

        private void Start()
        {
            Canvas canvas = UIBuilder.CreateOverlayCanvas("MainMenuCanvas", 100);

            UIBuilder.CreatePanel(canvas.transform, new Color(0.04f, 0.04f, 0.06f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UIBuilder.CreateText(canvas.transform, "BUTECO DOS DEVS", 64f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1400f, 100f));
            UIBuilder.CreateText(canvas.transform, "A GUERRA PÚNICA", 32f, TextAlignmentOptions.Center, new Color(1f, 0.55f, 0.15f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(1200f, 60f));
            UIBuilder.CreateText(canvas.transform, "Dois bares. Uma comunidade.", 20f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.75f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -245f), new Vector2(1200f, 40f));

            BuildCastRow(canvas.transform);

            Button jogar = UIBuilder.CreateButton(canvas.transform, "JOGAR", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(300f, 56f), OnJogar);
            UIBuilder.CreateButton(canvas.transform, "OPÇÕES", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -542f), new Vector2(300f, 56f), OnOpcoes);
            UIBuilder.CreateButton(canvas.transform, "CONTROLES", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -604f), new Vector2(300f, 56f), OnControles);
            UIBuilder.CreateButton(canvas.transform, "SAIR", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -666f), new Vector2(300f, 56f), OnSair);

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
                rt.anchoredPosition = new Vector2(startX + i * spacing, -345f);
                rt.sizeDelta = new Vector2(110f, 110f);
                // Mesma escala usada em jogo: sprites do Julio/Moe ocupam o quadro inteiro.
                string spriteName = castPortraits[i].name;
                float scale = spriteName.StartsWith("Julio") ? 0.75f : (spriteName.StartsWith("MoeSimpsons") ? 0.82f : 1f);
                rt.localScale = new Vector3(scale, scale, 1f);

                bool isNaldo = spriteName.StartsWith("Julio");
                string displayName = spriteName.StartsWith("Dev") ? "Dev Novato"
                    : spriteName.StartsWith("Pedro") ? "Pedro Pietro"
                    : spriteName.StartsWith("ReiLuiz") ? "Rei Luiz"
                    : spriteName.StartsWith("Funnie") ? "Funnie"
                    : isNaldo ? "J Machado (Naldo)"
                    : spriteName.StartsWith("Moe") ? "Moe"
                    : "";
                UIBuilder.CreateText(parent, displayName, 15f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.55f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(startX + i * spacing, -420f), new Vector2(128f, 24f));

                if (isNaldo)
                {
                    UIBuilder.CreateText(parent, "quem fez essa parada aqui", 11f, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f, 0.7f),
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(startX + i * spacing, -440f), new Vector2(150f, 18f));
                }
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
            bool isNewWarRestart = GameState.WarFinished;
            GameState.ResetAll();

            if (isNewWarRestart)
            {
                SceneTransition.Load(firstScene);
            }
            else
            {
                StartCoroutine(PlayIntroThenLoad());
            }
        }

        // ---------------- Intro narration (Fase 0, before the Buteco scene loads) ----------------

        private IEnumerator PlayIntroThenLoad()
        {
            introSkipped = false;

            Canvas canvas = UIBuilder.CreateOverlayCanvas("IntroCanvas", 500);
            UIBuilder.CreatePanel(canvas.transform, new Color(0.02f, 0.02f, 0.03f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject portraitGo = new GameObject("IntroPortrait", typeof(RectTransform));
            portraitGo.transform.SetParent(canvas.transform, false);
            Image portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.preserveAspect = true;
            portraitImg.enabled = false;
            RectTransform portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0.5f, 0.62f);
            portraitRt.anchorMax = new Vector2(0.5f, 0.62f);
            portraitRt.anchoredPosition = Vector2.zero;
            portraitRt.sizeDelta = new Vector2(160f, 160f);

            TMP_Text body = UIBuilder.CreateText(canvas.transform, "", 26f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1200f, 260f));
            UIBuilder.CreateText(canvas.transform, "Enter / Espaço — avançar     Esc — pular", 16f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.5f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(800f, 30f));

            for (int i = 0; i < IntroLines.Length && !introSkipped; i++)
            {
                UpdateIntroPortrait(portraitImg, IntroLines[i]);
                yield return TypeLine(body, IntroLines[i]);
                if (introSkipped)
                {
                    break;
                }
                yield return WaitForAdvance();
            }

            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }

            SceneTransition.Load(firstScene);
        }

        private void UpdateIntroPortrait(Image img, string line)
        {
            Sprite match = null;
            if (castPortraits != null)
            {
                foreach (Sprite s in castPortraits)
                {
                    if (s == null) continue;
                    string n = s.name;
                    if (n.StartsWith("Pedro") && line.Contains("Pedro")) { match = s; break; }
                    if (n.StartsWith("Moe") && line.Contains("Moe")) { match = s; break; }
                    if (n.StartsWith("ReiLuiz") && line.Contains("Rei Luiz")) { match = s; break; }
                    if (n.StartsWith("Funnie") && line.Contains("Funnie")) { match = s; break; }
                    if (n.StartsWith("Julio") && (line.Contains("Machado") || line.Contains("Naldo"))) { match = s; break; }
                    if (n.StartsWith("Dev") && line.Contains("Dev Novato")) { match = s; break; }
                }
            }
            if (img != null)
            {
                img.sprite = match;
                img.enabled = match != null;
            }
        }

        private IEnumerator TypeLine(TMP_Text label, string full)
        {
            label.text = string.Empty;
            for (int i = 0; i <= full.Length; i++)
            {
                if (SkipOrCompletePressed())
                {
                    label.text = full;
                    yield break;
                }
                label.text = full.Substring(0, i);
                yield return new WaitForSecondsRealtime(introCharDelay);
            }
        }

        private IEnumerator WaitForAdvance()
        {
            // Anti-soft-lock: never wait forever for input.
            float t = 0f;
            while (t < introAutoAdvanceTimeout)
            {
                if (AdvancePressed())
                {
                    yield break;
                }
                if (EscapePressed())
                {
                    introSkipped = true;
                    yield break;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private bool SkipOrCompletePressed()
        {
            if (EscapePressed())
            {
                introSkipped = true;
                return true;
            }
            return AdvancePressed();
        }

        private bool AdvancePressed()
        {
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            return false;
        }

        private bool EscapePressed()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
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
