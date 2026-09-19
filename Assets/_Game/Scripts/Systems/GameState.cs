using UnityEngine;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Minimal cross-scene game state. This is the project's 3rd deliberate exception
    /// to the "no singletons" rule (the other two are HitStop's static runner and
    /// CombatantRegistry's static team lists) — a plain static class, no MonoBehaviour,
    /// no DontDestroyOnLoad object to manage. Written by ButecoFlow (Arsenal step) and
    /// read by whatever loads the "Rua" scene next (weapon stats, war-finished flag).
    /// </summary>
    public static class GameState
    {
        public enum Weapon
        {
            None,
            Balanced,
            BigSlow,
            ShortFast,
            Bottle
        }

        public static Weapon ChosenWeapon { get; set; } = Weapon.None;
        public static bool WarFinished { get; set; }

        /// <summary>Human-readable label for HUD/weapon slot.</summary>
        public static string WeaponDisplayName(Weapon weapon)
        {
            switch (weapon)
            {
                case Weapon.Balanced: return "Espada Equilibrada";
                case Weapon.BigSlow: return "Espadona";
                case Weapon.ShortFast: return "Espada Curta";
                case Weapon.Bottle: return "Garrafa";
                default: return "-";
            }
        }
    }
}
