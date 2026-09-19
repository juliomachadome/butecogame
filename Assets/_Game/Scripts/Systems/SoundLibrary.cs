using UnityEngine;

namespace ButecoDosDevs.Systems
{
    /// <summary>Every gameplay sound event the Buteco can fire. No audio yet (team records their own — see CLAUDE.md); this is just the slot list.</summary>
    public enum SoundId
    {
        Rua,
        Aplausos,
        Vaias,
        Risadas,
        Burp,
        IsqueiroFalha,
        IsqueiroAcende,
        DardoNerf,
        DardoAcerta,
        CanecaMesa,
        GolpeSwing,
        GolpeAcerto,
        PlayerDano,
        Parry,
        Dash,
        PortaAbre,
        Timeout,
        BoraGrito,
        MusicaBar
    }

    /// <summary>
    /// One AudioClip slot per SoundId. All slots start empty (null) on purpose — the
    /// team records real SFX later and drops them in here; until then every slot is a
    /// silent no-op (see Sfx.Play). One asset lives at Assets/_Game/Audio/SoundLibrary.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "ButecoDosDevs/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("Soundboard / crowd")]
        public AudioClip rua;
        public AudioClip aplausos;
        public AudioClip vaias;
        public AudioClip risadas;
        public AudioClip burp;

        [Header("Pedro / isqueiro")]
        public AudioClip isqueiroFalha;
        public AudioClip isqueiroAcende;

        [Header("Nerf chaos")]
        public AudioClip dardoNerf;
        public AudioClip dardoAcerta;

        [Header("Bar props")]
        public AudioClip canecaMesa;

        [Header("Combat (wired in a later phase)")]
        public AudioClip golpeSwing;
        public AudioClip golpeAcerto;
        public AudioClip playerDano;
        public AudioClip parry;
        public AudioClip dash;

        [Header("Story beats")]
        public AudioClip portaAbre;
        public AudioClip timeout;
        public AudioClip boraGrito;

        [Header("Music (loop, wired in a later phase)")]
        public AudioClip musicaBar;

        public AudioClip Get(SoundId id)
        {
            switch (id)
            {
                case SoundId.Rua: return rua;
                case SoundId.Aplausos: return aplausos;
                case SoundId.Vaias: return vaias;
                case SoundId.Risadas: return risadas;
                case SoundId.Burp: return burp;
                case SoundId.IsqueiroFalha: return isqueiroFalha;
                case SoundId.IsqueiroAcende: return isqueiroAcende;
                case SoundId.DardoNerf: return dardoNerf;
                case SoundId.DardoAcerta: return dardoAcerta;
                case SoundId.CanecaMesa: return canecaMesa;
                case SoundId.GolpeSwing: return golpeSwing;
                case SoundId.GolpeAcerto: return golpeAcerto;
                case SoundId.PlayerDano: return playerDano;
                case SoundId.Parry: return parry;
                case SoundId.Dash: return dash;
                case SoundId.PortaAbre: return portaAbre;
                case SoundId.Timeout: return timeout;
                case SoundId.BoraGrito: return boraGrito;
                case SoundId.MusicaBar: return musicaBar;
                default: return null;
            }
        }
    }
}
