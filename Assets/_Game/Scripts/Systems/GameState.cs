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

        /// <summary>True while the player is on a "passeio" trip to the Rua scene before
        /// the war (door in the Buteco -> door in the Rua), set/cleared by SceneDoor.</summary>
        public static bool StreetVisit { get; set; }

        /// <summary>True once ButecoFlow's arrival gag (Pedro's "Tem 18?" gate) has played.
        /// Lets a return trip from a street visit skip straight back to free-roam instead
        /// of replaying the whole intro.</summary>
        public static bool IntroDone { get; set; }

        /// <summary>Coarse "how far into the Buteco story are we" marker, updated by
        /// ButecoFlow at the start of each major step. CanVisitStreet already restricts
        /// street trips to Explore/PedroLeaves, so a scene reload mid-story can't reach a
        /// state this doesn't cover — this exists purely so a reload (e.g. a future debug
        /// menu, or the street-visit door round-trip) can resume from the right stage
        /// instead of always restarting at Explore.</summary>
        public enum ButecoStage
        {
            Arrival,
            Explore,
            PedroLeaves,
            PedroReturns,
            Arsenal,
            Bora
        }

        public static ButecoStage CurrentButecoStage { get; set; } = ButecoStage.Arrival;

        /// <summary>0-based index of the last BarRival wave the player reached (set by
        /// BarRivalFlow as each wave starts). Used as a checkpoint: if the player is
        /// knocked out and the battle scene reloads, WaveSpawner.StartWaveIndex resumes
        /// from here instead of forcing a full replay from wave 1.</summary>
        public static int BarRivalWaveIndex { get; set; }

        /// <summary>The chosen weapon's held-item sprite (set once by ButecoFlow's arsenal
        /// pickup), so Rua/BarRival can re-show it in the player's hand on scene start
        /// without needing their own copy of the sword/bottle art references.</summary>
        public static Sprite ChosenWeaponSprite { get; set; }

        /// <summary>Reset everything a new "Guerra Púnica" run needs cleared: called by the
        /// main menu's JOGAR button and by "NOVA GUERRA PÚNICA" (pause menu / epilogue).</summary>
        public static void ResetAll()
        {
            ChosenWeapon = Weapon.None;
            ChosenWeaponSprite = null;
            WarFinished = false;
            StreetVisit = false;
            IntroDone = false;
            CurrentButecoStage = ButecoStage.Arrival;
            BarRivalWaveIndex = 0;
        }

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
        /// Applies ChosenWeapon's stats to a PlayerAttack, AND re-shows the weapon in
        /// the player's hand (sprite + scale + active) by locating the conventional
        /// "Visual/HeldItem_Weapon" child under attack's own GameObject — no extra
        /// Inspector wiring needed in Rua/BarRival's flow scripts, they already call
        /// this once in Awake with the same 2 args. Safe to call with weapon = None
        /// (falls back to Balanced for stats; the visual step still no-ops if
        /// ChosenWeaponSprite hasn't been set yet, e.g. testing a scene standalone).
        /// </summary>
        public static void ApplyWeapon(PlayerAttack attack, Weapon weapon)
        {
            if (weapon == Weapon.None)
            {
                weapon = Weapon.Balanced;
            }
            WeaponStats stats = GetWeaponStats(weapon);
            if (attack == null)
            {
                return;
            }

            attack.SetWeapon(stats.damage, stats.timeMultiplier, stats.knockbackMultiplier);

            if (ChosenWeaponSprite == null)
            {
                return;
            }
            Transform held = attack.transform.Find("Visual/HeldItem_Weapon");
            if (held == null)
            {
                return;
            }
            SpriteRenderer heldRenderer = held.GetComponent<SpriteRenderer>();
            if (heldRenderer != null)
            {
                heldRenderer.sprite = ChosenWeaponSprite;
            }
            held.localScale = Vector3.one * stats.visualScale;
            held.gameObject.SetActive(true);
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
