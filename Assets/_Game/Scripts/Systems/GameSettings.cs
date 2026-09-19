using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Static performance/display settings, persisted in PlayerPrefs. Applied once at
    /// boot (BeforeSceneLoad, before the Menu scene even runs) and re-applied whenever
    /// OptionsMenu's APLICAR is pressed. Also force-disables post-processing on every
    /// camera in every loaded scene (SceneManager.sceneLoaded hook) — the project uses
    /// no post-processing visually, so this only saves the (stripped-but-still-costly)
    /// render pass. No singleton object: everything here is stateless/static, same
    /// exception class as GameState/Sfx.
    /// </summary>
    public static class GameSettings
    {
        public enum Quality { Low, Medium, High }

        private const string KeyFps = "opt_fps";
        private const string KeyVSync = "opt_vsync";
        private const string KeyQuality = "opt_quality";
        private const string KeyScreenMode = "opt_screenmode"; // 0 Windowed, 1 Fullscreen, 2 Borderless
        private const string KeyResW = "opt_resw";
        private const string KeyResH = "opt_resh";
        private const string KeyVolume = "opt_volume"; // 0..100

        public static int TargetFrameRate { get; private set; } = 60;
        public static int VSyncCount { get; private set; } = 1;
        public static Quality QualityLevel { get; private set; } = Quality.Medium;
        public static FullScreenMode ScreenMode { get; private set; } = FullScreenMode.Windowed;
        public static int ResolutionWidth { get; private set; } = 1600;
        public static int ResolutionHeight { get; private set; } = 900;
        public static int VolumePercent { get; private set; } = 100;

        private static bool loaded;
        private static bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Load();
            ApplyAll();
            if (!hooked)
            {
                hooked = true;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DisablePostProcessingOnAllCameras();
        }

        public static void Load()
        {
            if (loaded)
            {
                return;
            }
            loaded = true;
            TargetFrameRate = PlayerPrefs.GetInt(KeyFps, 60);
            VSyncCount = PlayerPrefs.GetInt(KeyVSync, TargetFrameRate <= 30 ? 0 : 1);
            QualityLevel = (Quality)Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, (int)Quality.Medium), 0, 2);
            ScreenMode = (FullScreenMode)PlayerPrefs.GetInt(KeyScreenMode, (int)FullScreenMode.FullScreenWindow);
            ResolutionWidth = PlayerPrefs.GetInt(KeyResW, Display.main.systemWidth);
            ResolutionHeight = PlayerPrefs.GetInt(KeyResH, Display.main.systemHeight);
            VolumePercent = Mathf.Clamp(PlayerPrefs.GetInt(KeyVolume, 100), 0, 100);
        }

        public static void SetTargetFrameRate(int fps)
        {
            TargetFrameRate = fps;
            VSyncCount = fps <= 30 ? 0 : 1;
        }

        public static void SetQuality(Quality q) => QualityLevel = q;
        public static void SetScreenMode(FullScreenMode mode) => ScreenMode = mode;
        public static void SetResolution(int w, int h) { ResolutionWidth = w; ResolutionHeight = h; }
        public static void SetVolumePercent(int percent) => VolumePercent = Mathf.Clamp(percent, 0, 100);

        public static void Save()
        {
            PlayerPrefs.SetInt(KeyFps, TargetFrameRate);
            PlayerPrefs.SetInt(KeyVSync, VSyncCount);
            PlayerPrefs.SetInt(KeyQuality, (int)QualityLevel);
            PlayerPrefs.SetInt(KeyScreenMode, (int)ScreenMode);
            PlayerPrefs.SetInt(KeyResW, ResolutionWidth);
            PlayerPrefs.SetInt(KeyResH, ResolutionHeight);
            PlayerPrefs.SetInt(KeyVolume, VolumePercent);
            PlayerPrefs.Save();
        }

        /// <summary>Applies every current setting to the running game. Safe to call repeatedly.</summary>
        public static void ApplyAll()
        {
            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.vSyncCount = VSyncCount;
            QualitySettings.SetQualityLevel((int)QualityLevel, true);
            ApplyPipelineQuality();
            AudioListener.volume = VolumePercent / 100f;
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, ScreenMode);
            DisablePostProcessingOnAllCameras();
        }

        /// <summary>Cheap per-level knobs on the active URP asset: renderScale + MSAA. No visual change to the 2D art (only sampling/AA cost).</summary>
        private static void ApplyPipelineQuality()
        {
            RenderPipelineAsset active = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
            if (active is UniversalRenderPipelineAsset urp)
            {
                switch (QualityLevel)
                {
                    case Quality.Low:
                        urp.renderScale = 0.75f;
                        urp.msaaSampleCount = 1;
                        break;
                    case Quality.High:
                        urp.renderScale = 1f;
                        urp.msaaSampleCount = 2;
                        break;
                    default: // Medium
                        urp.renderScale = 1f;
                        urp.msaaSampleCount = 1;
                        break;
                }
            }
        }

        private static void DisablePostProcessingOnAllCameras()
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == null)
                {
                    continue;
                }
                UniversalAdditionalCameraData data = cameras[i].GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    data.renderPostProcessing = false;
                }
            }
        }
    }
}
