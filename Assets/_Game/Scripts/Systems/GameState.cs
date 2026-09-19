using UnityEngine;
using ButecoDosDevs.Player;

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

        /// <summary>Combat stats for a weapon: damage, windup/active/recovery time multiplier, knockback multiplier, held-item visual scale.</summary>
        public struct WeaponStats
        {
            public float damage;
            public float timeMultiplier;
            public float knockbackMultiplier;
            public float visualScale;
        }

        /// <summary>
        /// Single source of truth for weapon stats (Fase 7 arsenal in ButecoFlow, and
        /// GameState.ChosenWeapon applied again on scene load in Rua/BarRival — see
        /// ApplyWeapon below). Balanced is also the anti-soft-lock fallback.
        /// </summary>
        public static WeaponStats GetWeaponStats(Weapon weapon)
        {
            switch (weapon)
            {
                case Weapon.BigSlow:
                    return new WeaponStats { damage = 45f, timeMultiplier = 1.5f, knockbackMultiplier = 1f, visualScale = 1.4f };
                case Weapon.ShortFast:
                    return new WeaponStats { damage = 22f, timeMultiplier = 0.65f, knockbackMultiplier = 1f, visualScale = 0.7f };
                case Weapon.Bottle:
                    return new WeaponStats { damage = 26f, timeMultiplier = 1f, knockbackMultiplier = 2f, visualScale = 0.8f };
                default:
                    return new WeaponStats { damage = 30f, timeMultiplier = 1f, knockbackMultiplier = 1f, visualScale = 1f };
            }
        }

        /// <summary>
        /// Applies ChosenWeapon's stats to a PlayerAttack (and, optionally, a held-item
        /// visual). Safe to call with weapon = None (falls back to Balanced) — used both
        /// by ButecoFlow's arsenal pickups and by Rua/BarRival on scene start so the
        /// player keeps their chosen weapon across scenes.
        /// </summary>
        public static void ApplyWeapon(PlayerAttack attack, Weapon weapon)
        {
            if (weapon == Weapon.None)
            {
                weapon = Weapon.Balanced;
            }
            WeaponStats stats = GetWeaponStats(weapon);
            if (attack != null)
            {
                attack.SetWeapon(stats.damage, stats.timeMultiplier, stats.knockbackMultiplier);
            }
        }

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
