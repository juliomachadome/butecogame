using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using ButecoDosDevs.Systems;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Options panel shared by the main menu and the pause menu. Built with UIBuilder
    /// (same code-only pattern as the rest of the runtime UI). Reads/writes GameSettings
    /// (PlayerPrefs-backed); APLICAR calls GameSettings.ApplyAll()+Save(), VOLTAR just
    /// closes without discarding already-applied-live values (cycling only updates the
    /// in-memory GameSettings fields, not PlayerPrefs, until APLICAR is pressed).
    /// Self-contained: AddComponent this to any GameObject and call Open()/Close(). Does
    /// NOT touch Time.timeScale — the caller (PauseMenu) owns pause state.
    /// </summary>
    public class OptionsMenu : MonoBehaviour
    {
        private GameObject panelRoot;
        private TextMeshProUGUI screenModeLabel;
        private TextMeshProUGUI resolutionLabel;
        private TextMeshProUGUI qualityLabel;
        private TextMeshProUGUI fpsLabel;
        private TextMeshProUGUI volumeLabel;

        private List<Vector2Int> resolutions;
        private int resolutionIndex;

        public void Open()
        {
            GameSettings.Load();
            if (panelRoot == null)
            {
                Build();
            }
            RefreshLabels();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Build()
        {
            Canvas canvas = UIBuilder.CreateOverlayCanvas("OptionsMenuCanvas", 300, transform);
            panelRoot = UIBuilder.CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -260f), new Vector2(320f, 260f));

            UIBuilder.CreateText(panelRoot.transform, "OPÇÕES", 30f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(500f, 40f));

            BuildResolutions();

            screenModeLabel = BuildRow(90f, "Tela", () => CycleScreenMode(-1), () => CycleScreenMode(1));
            resolutionLabel = BuildRow(35f, "Resolução", () => CycleResolution(-1), () => CycleResolution(1));
            qualityLabel = BuildRow(-20f, "Qualidade", () => CycleQuality(-1), () => CycleQuality(1));
            fpsLabel = BuildRow(-75f, "FPS máx.", () => CycleFps(-1), () => CycleFps(1));
            volumeLabel = BuildRow(-130f, "Volume", () => CycleVolume(-1), () => CycleVolume(1));

            UIBuilder.CreateButton(panelRoot.transform, "APLICAR", new Vector2(-90f, -210f), new Vector2(160f, 46f), OnApply);
            UIBuilder.CreateButton(panelRoot.transform, "VOLTAR", new Vector2(90f, -210f), new Vector2(160f, 46f), Close);

            panelRoot.SetActive(false);
        }

        private void BuildResolutions()
        {
            resolutions = Screen.resolutions
                .Select(r => new Vector2Int(r.width, r.height))
                .Distinct()
                .OrderBy(r => r.x * r.y)
                .ToList();
            if (resolutions.Count == 0)
            {
                resolutions.Add(new Vector2Int(1280, 720));
            }
        }

        private TextMeshProUGUI BuildRow(float y, string label, UnityEngine.Events.UnityAction onLeft, UnityEngine.Events.UnityAction onRight)
        {
            UIBuilder.CreateText(panelRoot.transform, label, 20f, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.85f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, y), new Vector2(150f, 34f));

            UIBuilder.CreateButton(panelRoot.transform, "<", new Vector2(20f, y), new Vector2(40f, 34f), onLeft);
            TextMeshProUGUI value = UIBuilder.CreateText(panelRoot.transform, "-", 20f, TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110f, y), new Vector2(140f, 34f));
            UIBuilder.CreateButton(panelRoot.transform, ">", new Vector2(200f, y), new Vector2(40f, 34f), onRight);
            return value;
        }

        private void RefreshLabels()
        {
            resolutionIndex = resolutions.FindIndex(r => r.x == GameSettings.ResolutionWidth && r.y == GameSettings.ResolutionHeight);
            if (resolutionIndex < 0)
            {
                resolutionIndex = resolutions.Count - 1;
            }

            screenModeLabel.text = ScreenModeName(GameSettings.ScreenMode);
            Vector2Int res = resolutions[resolutionIndex];
            resolutionLabel.text = res.x + "x" + res.y;
            qualityLabel.text = QualityName(GameSettings.QualityLevel);
            fpsLabel.text = GameSettings.TargetFrameRate.ToString();
            volumeLabel.text = GameSettings.VolumePercent + "%";
        }

        private static string ScreenModeName(FullScreenMode mode)
        {
            switch (mode)
            {
                case FullScreenMode.Windowed: return "Janela";
                case FullScreenMode.FullScreenWindow: return "Sem borda";
                default: return "Tela cheia";
            }
        }

        private static string QualityName(GameSettings.Quality q)
        {
            switch (q)
            {
                case GameSettings.Quality.Low: return "Baixa";
                case GameSettings.Quality.High: return "Alta";
                default: return "Média";
            }
        }

        private void CycleScreenMode(int dir)
        {
            FullScreenMode[] modes = { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
            int idx = System.Array.IndexOf(modes, GameSettings.ScreenMode);
            idx = (idx + dir + modes.Length) % modes.Length;
            GameSettings.SetScreenMode(modes[idx]);
            RefreshLabels();
        }

        private void CycleResolution(int dir)
        {
            resolutionIndex = (resolutionIndex + dir + resolutions.Count) % resolutions.Count;
            Vector2Int res = resolutions[resolutionIndex];
            GameSettings.SetResolution(res.x, res.y);
            RefreshLabels();
        }

        private void CycleQuality(int dir)
        {
            int idx = ((int)GameSettings.QualityLevel + dir + 3) % 3;
            GameSettings.SetQuality((GameSettings.Quality)idx);
            RefreshLabels();
        }

        private void CycleFps(int dir)
        {
            int fps = GameSettings.TargetFrameRate >= 60 ? 30 : 60;
            GameSettings.SetTargetFrameRate(fps);
            RefreshLabels();
        }

        private void CycleVolume(int dir)
        {
            GameSettings.SetVolumePercent(GameSettings.VolumePercent + dir * 10);
            RefreshLabels();
        }

        private void OnApply()
        {
            GameSettings.ApplyAll();
            GameSettings.Save();
        }
    }
}
