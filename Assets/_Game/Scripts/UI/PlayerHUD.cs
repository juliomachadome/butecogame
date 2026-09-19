using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;
using ButecoDosDevs.Systems;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Player HUD: top-left portrait + HP bar + Coragem bar (event-driven, via
    /// Health.Damaged/Died and CourageMeter.Changed); bottom-center action slots
    /// (Attack/Defend/Dash/Interact + empty weapon slot). The only per-frame work is
    /// two cheap field reads (dash cooldown fraction, dialogue-open flag) — no Find.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Health playerHealth;
        [SerializeField] private CourageMeter courage;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private DialogueUI dialogueUI;

        [Header("HP")]
        [SerializeField] private Image hpFill;
        [SerializeField] private TMP_Text hpText;

        [Header("Coragem")]
        [SerializeField] private Image courageFill;

        [Header("Action bar")]
        [SerializeField] private GameObject actionBarRoot;
        [SerializeField] private Image dashCooldownOverlay; // dark radial mask; 1 = fully on cooldown, 0 = ready
        [SerializeField] private TMP_Text weaponNameText; // HUD_ActionBar/WeaponSlot/Label

        [Header("Combat HUD grouping (top-left portrait+HP+Coragem)")]
        [Tooltip("Optional: the top-left stats panel (portrait/HP/Coragem). Hidden together with the action bar before a weapon is chosen (Fase 5, exploration-only) via SetCombatHudVisible.")]
        [SerializeField] private GameObject statsPanelRoot;

        private float lastDisplayedHP = -1f;
        private bool combatHudEnabled = true;
        private bool cutsceneHidden;

        private void Awake()
        {
            if (playerHealth == null)
            {
                playerHealth = GetComponentInParent<Health>();
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged += OnHealthChanged;
                playerHealth.Died += OnHealthDied;
            }
            if (courage != null)
            {
                courage.Changed += OnCourageChanged;
            }

            RefreshHP();
            if (courage != null)
            {
                OnCourageChanged(courage.Value);
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= OnHealthChanged;
                playerHealth.Died -= OnHealthDied;
            }
            if (courage != null)
            {
                courage.Changed -= OnCourageChanged;
            }
        }

        private void Update()
        {
            // Cheap field reads only — no GameObject.Find / component lookups here.
            if (dashCooldownOverlay != null && movement != null)
            {
                dashCooldownOverlay.fillAmount = 1f - movement.DashReadiness01;
            }

            if (actionBarRoot != null)
            {
                bool dialogueOpen = dialogueUI != null && dialogueUI.IsOpen;
                bool shouldShow = combatHudEnabled && !dialogueOpen && !cutsceneHidden;
                if (actionBarRoot.activeSelf != shouldShow)
                {
                    actionBarRoot.SetActive(shouldShow);
                }
            }

            // Cheap field read (no Find) so the bar/text also catch changes that don't
            // fire an event, e.g. PlayerKO's silent ResetHealth() after respawn.
            if (playerHealth != null && !Mathf.Approximately(playerHealth.CurrentHP, lastDisplayedHP))
            {
                RefreshHP();
            }
        }

        private void OnHealthChanged(DamageInfo info)
        {
            RefreshHP();
        }

        private void OnHealthDied()
        {
            RefreshHP();
        }

        private void RefreshHP()
        {
            if (playerHealth == null)
            {
                return;
            }

            float max = Mathf.Max(playerHealth.MaxHP, 0.0001f);
            float current = Mathf.Max(playerHealth.CurrentHP, 0f);

            if (hpFill != null)
            {
                hpFill.fillAmount = current / max;
            }
            if (hpText != null)
            {
                hpText.text = Mathf.CeilToInt(current) + "/" + Mathf.CeilToInt(max);
            }
            lastDisplayedHP = current;
        }

        private void OnCourageChanged(float value)
        {
            if (courageFill != null)
            {
                courageFill.fillAmount = Mathf.Clamp01(value / CourageMeter.MaxValue);
            }
        }

        /// <summary>Called by ButecoFlow's Arsenal step once the player picks a weapon.</summary>
        public void SetWeaponName(string weaponName)
        {
            if (weaponNameText != null)
            {
                weaponNameText.text = weaponName;
            }
        }

        /// <summary>Shows/hides the full combat HUD (stats panel + action bar): before the
        /// arsenal (Fase 5, exploration-only) only OBJETIVO + the [E] prompt should be visible,
        /// so ButecoFlow calls this false on Start and true once a weapon is chosen.</summary>
        public void SetCombatHudVisible(bool visible)
        {
            combatHudEnabled = visible;
            if (statsPanelRoot != null)
            {
                statsPanelRoot.SetActive(visible && !cutsceneHidden);
            }
            if (actionBarRoot != null)
            {
                bool dialogueOpen = dialogueUI != null && dialogueUI.IsOpen;
                actionBarRoot.SetActive(visible && !dialogueOpen && !cutsceneHidden);
            }
        }

        /// <summary>Called by CutsceneMode: forces the whole HUD (stats panel + action
        /// bar) off for the duration of a scripted story beat, regardless of the normal
        /// combat-HUD/dialogue visibility rules, then restores whatever those rules say
        /// once the cutscene ends.</summary>
        public void SetCutsceneHidden(bool hidden)
        {
            cutsceneHidden = hidden;
            if (statsPanelRoot != null)
            {
                statsPanelRoot.SetActive(combatHudEnabled && !cutsceneHidden);
            }
            if (actionBarRoot != null)
            {
                bool dialogueOpen = dialogueUI != null && dialogueUI.IsOpen;
                actionBarRoot.SetActive(combatHudEnabled && !dialogueOpen && !cutsceneHidden);
            }
        }
    }
}
