using System;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Data describing a single instance of damage.
    /// </summary>
    public struct DamageInfo
    {
        public float amount;
        public Vector2 knockbackDir;
        public float knockbackForce;
        public GameObject source;

        public DamageInfo(float amount, Vector2 knockbackDir, float knockbackForce, GameObject source)
        {
            this.amount = amount;
            this.knockbackDir = knockbackDir;
            this.knockbackForce = knockbackForce;
            this.source = source;
        }
    }

    /// <summary>
    /// Generic HP/i-frames component shared by player and any hittable target.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float iFrameDuration = 0.4f;

        private float currentHP;
        private float iFrameTimer;
        private float shieldTimer;
        private float shieldDamageMultiplier = 1f;

        public float MaxHP => maxHP;
        public float CurrentHP => currentHP;
        public bool IsDead { get; private set; }

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        /// <summary>
        /// Optional hook to modify a DamageInfo before it's applied (e.g. PlayerBlock
        /// reducing frontal-guard damage to x0.2, or zeroing it entirely on a parry).
        /// Applied once at the very start of TakeDamage, before i-frames/HP/events.
        /// </summary>
        public Func<DamageInfo, DamageInfo> DamageFilter;

        private void Awake()
        {
            currentHP = maxHP;
        }

        private void Update()
        {
            if (iFrameTimer > 0f)
            {
                iFrameTimer -= Time.deltaTime;
            }
            if (shieldTimer > 0f)
            {
                shieldTimer -= Time.deltaTime;
                if (shieldTimer <= 0f)
                {
                    shieldDamageMultiplier = 1f;
                }
            }
        }

        /// <summary>True while a Support ally's blue orb shield is active (see AllyController).</summary>
        public bool IsShielded => shieldTimer > 0f;

        /// <summary>
        /// Applies damage if not dead and not currently in i-frames. Returns true if it was applied.
        /// </summary>
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || iFrameTimer > 0f)
            {
                return false;
            }

            if (DamageFilter != null)
            {
                info = DamageFilter(info);
            }

            // Support's blue-orb shield (Fase 8/9): applied AFTER the guard/parry filter
            // above, so it stacks with (doesn't replace) PlayerBlock's own reduction.
            if (shieldTimer > 0f)
            {
                info.amount *= shieldDamageMultiplier;
            }

            currentHP -= info.amount;
            iFrameTimer = iFrameDuration;

            Damaged?.Invoke(info);

            if (currentHP <= 0f)
            {
                currentHP = 0f;
                IsDead = true;
                Died?.Invoke();
            }

            return true;
        }

        /// <summary>Heals up to maxHP. No-op while dead (revive uses ReviveWithFraction instead).</summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }
            currentHP = Mathf.Min(maxHP, currentHP + amount);
        }

        /// <summary>
        /// Support's blue-orb ability: for 'duration' seconds, incoming damage (after any
        /// DamageFilter) is multiplied by 'multiplier' (e.g. 0.5 = half damage). A new call
        /// simply overwrites the previous shield (no stacking of multipliers).
        /// </summary>
        public void ApplyShield(float duration, float multiplier)
        {
            shieldTimer = Mathf.Max(shieldTimer, duration);
            shieldDamageMultiplier = multiplier;
        }

        public void ResetHealth()
        {
            currentHP = maxHP;
            IsDead = false;
            iFrameTimer = 0f;
        }

        /// <summary>
        /// Revives from a dead state with HP set to the given fraction of max (clamped
        /// 0..1). No-op if not currently dead. Used by AllyController's Knocked ->
        /// Follow revive (comes back at partial HP, not full).
        /// </summary>
        public void ReviveWithFraction(float fraction)
        {
            if (!IsDead)
            {
                return;
            }
            currentHP = Mathf.Clamp01(fraction) * maxHP;
            IsDead = false;
            iFrameTimer = 0f;
        }
    }
}
