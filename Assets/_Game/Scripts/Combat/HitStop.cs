using System.Collections;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Brief hit-stop pause via Time.timeScale. Static utility with an on-demand,
    /// DontDestroyOnLoad runner MonoBehaviour to host the coroutine.
    /// NOTE: this is the one accepted exception to "no singletons" in this project —
    /// it's needed so timeScale is always restored deterministically (including when
    /// play mode stops), never left stuck at 0. See Runner.OnDestroy below.
    /// </summary>
    public static class HitStop
    {
        private class Runner : MonoBehaviour
        {
            private void OnDestroy()
            {
                Time.timeScale = 1f;
            }
        }

        private static Runner runner;
        private static Coroutine activeRoutine;
        private static float previousTimeScale = 1f;

        public static void Trigger(float duration = 0.06f)
        {
            duration = Mathf.Clamp(duration, 0.04f, 0.08f);
            EnsureRunner();

            if (activeRoutine != null)
            {
                // Already paused: don't stack, just refresh the timer.
                runner.StopCoroutine(activeRoutine);
            }
            else
            {
                previousTimeScale = Time.timeScale;
            }

            activeRoutine = runner.StartCoroutine(Routine(duration));
        }

        private static IEnumerator Routine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            activeRoutine = null;
        }

        private static void EnsureRunner()
        {
            if (runner != null)
            {
                return;
            }

            activeRoutine = null; // a routine from a destroyed runner is gone with it
            var go = new GameObject("HitStopRunner");
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }

        /// <summary>Forces timeScale back to 1 immediately (safety net for tests/tools).</summary>
        public static void ForceReset()
        {
            if (runner != null && activeRoutine != null)
            {
                runner.StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            Time.timeScale = 1f;
        }
    }
}
