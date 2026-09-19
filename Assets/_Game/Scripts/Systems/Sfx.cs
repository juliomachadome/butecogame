using UnityEngine;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Minimal static one-shot SFX helper. Silently does nothing (no error, no
    /// warning) whenever the library isn't set or the requested slot's clip is empty —
    /// the whole point is that the game plays fine with zero audio while the team
    /// records real SFX. Bootstrapped once by ButecoFlow.Awake via SetLibrary(); static
    /// so any script can call Sfx.Play without a scene reference (cleared automatically
    /// on the next domain reload/play session, same pattern as GameState/CombatantRegistry).
    /// </summary>
    public static class Sfx
    {
        private static SoundLibrary library;

        public static void SetLibrary(SoundLibrary lib)
        {
            library = lib;
        }

        /// <summary>Plays a one-shot clip at pos (or the main camera's position if omitted). No-op if the slot has no clip yet.</summary>
        public static void Play(SoundId id, Vector3? pos = null)
        {
            if (library == null)
            {
                return;
            }

            AudioClip clip = library.Get(id);
            if (clip == null)
            {
                return;
            }

            Vector3 worldPos = pos ?? (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            AudioSource.PlayClipAtPoint(clip, worldPos);
        }
    }
}
