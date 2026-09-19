using System.Collections;
using UnityEngine;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Ally that follows the player in a formation slot and joins melee combat against
    /// nearby enemies. State machine (enum + switch): Follow, Combat, Knocked.
    /// No pathfinding: moves straight toward its target point via Rigidbody2D velocity
    /// and lets Box2D resolve walls (same approach as EnemyController), with a periodic
    /// stuck check that slides perpendicular, and a distance/stuck rescue teleport so
    /// an ally can never be soft-locked away from the group.
    /// </summary>
    public class AllyController : MonoBehaviour
    {
        public enum Role { Frontline, Support }

        private enum State { Follow, Combat, Knocked }

        [Header("Refs")]
        [SerializeField] private Transform target; // the player; auto-found by tag if left empty
        [SerializeField] private Health health;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private SpriteRenderer hitboxSprite;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D solidCollider;
        [SerializeField] private Collider2D hurtboxCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CharacterSpriteAnimator spriteAnimator;
        [SerializeField] private AttackFX attackFX;

        [Header("Role")]
        [SerializeField] private Role role = Role.Frontline;

        public Role CurrentRole => role;

        [Header("Support (Role = Support only: Julio/Funnie, retaguarda)")]
        [SerializeField] private float supportBackDistance = 2.5f;
        [SerializeField] private float healInterval = 6f;
        [SerializeField] private float healAmount = 15f;
        [SerializeField] private float healRange = 7f;
        [SerializeField] private float shieldInterval = 9f;
        [SerializeField] private float shieldDuration = 4f;
        [SerializeField] private float shieldDamageMultiplier = 0.5f;
        [SerializeField] private float shieldRange = 7f;
        [SerializeField] private Sprite orbSprite; // reuse FX_SmokePuff or PH_Square, tinted below
        [SerializeField] private float orbTravelTime = 0.4f;
        [SerializeField] private Color healOrbColor = new Color(0.3f, 1f, 0.4f, 1f);
        [SerializeField] private Color shieldOrbColor = new Color(0.3f, 0.6f, 1f, 1f);
        [SerializeField] private float supportFlashDuration = 0.25f;

        private float healTimer;
        private float shieldTimer;

        [Header("Formation (Follow)")]
        [SerializeField] private float formationBackDistance = 1.4f;
        [SerializeField] private float formationSideOffset = 0f; // e.g. -1 = atrás-esquerda, 0 = atrás, 1 = atrás-direita
        [SerializeField] private float followStopDistance = 0.8f;
        [SerializeField] private float followSpeed = 4.5f;

        [Header("Rescue (anti-soft-lock)")]
        [SerializeField] private float rescueDistance = 8f;
        [SerializeField] private float stuckTimeout = 2f;
        [SerializeField] private float stuckMinDistance = 0.15f;
        [SerializeField] private float rescueTestRadius = 0.35f;
        [SerializeField] private float rescueRingDistance = 1.5f;

        [Header("Combat")]
        [SerializeField] private float combatDetectRadius = 6f;
        [SerializeField] private float approachDistance = 1.0f;
        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private float windupTime = 0.3f;
        [SerializeField] private float attackActiveTime = 0.1f;
        [SerializeField] private float recoverTime = 0.5f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float knockbackForce = 4f;
        [SerializeField] private float hitboxDistance = 0.6f;
        [SerializeField] private Color windupColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private float combatStuckCheckInterval = 1f;
        [SerializeField] private float slideTime = 0.4f;

        [Header("Knocked (Sacrifice)")]
        [SerializeField] private float knockedCheckRadius = 8f;
        [SerializeField] private float knockedReviveClearTime = 4f;
        [SerializeField] private float reviveHpFraction = 0.5f;
        [SerializeField] private Color koColor = new Color(0.25f, 0.25f, 0.3f, 1f);

        private static readonly ContactFilter2D SolidFilter = MakeSolidFilter();
        private static readonly Collider2D[] RescueOverlapBuffer = new Collider2D[4];

        private State state = State.Follow;
        private Vector2 moveDir;
        private PlayerMovement playerMovement;

        private Vector3 originalScale;
        private Quaternion originalRotation;
        private Color originalColor;

        // Combat sub-state (windup/attack/recover), nested inside State.Combat.
        private enum CombatPhase { Approach, Windup, Attack, Recover }
        private CombatPhase combatPhase;
        private float combatPhaseTimer;
        private Health combatTargetHealth;
        private Vector2 attackDir;

        // Follow stuck tracking.
        private float stuckCheckTimer;
        private Vector2 lastStuckPosition;

        // Combat stuck tracking (slide).
        private bool isSliding;
        private float slideTimer;
        private Vector2 slideDir;
        private float combatStuckCheckTimer;
        private Vector2 lastCombatStuckPosition;

        // Knocked timer.
        private float knockedClearTimer;

        // Brief "Hurt" window after taking damage: FixedUpdate stops overriding velocity
        // for this long so Knockback's impulse (applied from Health.Damaged, in Update)
        // actually shows instead of being stomped by moveDir on the very next physics step.
        [SerializeField] private float hurtVelocityFreezeTime = 0.15f;
        private bool isHurt;
        private float hurtTimer;

        private static ContactFilter2D MakeSolidFilter()
        {
            ContactFilter2D filter = ContactFilter2D.noFilter;
            filter.useTriggers = false;
            return filter;
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteAnimator == null) spriteAnimator = GetComponent<CharacterSpriteAnimator>();

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
                playerMovement = target.GetComponent<PlayerMovement>();
            }

            originalScale = transform.localScale;
            originalRotation = transform.rotation;
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
                hitbox.SetOwnerTeam(Team.Ally);
                hitbox.Close();
                hitbox.OnHit += OnHitboxHit;
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }

            lastStuckPosition = transform.position;
            stuckCheckTimer = stuckTimeout;
            lastCombatStuckPosition = transform.position;
            combatStuckCheckTimer = combatStuckCheckInterval;
        }

        private void Start()
        {
            // Never let an ally's solid body push/trap the player (or vice versa);
            // walls/props are unaffected since those use different colliders.
            if (solidCollider != null && target != null)
            {
                Collider2D playerCollider = target.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    Physics2D.IgnoreCollision(solidCollider, playerCollider);
                }
            }
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
            if (hitbox != null)
            {
                hitbox.OnHit -= OnHitboxHit;
            }
        }

        private void OnHitboxHit(Health targetHealth, DamageInfo info)
        {
            if (attackFX != null && targetHealth != null)
            {
                attackFX.PlayImpact(targetHealth.transform.position);
            }
        }

        private void Update()
        {
            if (role == Role.Support && state != State.Knocked)
            {
                TickSupportFollow();
                TickSupportAbilities();
                return;
            }

            switch (state)
            {
                case State.Follow:
                    TickFollow();
                    break;
                case State.Combat:
                    TickCombat();
                    break;
                case State.Knocked:
                    TickKnocked();
                    break;
            }
        }

        // ---------------- Support (Role.Support: stays back, never melees) ----------------

        private void TickSupportFollow()
        {
            if (target == null)
            {
                moveDir = Vector2.zero;
                return;
            }

            if (spriteAnimator != null)
            {
                spriteAnimator.ClearFacingOverride();
            }

            Vector2 facing = playerMovement != null ? playerMovement.LastMoveDirection : Vector2.down;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector2.down;
            facing.Normalize();
            Vector2 behind = -facing;
            Vector2 side = new Vector2(-behind.y, behind.x);
            Vector2 formationPoint = (Vector2)target.position + behind * supportBackDistance + side * formationSideOffset;

            Vector2 toFormation = formationPoint - (Vector2)transform.position;
            float dist = toFormation.magnitude;
            moveDir = dist <= followStopDistance ? Vector2.zero : (toFormation / Mathf.Max(dist, 0.0001f)) * followSpeed;

            // Same anti-soft-lock rescue as Frontline's Follow.
            float distanceToPlayer = Vector2.Distance(transform.position, target.position);
            stuckCheckTimer -= Time.deltaTime;
            bool timedOut = false;
            if (stuckCheckTimer <= 0f)
            {
                float moved = Vector2.Distance(transform.position, lastStuckPosition);
                timedOut = moved < stuckMinDistance && dist > followStopDistance;
                lastStuckPosition = transform.position;
                stuckCheckTimer = stuckTimeout;
            }
            if (distanceToPlayer > rescueDistance || timedOut)
            {
                RescueTeleport();
            }
        }

        private void TickSupportAbilities()
        {
            healTimer -= Time.deltaTime;
            shieldTimer -= Time.deltaTime;

            if (healTimer <= 0f)
            {
                healTimer = healInterval;
                Health worst = FindLowestFractionInRange(healRange, requireDamaged: true);
                if (worst != null)
                {
                    StartCoroutine(FireOrb(worst.transform, healOrbColor, () =>
                    {
                        if (worst != null) worst.Heal(healAmount);
                        FlashTarget(worst, healOrbColor);
                    }));
                }
            }

            if (shieldTimer <= 0f)
            {
                shieldTimer = shieldInterval;
                Health frontline = FindLowestFractionInRange(shieldRange, requireDamaged: false);
                if (frontline != null)
                {
                    StartCoroutine(FireOrb(frontline.transform, shieldOrbColor, () =>
                    {
                        if (frontline != null) frontline.ApplyShield(shieldDuration, shieldDamageMultiplier);
                        FlashTarget(frontline, shieldOrbColor);
                    }));
                }
            }
        }

        /// <summary>Lowest-HP-fraction live ally/player within range. requireDamaged skips full-HP targets (heal only).</summary>
        private Health FindLowestFractionInRange(float range, bool requireDamaged)
        {
            Health best = null;
            float bestFraction = float.MaxValue;
            ConsiderList(CombatantRegistry.Players, range, requireDamaged, ref best, ref bestFraction);
            ConsiderList(CombatantRegistry.Allies, range, requireDamaged, ref best, ref bestFraction);
            return best;
        }

        private void ConsiderList(System.Collections.Generic.IReadOnlyList<Health> list, float range, bool requireDamaged, ref Health best, ref float bestFraction)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Health candidate = list[i];
                if (candidate == null || candidate.IsDead || candidate == health)
                {
                    continue;
                }
                float dist = Vector2.Distance(transform.position, candidate.transform.position);
                if (dist > range)
                {
                    continue;
                }
                float fraction = candidate.MaxHP > 0f ? candidate.CurrentHP / candidate.MaxHP : 1f;
                if (requireDamaged && fraction >= 0.999f)
                {
                    continue;
                }
                if (fraction < bestFraction)
                {
                    bestFraction = fraction;
                    best = candidate;
                }
            }
        }

        private IEnumerator FireOrb(Transform destination, Color color, System.Action onArrive)
        {
            GameObject go = new GameObject("SupportOrb");
            go.transform.position = transform.position;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = orbSprite;
            sr.color = color;
            sr.sortingOrder = 55;

            float t = 0f;
            Vector3 start = transform.position;
            while (t < orbTravelTime)
            {
                if (destination == null)
                {
                    Destroy(go);
                    yield break;
                }
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / orbTravelTime);
                go.transform.position = Vector3.Lerp(start, destination.position, t01) + Vector3.up * (Mathf.Sin(t01 * Mathf.PI) * 0.4f);
                yield return null;
            }

            Destroy(go);
            onArrive?.Invoke();
        }

        private void FlashTarget(Health targetHealth, Color color)
        {
            if (targetHealth == null)
            {
                return;
            }
            SpriteRenderer sr = targetHealth.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                StartCoroutine(FlashRoutine(sr, color, supportFlashDuration));
            }
        }

        private static IEnumerator FlashRoutine(SpriteRenderer sr, Color flashColor, float duration)
        {
            Color original = sr.color;
            sr.color = flashColor;
            yield return new WaitForSeconds(duration);
            if (sr != null)
            {
                sr.color = original;
            }
        }

        private void FixedUpdate()
        {
            if (state == State.Knocked)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (isHurt)
            {
                hurtTimer -= Time.fixedDeltaTime;
                if (hurtTimer <= 0f)
                {
                    isHurt = false;
                }
                return; // leave Knockback's velocity alone this step
            }

            if (state == State.Combat && (combatPhase == CombatPhase.Windup || combatPhase == CombatPhase.Attack || combatPhase == CombatPhase.Recover))
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            rb.linearVelocity = moveDir;
        }

        // ---------------- Follow ----------------

        private Vector2 FormationPoint()
        {
            if (target == null)
            {
                return transform.position;
            }

            Vector2 facing = playerMovement != null ? playerMovement.LastMoveDirection : Vector2.down;
            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = Vector2.down;
            }
            facing.Normalize();

            Vector2 behind = -facing;
            Vector2 side = new Vector2(-behind.y, behind.x);

            return (Vector2)target.position + behind * formationBackDistance + side * formationSideOffset;
        }

        private void TickFollow()
        {
            if (target == null)
            {
                moveDir = Vector2.zero;
                return;
            }

            if (spriteAnimator != null)
            {
                spriteAnimator.ClearFacingOverride();
            }

            // Enter combat if a live enemy is close enough to the player.
            Health nearestThreat = CombatantRegistry.FindClosest(Team.Enemy, target.position, combatDetectRadius);
            if (nearestThreat != null)
            {
                EnterCombat();
                return;
            }

            Vector2 formationPoint = FormationPoint();
            Vector2 toFormation = formationPoint - (Vector2)transform.position;
            float dist = toFormation.magnitude;

            if (dist <= followStopDistance)
            {
                moveDir = Vector2.zero;
            }
            else
            {
                moveDir = (toFormation / Mathf.Max(dist, 0.0001f)) * followSpeed;
            }

            // Anti-soft-lock: rescue teleport if too far from the player or stuck in place.
            float distanceToPlayer = Vector2.Distance(transform.position, target.position);
            bool tooFar = distanceToPlayer > rescueDistance;

            stuckCheckTimer -= Time.deltaTime;
            bool timedOut = false;
            if (stuckCheckTimer <= 0f)
            {
                float moved = Vector2.Distance(transform.position, lastStuckPosition);
                timedOut = moved < stuckMinDistance && dist > followStopDistance;
                lastStuckPosition = transform.position;
                stuckCheckTimer = stuckTimeout;
            }

            if (tooFar || timedOut)
            {
                RescueTeleport();
            }
        }

        private void RescueTeleport()
        {
            if (target == null)
            {
                return;
            }

            Vector2 origin = target.position;
            Vector2 boxSize = new Vector2(rescueTestRadius, rescueTestRadius);

            // Try a ring of candidate points around the player; pick the first that's clear
            // of solid geometry. If every candidate fails, fall back to the player's own
            // position (never leaves the ally permanently stuck).
            const int candidateCount = 8;
            Vector2 chosen = origin;
            for (int i = 0; i < candidateCount; i++)
            {
                float angle = (360f / candidateCount) * i * Mathf.Deg2Rad;
                Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rescueRingDistance;
                int hitCount = Physics2D.OverlapBox(candidate, boxSize, 0f, SolidFilter, RescueOverlapBuffer);
                if (hitCount == 0)
                {
                    chosen = candidate;
                    break;
                }
            }

            transform.position = chosen;
            if (rb != null)
            {
                rb.position = chosen;
                rb.linearVelocity = Vector2.zero;
            }

            stuckCheckTimer = stuckTimeout;
            lastStuckPosition = chosen;
        }

        // ---------------- Combat ----------------

        private void EnterCombat()
        {
            state = State.Combat;
            combatPhase = CombatPhase.Approach;
            combatTargetHealth = null;
            isSliding = false;
            lastCombatStuckPosition = transform.position;
            combatStuckCheckTimer = combatStuckCheckInterval;
        }

        private void TickCombat()
        {
            if (target == null)
            {
                state = State.Follow;
                moveDir = Vector2.zero;
                return;
            }

            switch (combatPhase)
            {
                case CombatPhase.Approach:
                    TickCombatApproach();
                    break;
                case CombatPhase.Windup:
                    TickCombatWindup();
                    break;
                case CombatPhase.Attack:
                    TickCombatAttack();
                    break;
                case CombatPhase.Recover:
                    TickCombatRecover();
                    break;
            }
        }

        private Health FindNearestEnemyToSelf()
        {
            // Only consider enemies close enough to the player (the group's fight), but
            // pick the one nearest to THIS ally to attack.
            Health best = null;
            float bestSqr = float.MaxValue;
            var enemies = CombatantRegistry.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Health candidate = enemies[i];
                if (candidate == null || candidate.IsDead)
                {
                    continue;
                }
                float distToPlayer = Vector2.Distance(candidate.transform.position, target.position);
                if (distToPlayer > combatDetectRadius)
                {
                    continue;
                }
                float sqrToSelf = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqrToSelf < bestSqr)
                {
                    bestSqr = sqrToSelf;
                    best = candidate;
                }
            }
            return best;
        }

        private void TickCombatApproach()
        {
            if (combatTargetHealth == null || combatTargetHealth.IsDead)
            {
                combatTargetHealth = FindNearestEnemyToSelf();
            }

            if (combatTargetHealth == null)
            {
                // Nothing left to fight near the player: go back to formation.
                state = State.Follow;
                moveDir = Vector2.zero;
                isSliding = false;
                return;
            }

            Vector2 toTarget = (Vector2)combatTargetHealth.transform.position - (Vector2)transform.position;
            float dist = toTarget.magnitude;

            if (spriteAnimator != null && dist > 0.0001f)
            {
                spriteAnimator.SetFacingOverride(toTarget);
            }

            if (dist <= approachDistance)
            {
                StartWindup(dist > 0.0001f ? toTarget / dist : Vector2.down);
                return;
            }

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                moveDir = slideDir * moveSpeed;
                if (slideTimer <= 0f)
                {
                    isSliding = false;
                }
                return;
            }

            moveDir = (dist > 0.0001f ? toTarget / dist : Vector2.zero) * moveSpeed;

            combatStuckCheckTimer -= Time.deltaTime;
            if (combatStuckCheckTimer <= 0f)
            {
                float moved = Vector2.Distance(transform.position, lastCombatStuckPosition);
                if (moved < stuckMinDistance)
                {
                    Vector2 dir = moveDir.sqrMagnitude > 0.0001f ? moveDir.normalized : Vector2.right;
                    Vector2 perp = new Vector2(-dir.y, dir.x);
                    slideDir = perp;
                    isSliding = true;
                    slideTimer = slideTime;
                    moveDir = slideDir * moveSpeed;
                }
                lastCombatStuckPosition = transform.position;
                combatStuckCheckTimer = combatStuckCheckInterval;
            }
        }

        private void StartWindup(Vector2 dir)
        {
            attackDir = dir;
            combatPhase = CombatPhase.Windup;
            combatPhaseTimer = windupTime;
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

        private void TickCombatWindup()
        {
            combatPhaseTimer -= Time.deltaTime;
            if (combatPhaseTimer <= 0f)
            {
                StartAttack();
            }
        }

        private void StartAttack()
        {
            combatPhase = CombatPhase.Attack;
            combatPhaseTimer = attackActiveTime;

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

        private void TickCombatAttack()
        {
            hitbox?.CheckHits(damage, knockbackForce);

            combatPhaseTimer -= Time.deltaTime;
            if (combatPhaseTimer <= 0f)
            {
                EndAttack();
            }
        }

        private void EndAttack()
        {
            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }
            RestoreVisual();

            combatPhase = CombatPhase.Recover;
            combatPhaseTimer = recoverTime;
        }

        private void TickCombatRecover()
        {
            combatPhaseTimer -= Time.deltaTime;
            if (combatPhaseTimer <= 0f)
            {
                combatPhase = CombatPhase.Approach;
                lastCombatStuckPosition = transform.position;
                combatStuckCheckTimer = combatStuckCheckInterval;
            }
        }

        private void RestoreVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            transform.localScale = originalScale;
        }

        // ---------------- Knocked ----------------

        private void TickKnocked()
        {
            Health threat = CombatantRegistry.FindClosest(Team.Enemy, transform.position, knockedCheckRadius);
            if (threat != null)
            {
                knockedClearTimer = 0f;
                return;
            }

            knockedClearTimer += Time.deltaTime;
            if (knockedClearTimer >= knockedReviveClearTime)
            {
                Revive();
            }
        }

        private void Revive()
        {
            health.ReviveWithFraction(reviveHpFraction);

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

            state = State.Follow;
            moveDir = Vector2.zero;
            stuckCheckTimer = stuckTimeout;
            lastStuckPosition = transform.position;
        }

        // ---------------- Health events ----------------

        private void OnDamaged(DamageInfo info)
        {
            if (state == State.Knocked)
            {
                return;
            }

            isHurt = true;
            hurtTimer = hurtVelocityFreezeTime;

            // Cancel any windup/attack in progress so a hit reads clearly.
            if (state == State.Combat && (combatPhase == CombatPhase.Windup || combatPhase == CombatPhase.Attack))
            {
                if (hitbox != null)
                {
                    hitbox.Close();
                }
                if (hitboxSprite != null)
                {
                    hitboxSprite.enabled = false;
                }
                RestoreVisual();
                combatPhase = CombatPhase.Recover;
                combatPhaseTimer = recoverTime * 0.5f;
            }
        }

        private void OnDied()
        {
            state = State.Knocked;
            knockedClearTimer = 0f;

            if (hitbox != null)
            {
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }
            if (spriteAnimator != null)
            {
                spriteAnimator.ClearFacingOverride();
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            if (solidCollider != null)
            {
                solidCollider.enabled = false; // anti-soft-lock: never blocks the player/allies while down
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

            moveDir = Vector2.zero;
            isSliding = false;
        }
    }
}
