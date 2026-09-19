using System.Collections.Generic;
using UnityEngine;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Simple melee enemy ("Hacker Rival" placeholder). State machine (enum + switch):
    /// Idle -> Chase -> Windup -> Attack -> Recover -> Chase, plus Hurt and KO.
    /// No pathfinding: moves straight at the target via Rigidbody2D velocity and lets
    /// Box2D resolve walls; if stuck for ~1s it slides on the perpendicular axis briefly.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        private enum State { Idle, Chase, Windup, Attack, Recover, Hurt, KO }

        [Header("Refs")]
        [SerializeField] private Transform target;
        [SerializeField] private Health health;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D solidCollider;
        [SerializeField] private Collider2D hurtboxCollider;
        [SerializeField] private SpriteRenderer hitboxSprite;
        [SerializeField] private AttackFX attackFX;

        [Header("Detection / Movement")]
        [SerializeField] private float detectRadius = 7f;
        [SerializeField] private float retargetInterval = 0.5f;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float stuckCheckInterval = 1f;
        [SerializeField] private float stuckMinDistance = 0.15f;
        [SerializeField] private float slideTime = 0.5f;

        [Header("Attack")]
        [SerializeField] private float windupTime = 0.45f;
        [SerializeField] private float attackActiveTime = 0.12f;
        [SerializeField] private float recoverTime = 0.6f;
        [SerializeField] private float damage = 12f;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float hitboxDistance = 0.7f;
        [SerializeField] private Color windupColor = new Color(1f, 0.05f, 0.05f, 1f);

        [Header("Hurt / KO")]
        [SerializeField] private float hurtStunTime = 0.25f;
        [SerializeField] private Color koColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        [SerializeField] private bool respawnForTesting = true;
        [SerializeField] private float respawnDelay = 5f;

        [Header("Formation / separação (combate mais natural)")]
        [SerializeField] private float separationRadius = 0.8f;
        [SerializeField] private float separationStrength = 1f;
        [SerializeField] private float surroundRadiusFactor = 0.85f;
        [SerializeField] private float attackQueueStrafeRadius = 2.5f;
        [SerializeField] private float attackQueueStrafeSpeed = 40f; // deg/s

        private float surroundAngle;

        private State state = State.Idle;
        private float stateTimer;
        private float respawnTimer;

        private Vector2 attackDir;
        private Vector2 moveDir;

        private bool isSliding;
        private float slideTimer;
        private Vector2 slideDir;
        private float stuckCheckTimer;
        private Vector2 lastStuckPosition;

        private Health targetHealth;
        private PlayerKO targetKO;
        private float retargetTimer;

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Color originalColor;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (target == null)
            {
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    target = playerGo.transform;
                }
            }

            if (target != null)
            {
                targetHealth = target.GetComponent<Health>();
                targetKO = target.GetComponent<PlayerKO>();
            }

            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            if (attackFX == null)
            {
                attackFX = GetComponent<AttackFX>();
            }

            if (hitbox != null)
            {
                hitbox.SetOwner(gameObject);
                hitbox.SetOwnerTeam(Team.Enemy);
                hitbox.Close();
                hitbox.OnHit += OnHitboxHit;
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }

            lastStuckPosition = transform.position;
            stuckCheckTimer = stuckCheckInterval;
            retargetTimer = 0f; // retarget immediately on the first Update

            // Per-instance variation so a crowd doesn't move/attack in lockstep.
            surroundAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            moveSpeed *= Random.Range(0.88f, 1.12f);
            recoverTime *= Random.Range(0.8f, 1.2f);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void OnDestroy()
        {
            EnemyAttackQueue.Release(this);
            if (hitbox != null)
            {
                hitbox.OnHit -= OnHitboxHit;
            }
        }

        private void OnHitboxHit(Health hitHealth, DamageInfo info)
        {
            if (attackFX != null && hitHealth != null)
            {
                attackFX.PlayImpact(hitHealth.transform.position);
            }
        }

        private void Update()
        {
            // Re-pick the closest live target between Team.Player and Team.Ally every
            // ~0.5s (not every frame) while free to move/notice; never mid-attack, and
            // never while down.
            if (state == State.Idle || state == State.Chase)
            {
                retargetTimer -= Time.deltaTime;
                if (retargetTimer <= 0f)
                {
                    RetargetNearest();
                }
            }

            switch (state)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.Chase:
                    TickChase();
                    break;
                case State.Windup:
                    TickWindup();
                    break;
                case State.Attack:
                    TickAttack();
                    break;
                case State.Recover:
                    TickRecover();
                    break;
                case State.Hurt:
                    TickHurt();
                    break;
                case State.KO:
                    TickKO();
                    break;
            }
        }

        private void FixedUpdate()
        {
            switch (state)
            {
                case State.Chase:
                    rb.linearVelocity = moveDir * moveSpeed;
                    break;
                case State.Idle:
                case State.Windup:
                case State.Attack:
                case State.Recover:
                    rb.linearVelocity = Vector2.zero;
                    break;
                // Hurt: velocity is driven by Knockback, do not override it here.
                // KO: velocity was zeroed once on death; nothing pushes it (collider disabled).
            }
        }

        /// <summary>
        /// Picks the closest live Health between Team.Player and Team.Ally within
        /// detectRadius (via CombatantRegistry, no Find*) and switches target to it.
        /// A Knocked ally (Health.IsDead) or a downed player (PlayerKO.IsDown, handled
        /// by IsTargetDown) is simply skipped by CombatantRegistry.FindClosest/IsTargetDown.
        /// Leaves the current target untouched if nothing is in range.
        /// </summary>
        private void RetargetNearest()
        {
            retargetTimer = retargetInterval;

            Health closestPlayer = CombatantRegistry.FindClosest(Team.Player, transform.position, detectRadius);
            Health closestAlly = CombatantRegistry.FindClosest(Team.Ally, transform.position, detectRadius);

            Health best;
            if (closestPlayer != null && closestAlly != null)
            {
                float dp = Vector2.Distance(transform.position, closestPlayer.transform.position);
                float da = Vector2.Distance(transform.position, closestAlly.transform.position);
                best = dp <= da ? closestPlayer : closestAlly;
            }
            else
            {
                best = closestPlayer != null ? closestPlayer : closestAlly;
            }

            if (best == null || best.transform == target)
            {
                return;
            }

            target = best.transform;
            targetHealth = best;
            targetKO = best.GetComponent<PlayerKO>(); // null for allies; IsTargetDown falls back to targetHealth.IsDead
        }

        private bool IsTargetDown()
        {
            if (targetKO != null && targetKO.IsDown)
            {
                return true;
            }
            if (targetHealth != null && targetHealth.IsDead)
            {
                return true;
            }
            return false;
        }

        private void TickIdle()
        {
            moveDir = Vector2.zero;
            if (target == null || IsTargetDown())
            {
                return;
            }

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= detectRadius)
            {
                state = State.Chase;
                stuckCheckTimer = stuckCheckInterval;
                lastStuckPosition = transform.position;
                isSliding = false;
            }
        }

        private void TickChase()
        {
            if (target == null || IsTargetDown())
            {
                state = State.Idle;
                moveDir = Vector2.zero;
                isSliding = false;
                return;
            }

            // Only the Player has a PlayerKO component -> targetKO != null means we're
            // chasing the player, which is the only case the attack queue caps.
            bool targetIsPlayer = targetKO != null;
            Vector2 targetPos = target.position;
            Vector2 toTargetRaw = targetPos - rb.position;
            float distRaw = toTargetRaw.magnitude;

            if (distRaw <= attackRange)
            {
                if (!targetIsPlayer || EnemyAttackQueue.TryAcquire(this))
                {
                    Vector2 dir = distRaw > 0.0001f ? toTargetRaw / distRaw : Vector2.down;
                    StartWindup(dir);
                    return;
                }

                // Attack queue full: strafe around the player instead of stacking on top.
                surroundAngle += attackQueueStrafeSpeed * Mathf.Deg2Rad * Time.deltaTime;
                Vector2 strafePoint = targetPos + new Vector2(Mathf.Cos(surroundAngle), Mathf.Sin(surroundAngle)) * attackQueueStrafeRadius;
                Vector2 toStrafe = strafePoint - rb.position;
                moveDir = toStrafe.sqrMagnitude > 0.0001f ? toStrafe.normalized : Vector2.zero;
                ApplySeparation();
                return;
            }

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                moveDir = slideDir;
                if (slideTimer <= 0f)
                {
                    isSliding = false;
                }
                ApplySeparation();
                return;
            }

            // Aim at a point around the target (not dead center) so multiple attackers spread out.
            Vector2 aimPoint = targetPos + new Vector2(Mathf.Cos(surroundAngle), Mathf.Sin(surroundAngle)) * (attackRange * surroundRadiusFactor);
            Vector2 toAim = aimPoint - rb.position;
            moveDir = toAim.sqrMagnitude > 0.0001f ? toAim.normalized : Vector2.zero;

            stuckCheckTimer -= Time.deltaTime;
            if (stuckCheckTimer <= 0f)
            {
                float movedDist = Vector2.Distance(rb.position, lastStuckPosition);
                if (movedDist < stuckMinDistance)
                {
                    Vector2 perp = new Vector2(-moveDir.y, moveDir.x);
                    slideDir = perp.sqrMagnitude > 0.0001f ? perp.normalized : Vector2.right;
                    isSliding = true;
                    slideTimer = slideTime;
                    moveDir = slideDir;
                }
                lastStuckPosition = rb.position;
                stuckCheckTimer = stuckCheckInterval;
            }

            ApplySeparation();
        }

        /// <summary>Adds a soft push away from same-team enemies closer than separationRadius, so a crowd doesn't stack on the exact same point.</summary>
        private void ApplySeparation()
        {
            IReadOnlyList<Health> list = CombatantRegistry.Enemies;
            Vector2 push = Vector2.zero;
            for (int i = 0; i < list.Count; i++)
            {
                Health other = list[i];
                if (other == null || other == health || other.IsDead)
                {
                    continue;
                }
                Vector2 diff = rb.position - (Vector2)other.transform.position;
                float d = diff.magnitude;
                if (d > 0.0001f && d < separationRadius)
                {
                    push += diff.normalized * ((separationRadius - d) / separationRadius);
                }
            }
            moveDir += push * separationStrength;
            if (moveDir.sqrMagnitude > 1f)
            {
                moveDir = moveDir.normalized;
            }
        }

        private void StartWindup(Vector2 dir)
        {
            attackDir = dir;
            state = State.Windup;
            stateTimer = windupTime;
            moveDir = Vector2.zero;
            isSliding = false;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = windupColor;
            }
            if (attackFX != null)
            {
                attackFX.PlayWindupSquash(windupTime);
            }
        }

        private void TickWindup()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                StartAttack();
            }
        }

        private void StartAttack()
        {
            state = State.Attack;
            stateTimer = attackActiveTime;

            if (hitbox != null)
            {
                hitbox.transform.localPosition = attackDir * hitboxDistance;
                hitbox.Open();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = true;
            }
            if (attackFX != null)
            {
                attackFX.PlaySwing(attackDir, attackActiveTime);
            }
        }

        private void TickAttack()
        {
            hitbox?.CheckHits(damage, knockbackForce);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                EndAttack();
            }
        }

        private void EndAttack()
        {
            EnemyAttackQueue.Release(this);
            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }
            RestoreVisual();

            state = State.Recover;
            stateTimer = recoverTime;
        }

        private void TickRecover()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                state = State.Chase;
                stuckCheckTimer = stuckCheckInterval;
                lastStuckPosition = transform.position;
            }
        }

        private void TickHurt()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                state = State.Chase;
                stuckCheckTimer = stuckCheckInterval;
                lastStuckPosition = transform.position;
            }
        }

        private void TickKO()
        {
            if (!respawnForTesting)
            {
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
        }

        /// <summary>
        /// Cancels any windup/attack in progress and holds the enemy still (Hurt state)
        /// for the given duration. Used by PlayerBlock's parry counter-stagger.
        /// </summary>
        public void Stagger(float seconds)
        {
            if (state == State.KO)
            {
                return;
            }

            EnemyAttackQueue.Release(this);
            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }
            RestoreVisual();

            isSliding = false;
            moveDir = Vector2.zero;
            state = State.Hurt;
            stateTimer = Mathf.Max(seconds, hurtStunTime);
        }

        private void RestoreVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            transform.localScale = originalScale;
        }

        private void OnDamaged(DamageInfo info)
        {
            EnemyAttackQueue.Release(this);
            if (state == State.KO)
            {
                return;
            }

            // Cancels any windup/attack in progress: close the hitbox and restore visuals.
            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }
            RestoreVisual();

            isSliding = false;
            moveDir = Vector2.zero;
            state = State.Hurt;
            stateTimer = hurtStunTime;
        }

        private void OnDied()
        {
            EnemyAttackQueue.Release(this);
            state = State.KO;

            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            if (solidCollider != null)
            {
                solidCollider.enabled = false; // anti-soft-lock: never blocks the player's path while down
            }
            if (hurtboxCollider != null)
            {
                hurtboxCollider.enabled = false;
            }

            transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, 90f);
            transform.localScale = originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = koColor;
            }

            isSliding = false;
            moveDir = Vector2.zero;

            if (respawnForTesting)
            {
                respawnTimer = respawnDelay;
            }
        }

        private void Respawn()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            if (solidCollider != null)
            {
                solidCollider.enabled = true;
            }
            if (hurtboxCollider != null)
            {
                hurtboxCollider.enabled = true;
            }
            if (rb != null)
            {
                rb.position = originalPosition;
                rb.linearVelocity = Vector2.zero;
            }

            health.ResetHealth();
            state = State.Idle;
        }
    }

    /// <summary>
    /// Caps how many enemies can be actively winding up/attacking the PLAYER at once
    /// (max 3) so a crowd doesn't stack every hit on the player simultaneously. Allies
    /// have no such limit (see AllyController). Same "no persistent object, just a
    /// plain static collection self-maintained by the owning components" pattern as
    /// CombatantRegistry — cleared entry-by-entry as each EnemyController releases,
    /// dies, or is destroyed, so nothing leaks across a scene reload.
    /// </summary>
    internal static class EnemyAttackQueue
    {
        private const int MaxConcurrent = 3;
        private static readonly HashSet<EnemyController> active = new HashSet<EnemyController>();

        public static bool TryAcquire(EnemyController enemy)
        {
            if (active.Contains(enemy))
            {
                return true;
            }
            if (active.Count >= MaxConcurrent)
            {
                return false;
            }
            active.Add(enemy);
            return true;
        }

        public static void Release(EnemyController enemy)
        {
            active.Remove(enemy);
        }
    }
}
