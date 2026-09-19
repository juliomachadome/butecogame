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

        public float MaxHP => maxHP;
        public float CurrentHP => currentHP;
        public bool IsDead { get; private set; }

        public event Action<DamageInfo> Damaged;
        public event Action Died;

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
        }

        /// <summary>
        /// Applies damage if not dead and not currently in i-frames. Returns true if it was applied.
        /// </summary>
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || iFrameTimer > 0f)
            {
                return false;
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

        public void ResetHealth()
        {
            currentHP = maxHP;
            IsDead = false;
            iFrameTimer = 0f;
        }
    }
}
