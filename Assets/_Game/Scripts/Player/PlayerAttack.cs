using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Systems;

namespace ButecoDosDevs.Player
{
    /// <summary>
    /// Player melee attack: windup -> active window (hitbox checks) -> recovery.
    /// Input built in code (J, mouse left button, gamepad buttonWest), matching
    /// PlayerMovement's style. Not usable during dash or knockback.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAttack : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField] private float damage = 30f;
        [SerializeField] private float knockbackForce = 6f;
        [SerializeField] private float windupTime = 0.06f;
        [SerializeField] private float activeTime = 0.10f;
        [SerializeField] private float recoveryTime = 0.18f;
        [SerializeField] private float hitboxDistance = 0.7f;
        [SerializeField] private float hitStopDuration = 0.06f;

        [Header("Refs")]
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private SpriteRenderer hitboxSprite;
        [SerializeField] private PlayerBlock block;
        [SerializeField] private CourageMeter courage;
        [SerializeField] private AttackFX attackFX;
        [SerializeField] private CameraFollow2D cameraShake;

        [Header("Feedback")]
        [SerializeField] private float hitShakeIntensity = 0.08f;
        [SerializeField] private float hitShakeDuration = 0.1f;

        private PlayerMovement movement;
        private InputAction attackAction;
        private bool isAttacking;
        private Coroutine attackRoutine;
        private float currentAttackDamage;

        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            if (block == null)
            {
                block = GetComponent<PlayerBlock>();
            }
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

            if (hitbox != null)
            {
                hitbox.SetOwner(gameObject);
                hitbox.SetOwnerTeam(Team.Player);
                hitbox.Close();
                hitbox.OnHit += OnHitboxHit;
            }

            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }

            attackAction = new InputAction(name: "Attack", type: InputActionType.Button);
            attackAction.AddBinding("<Keyboard>/j");
            attackAction.AddBinding("<Mouse>/leftButton");
            attackAction.AddBinding("<Gamepad>/buttonWest");
            attackAction.performed += OnAttackPerformed;
        }

        private void OnEnable()
        {
            attackAction?.Enable();
        }

        private void OnDisable()
        {
            attackAction?.Disable();
            StopAttackImmediate();
        }

        private void OnDestroy()
        {
            if (attackAction != null)
            {
                attackAction.performed -= OnAttackPerformed;
            }
            if (hitbox != null)
            {
                hitbox.OnHit -= OnHitboxHit;
            }
            attackAction?.Dispose();
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            TryAttack();
        }

        private void OnHitboxHit(Health target, DamageInfo info)
        {
            HitStop.Trigger(hitStopDuration);
            if (courage != null)
            {
                courage.OnHitLanded();
            }
            if (attackFX != null && target != null)
            {
                attackFX.PlayImpact(target.transform.position);
            }
            if (cameraShake != null)
            {
                cameraShake.Shake(hitShakeIntensity, hitShakeDuration);
            }
        }

        /// <summary>
        /// Attempts to start an attack. Public for test/reflection use.
        /// </summary>
        public bool TryAttack()
        {
            if (isAttacking || movement.IsDashing || movement.IsKnockedBack)
            {
                return false;
            }

            if (block != null && block.IsBlocking)
            {
                return false;
            }

            currentAttackDamage = block != null ? damage * block.ConsumeAttackBonus() : damage;
            attackRoutine = StartCoroutine(AttackSequence());
            return true;
        }

        private void StopAttackImmediate()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            isAttacking = false;
            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }
        }

        private IEnumerator AttackSequence()
        {
            isAttacking = true;

            Vector2 dir = movement.LastMoveDirection;
            if (hitbox != null)
            {
                hitbox.transform.localPosition = dir * hitboxDistance;
            }

            if (attackFX != null)
            {
                attackFX.PlayWindupSquash(windupTime);
            }

            yield return WaitFrames(windupTime);

            if (hitbox != null)
            {
                hitbox.Open();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = true;
            }
            if (attackFX != null)
            {
                attackFX.PlaySwing(dir, activeTime);
            }

            float t = 0f;
            while (t < activeTime)
            {
                hitbox?.CheckHits(currentAttackDamage, knockbackForce);
                t += Time.deltaTime;
                yield return null;
            }

            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }

            yield return WaitFrames(recoveryTime);

            isAttacking = false;
            attackRoutine = null;
        }

        private static IEnumerator WaitFrames(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
