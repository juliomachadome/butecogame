using System;
using UnityEngine;
using ButecoDosDevs.Combat;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Player "Coragem" (courage) meter, 0-100, starts at 50. +5 on landing a hit,
    /// +10 on a parry, -8 when the player takes damage. Purely a displayed stat in
    /// this phase (HUD bar) — no gameplay effect yet; that comes in a later phase.
    /// Reads Health.Damaged directly (own reference, no Find) to apply the -8.
    /// </summary>
    public class CourageMeter : MonoBehaviour
    {
        public const float MaxValue = 100f;
        public const float MinValue = 0f;

        [SerializeField] private float startValue = 50f;
        [SerializeField] private float hitGain = 5f;
        [SerializeField] private float parryGain = 10f;
        [SerializeField] private float damageLoss = 8f;
        [SerializeField] private Health health;

        private float value;

        public float Value => value;

        /// <summary>Fired whenever the value changes, with the new (already clamped) value.</summary>
        public event Action<float> Changed;

        private void Awake()
        {
            value = Mathf.Clamp(startValue, MinValue, MaxValue);
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnPlayerDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnPlayerDamaged;
            }
        }

        private void OnPlayerDamaged(DamageInfo info)
        {
            // Zero-damage hits are parries, handled explicitly via OnParry (+10) instead.
            if (info.amount > 0f)
            {
                Add(-damageLoss);
            }
        }

        public void OnHitLanded()
        {
            Add(hitGain);
        }

        public void OnParry()
        {
            Add(parryGain);
        }

        public void Add(float amount)
        {
            float clamped = Mathf.Clamp(value + amount, MinValue, MaxValue);
            if (!Mathf.Approximately(clamped, value))
            {
                value = clamped;
                Changed?.Invoke(value);
            }
        }
    }
}
