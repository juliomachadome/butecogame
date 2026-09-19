using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ButecoDosDevs.Player;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Global "we're watching a scripted story beat" mode: shows black letterbox bars,
    /// hides the combat/objective HUD, and disables player movement/attack input.
    /// Static (no scene wiring needed) like SceneTransition/TitleCard/NewWarPrompt: a
    /// hidden Runner GameObject hosts the coroutine a plain static class can't host
    /// itself. Begin/End are idempotent, and a watchdog auto-reverts everything after
    /// maxSeconds in case a caller's coroutine gets interrupted, so the player can never
    /// be left stuck with input disabled and bars on screen forever (anti-soft-lock).
    /// </summary>
    public static class CutsceneMode
    {
        private class Runner : MonoBehaviour { }

        private const float BarHeight = 70f;
        private const float WatchdogSeconds = 60f;

        private static Runner runner;
        private static GameObject barsRoot;
        private static Coroutine watchdog;

        private static bool active;
        private static PlayerMovement movement;
        private static PlayerAttack attack;
        private static PlayerHUD hud;
        private static GameObject[] extraHiddenRoots;

        public static bool IsActive => active;

        /// <summary>
        /// Starts a cutscene: disables the given player components, hides the HUD (via
        /// PlayerHUD.SetCutsceneHidden) plus any extra GameObjects (e.g. the ObjectiveUI
        /// panel), and shows the letterbox bars. Safe to call again while already active
        /// (just updates the tracked refs; never stacks bars/canvases).
        /// </summary>
        public static void Begin(PlayerMovement playerMovement, PlayerAttack playerAttack, PlayerHUD playerHud, params GameObject[] extraRootsToHide)
        {
            movement = playerMovement;
            attack = playerAttack;
            hud = playerHud;
            extraHiddenRoots = extraRootsToHide;

            if (movement != null) movement.enabled = false;
            if (attack != null) attack.enabled = false;
            if (hud != null) hud.SetCutsceneHidden(true);
            SetExtraRoots(false);

            EnsureBars();
            barsRoot.SetActive(true);

            EnsureRunner();
            if (watchdog != null)
            {
                runner.StopCoroutine(watchdog);
            }
            watchdog = runner.StartCoroutine(Watchdog(WatchdogSeconds));

            active = true;
        }

        /// <summary>Reverts everything Begin() changed. Safe to call even if not active (no-op).</summary>
        public static void End()
        {
            if (!active)
            {
                return;
            }
            active = false;

            if (movement != null) movement.enabled = true;
            if (attack != null) attack.enabled = true;
            if (hud != null) hud.SetCutsceneHidden(false);
            SetExtraRoots(true);

            if (barsRoot != null)
            {
                barsRoot.SetActive(false);
            }

            if (runner != null && watchdog != null)
            {
                runner.StopCoroutine(watchdog);
            }
            watchdog = null;
        }

        private static void SetExtraRoots(bool activeState)
        {
            if (extraHiddenRoots == null)
            {
                return;
            }
            for (int i = 0; i < extraHiddenRoots.Length; i++)
            {
                if (extraHiddenRoots[i] != null)
                {
                    extraHiddenRoots[i].SetActive(activeState);
                }
            }
        }

        private static IEnumerator Watchdog(float maxSeconds)
        {
            yield return new WaitForSeconds(maxSeconds);
            End();
        }

        private static void EnsureRunner()
        {
            if (runner != null)
            {
                return;
            }
            GameObject go = new GameObject("CutsceneModeRunner");
            runner = go.AddComponent<Runner>();
        }

        private static void EnsureBars()
        {
            if (barsRoot != null)
            {
                return;
            }

            Canvas canvas = UIBuilder.CreateOverlayCanvas("CutsceneBarsCanvas", 3000);
            barsRoot = canvas.gameObject;

            CreateBar(canvas.transform, top: true);
            CreateBar(canvas.transform, top: false);

            barsRoot.SetActive(false);
        }

        private static void CreateBar(Transform parent, bool top)
        {
            GameObject go = new GameObject(top ? "TopBar" : "BottomBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = Color.black;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (top)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
            }
            rt.sizeDelta = new Vector2(0f, BarHeight);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
