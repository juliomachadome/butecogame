using UnityEngine;
using UnityEngine.InputSystem;
using ButecoDosDevs.Combat;
using ButecoDosDevs.NPC;
using ButecoDosDevs.Systems;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Player
{
    /// <summary>
    /// Guard / parry. Hold to guard: half move speed (via PlayerMovement.SetGuarding),
    /// no attack, no dash. Installs itself as the sibling Health's DamageFilter so it
    /// can rewrite incoming damage before it's applied:
    ///   - Not guarding: damage passes through unchanged.
    ///   - Guarding, hit from the front (angle to attacker &lt;= frontalAngle): x0.2
    ///     damage and knockback.
    ///   - Guarding, hit from the front AND guard started &lt;= parryWindow ago: a PARRY —
    ///     0 damage, attacker is Staggered, +courage, "PARRY!" floating text, and the
    ///     player's next attack (within attackBonusWindow) deals x1.5 damage.
    ///   - Guarding, hit from behind: full damage (guard doesn't protect the back).
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerBlock : MonoBehaviour
    {
        [Header("Guard")]
        [SerializeField] private float frontalAngle = 90f;
        [SerializeField] private float frontalDamageMultiplier = 0.2f;

        [Header("Parry")]
        [SerializeField] private float parryWindow = 0.15f;
        [SerializeField] private float parryStaggerDuration = 0.6f;
        [SerializeField] private float attackBonusMultiplier = 1.5f;
        [SerializeField] private float attackBonusWindow = 1.0f;
        [SerializeField] private float parryHitStop = 0.08f;

        [Header("Refs")]
        [SerializeField] private CourageMeter courage;
        [SerializeField] private SpriteRenderer guardIndicator;
        [SerializeField] private AttackFX attackFX;
        [SerializeField] private CameraFollow2D cameraShake;

        [Header("Feedback")]
        [SerializeField] private float parryShakeIntensity = 0.1f;
        [SerializeField] private float parryShakeDuration = 0.12f;
        [SerializeField] private float parrySparkScale = 1.8f;

        private Health health;
        private PlayerMovement movement;
        private InputAction blockAction;

        private bool isBlocking;
        private float guardStartTime;
        private float attackBonusExpireTime = -1f;

        public bool IsBlocking => isBlocking;

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
            if (courage == null)
            {
                courage = GetComponent<CourageMeter>();
            }
            if (attackFX == null)
            {
                attackFX = GetComponent<AttackFX>();
            }
            if (cameraShake == null)
            {
                Camera main = Camera.main;
                cameraShake = main != null ? main.GetComponent<CameraFollow2D>() : FindAnyObjectByType<CameraFollow2D>();
            }

            blockAction = new InputAction(name: "Block", type: InputActionType.Button);
            blockAction.AddBinding("<Mouse>/rightButton");
            blockAction.AddBinding("<Keyboard>/k");
            blockAction.AddBinding("<Gamepad>/leftShoulder");
            blockAction.started += OnBlockStarted;
            blockAction.canceled += OnBlockCanceled;

            if (guardIndicator != null)
            {
                guardIndicator.enabled = false;
            }
        }

        private void OnEnable()
        {
            blockAction?.Enable();
            health.DamageFilter = ModifyIncomingDamage;
        }

        private void OnDisable()
        {
            blockAction?.Disable();
            StopBlocking();
            if (health.DamageFilter == (System.Func<DamageInfo, DamageInfo>)ModifyIncomingDamage)
            {
                health.DamageFilter = null;
            }
        }

        private void OnDestroy()
        {
            if (blockAction != null)
            {
                blockAction.started -= OnBlockStarted;
                blockAction.canceled -= OnBlockCanceled;
            }
            blockAction?.Dispose();
        }

        private void OnBlockStarted(InputAction.CallbackContext ctx)
        {
            isBlocking = true;
            guardStartTime = Time.time;
            movement.SetGuarding(true);
            if (guardIndicator != null)
            {
                guardIndicator.enabled = true;
            }
        }

        private void OnBlockCanceled(InputAction.CallbackContext ctx)
        {
            StopBlocking();
        }

        private void StopBlocking()
        {
            isBlocking = false;
            if (movement != null)
            {
                movement.SetGuarding(false);
            }
            if (guardIndicator != null)
            {
                guardIndicator.enabled = false;
            }
        }

        private void Update()
        {
            if (isBlocking && guardIndicator != null)
            {
                Vector2 dir = movement.LastMoveDirection;
                guardIndicator.transform.localPosition = dir * 0.6f;

                // FX_Shield art points toward +X (east); mirror instead of rotating a
                // full half-turn for west so the arc keeps facing the same way, matching
                // AttackFX's slash orientation convention.
                bool isWest = dir.x < -0.0001f && Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
                Vector2 rotDir = isWest ? new Vector2(-dir.x, dir.y) : dir;
                float angle = Mathf.Atan2(rotDir.y, rotDir.x) * Mathf.Rad2Deg;
                guardIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                guardIndicator.flipX = isWest;
            }
        }

        /// <summary>
        /// Returns the current bonus multiplier for the player's next outgoing attack
        /// (from a recent parry) and clears it (single use). 1 if no bonus is active.
        /// </summary>
        public float ConsumeAttackBonus()
        {
            if (attackBonusExpireTime >= 0f && Time.time <= attackBonusExpireTime)
            {
                attackBonusExpireTime = -1f;
                return attackBonusMultiplier;
            }
            attackBonusExpireTime = -1f;
            return 1f;
        }

        private DamageInfo ModifyIncomingDamage(DamageInfo info)
        {
            if (!isBlocking)
            {
                return info;
            }

            Vector2 towardAttacker = -info.knockbackDir; // knockbackDir points attacker->target
            float angle = towardAttacker.sqrMagnitude > 0.0001f
                ? Vector2.Angle(movement.LastMoveDirection, towardAttacker)
                : 0f;

            bool isFrontal = angle <= frontalAngle;
            if (!isFrontal)
            {
                // Hit from behind: guard doesn't help.
                return info;
            }

            bool isParry = (Time.time - guardStartTime) <= parryWindow;
            if (isParry)
            {
                HandleParry(info);
                info.amount = 0f;
                info.knockbackForce = 0f;
                return info;
            }

            info.amount *= frontalDamageMultiplier;
            info.knockbackForce *= frontalDamageMultiplier;
            return info;
        }

        private void HandleParry(DamageInfo info)
        {
            if (info.source != null)
            {
                EnemyController enemy = info.source.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.Stagger(parryStaggerDuration);
                }
            }

            attackBonusExpireTime = Time.time + attackBonusWindow;

            if (courage != null)
            {
                courage.OnParry();
            }

            HitStop.Trigger(parryHitStop);
            DamageNumbers.SpawnParry(transform.position + Vector3.up * 0.6f);

            if (attackFX != null)
            {
                attackFX.PlayImpact(transform.position + Vector3.up * 0.5f, parrySparkScale);
            }
            if (cameraShake != null)
            {
                cameraShake.Shake(parryShakeIntensity, parryShakeDuration);
            }
        }
    }
}
