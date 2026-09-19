using System.Collections.Generic;
using UnityEngine;
using ButecoDosDevs.Combat;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Admin Rival boss (Fase 9). State machine (enum + switch): Idle/Chase like
    /// EnemyController, then two telegraphed attacks picked at random once in range —
    /// "Teclado no chão" (circle AoE) and "Investida" (straight-line dash) — plus a
    /// one-time reinforcement call at 50% HP. No pathfinding: chases in a straight line
    /// via Rigidbody2D, same wall-tolerant approach as EnemyController/AllyController.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        private enum State { Idle, Chase, SlamTelegraph, SlamHit, ChargeTelegraph, Charging, Recover, Hurt, KO }

        [Header("Refs")]
        [SerializeField] private Transform target;
        [SerializeField] private Health health;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D solidCollider;
        [SerializeField] private Collider2D hurtboxCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Hitbox chargeHitbox;
        [SerializeField] private AttackFX attackFX;

        [Header("Telegraph visuals (placeholder: tinted square/line, no art dependency)")]
        [SerializeField] private Sprite telegraphSprite; // PH_Square works fine
        [SerializeField] private Color slamTelegraphColor = new Color(1f, 0.1f, 0.1f, 0.5f);
        [SerializeField] private Color chargeTelegraphColor = new Color(1f, 0.6f, 0.1f, 0.6f);

        [Header("Movement")]
        [SerializeField] private float detectRadius = 9f;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float attackDecisionRange = 3f;

        [Header("Slam ('Teclado no chão')")]
        [SerializeField] private float slamTelegraphTime = 1.0f;
        [SerializeField] private float slamDamage = 18f;
        [SerializeField] private float slamRadius = 1.6f;
        [SerializeField] private float slamKnockback = 6f;

        [Header("Charge ('Investida')")]
        [SerializeField] private float chargeTelegraphTime = 0.7f;
        [SerializeField] private float chargeSpeed = 9f;
        [SerializeField] private float chargeDuration = 0.6f;
        [SerializeField] private float chargeDamage = 14f;
        [SerializeField] private float chargeKnockback = 7f;

        [Header("Recover / Hurt")]
        [SerializeField] private float recoverTime = 1.0f;
        [SerializeField] private float hurtStunTime = 0.2f;
        [SerializeField] private Color hurtColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color koColor = new Color(0.25f, 0.25f, 0.3f, 1f);

        [Header("Reinforcements (once, at 50% HP)")]
        [SerializeField] private GameObject reinforcementPrefab;
        [SerializeField] private Transform[] reinforcementPoints;

        [Header("Walk animation (South-facing strip; idle = first frame)")]
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float walkFps = 8f;

        private State state = State.Idle;
        private float walkFrameTimer;
        private int walkFrameIndex;
        private float stateTimer;
        private bool calledReinforcements;
        private Vector2 chargeDir;
        private Vector3 originalScale;
        private Color originalColor;
        private Transform telegraphVisual;
        private SpriteRenderer telegraphRenderer;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (attackFX == null) attackFX = GetComponent<AttackFX>();

            if (target == null)
            {
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null) target = playerGo.transform;
            }

            originalScale = transform.localScale;
            if (spriteRenderer != null) originalColor = spriteRenderer.color;

            if (chargeHitbox != null)
            {
                chargeHitbox.SetOwner(gameObject);
                chargeHitbox.SetOwnerTeam(Team.Enemy);
                chargeHitbox.Close();
            }

            BuildTelegraphVisual();
        }

        private void BuildTelegraphVisual()
        {
            GameObject go = new GameObject("TelegraphVisual");
            go.transform.SetParent(transform, false);
            telegraphVisual = go.transform;
            telegraphRenderer = go.AddComponent<SpriteRenderer>();
            telegraphRenderer.sprite = telegraphSprite;
            telegraphRenderer.sortingOrder = -1;
            go.SetActive(false);
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

        private void Update()
        {
            switch (state)
            {
                case State.Idle: TickIdle(); break;
                case State.Chase: TickChase(); break;
                case State.SlamTelegraph: TickTimer(State.SlamHit); break;
                case State.SlamHit: TickSlamHit(); break;
                case State.ChargeTelegraph: TickChargeTelegraph(); break;
                case State.Charging: TickCharging(); break;
                case State.Recover: TickTimer(State.Chase); break;
                case State.Hurt: TickTimer(State.Chase); break;
                case State.KO: break;
            }

            TickWalkAnimation();
        }

        private void TickWalkAnimation()
        {
            if (spriteRenderer == null || walkFrames == null || walkFrames.Length == 0 || rb == null)
            {
                return;
            }

            bool isMoving = state != State.KO && rb.linearVelocity.sqrMagnitude > 0.01f;
            if (!isMoving)
            {
                walkFrameTimer = 0f;
                walkFrameIndex = 0;
                return;
            }

            walkFrameTimer += Time.deltaTime;
            float secondsPerFrame = walkFps > 0f ? 1f / walkFps : 0f;
            if (secondsPerFrame <= 0f)
            {
                return;
            }
            while (walkFrameTimer >= secondsPerFrame)
            {
                walkFrameTimer -= secondsPerFrame;
                walkFrameIndex = (walkFrameIndex + 1) % walkFrames.Length;
            }
            if (state == State.Hurt || state == State.SlamTelegraph || state == State.ChargeTelegraph)
            {
                return; // don't override the hurt/telegraph tint frame swap concerns; still fine to animate, but keep simple
            }
            spriteRenderer.sprite = walkFrames[walkFrameIndex];
        }

        private void FixedUpdate()
        {
            if (state == State.Chase && target != null)
            {
                Vector2 toTarget = (Vector2)target.position - rb.position;
                float dist = toTarget.magnitude;
                Vector2 dir = dist > attackDecisionRange && dist > 0.001f ? toTarget / dist : Vector2.zero;
                rb.linearVelocity = dir * moveSpeed;
            }
            else if (state == State.Charging)
            {
                rb.linearVelocity = chargeDir * chargeSpeed;
            }
            else if (state != State.KO)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void TickIdle()
        {
            if (target == null) return;
            if (Vector2.Distance(transform.position, target.position) <= detectRadius)
            {
                state = State.Chase;
            }
        }

        private void TickChase()
        {
            if (target == null) return;
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= attackDecisionRange)
            {
                StartAttack();
            }
        }

        private void StartAttack()
        {
            bool useSlam = Random.value < 0.5f;
            if (useSlam) StartSlamTelegraph();
            else StartChargeTelegraph();
        }

        private void StartSlamTelegraph()
        {
            state = State.SlamTelegraph;
            stateTimer = slamTelegraphTime;
            if (telegraphVisual != null)
            {
                telegraphVisual.gameObject.SetActive(true);
                telegraphVisual.localScale = Vector3.one * (slamRadius * 2f);
                telegraphVisual.localPosition = Vector3.zero;
            }
            if (telegraphRenderer != null) telegraphRenderer.color = slamTelegraphColor;
        }

        private void TickSlamHit()
        {
            // One-shot: apply AoE damage then move to Recover.
            if (telegraphVisual != null) telegraphVisual.gameObject.SetActive(false);
            ApplyAoeDamage(transform.position, slamRadius, slamDamage, slamKnockback);
            if (attackFX != null) attackFX.PlayImpact(transform.position, 1.6f);
            state = State.Recover;
            stateTimer = recoverTime;
        }

        private void ApplyAoeDamage(Vector3 center, float radius, float damage, float knockback)
        {
            ApplyAoeToList(CombatantRegistry.Players, center, radius, damage, knockback);
            ApplyAoeToList(CombatantRegistry.Allies, center, radius, damage, knockback);
        }

        private static void ApplyAoeToList(IReadOnlyList<Health> list, Vector3 center, float radius, float damage, float knockback)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Health h = list[i];
                if (h == null || h.IsDead) continue;
                Vector2 toTarget = (Vector2)h.transform.position - (Vector2)center;
                if (toTarget.magnitude > radius) continue;
                Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.up;
                h.TakeDamage(new DamageInfo(damage, dir, knockback, null));
            }
        }

        private void StartChargeTelegraph()
        {
            state = State.ChargeTelegraph;
            stateTimer = chargeTelegraphTime;
            chargeDir = target != null ? ((Vector2)target.position - (Vector2)transform.position).normalized : Vector2.down;
            if (chargeDir.sqrMagnitude < 0.0001f) chargeDir = Vector2.down;

            if (telegraphVisual != null)
            {
                telegraphVisual.gameObject.SetActive(true);
                telegraphVisual.localScale = new Vector3(0.4f, 4f, 1f);
                float angle = Mathf.Atan2(chargeDir.y, chargeDir.x) * Mathf.Rad2Deg - 90f;
                telegraphVisual.localRotation = Quaternion.Euler(0f, 0f, angle);
                telegraphVisual.localPosition = (Vector3)(chargeDir * 2f);
            }
            if (telegraphRenderer != null) telegraphRenderer.color = chargeTelegraphColor;
        }

        private void TickChargeTelegraph()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                state = State.Charging;
                stateTimer = chargeDuration;
            }
        }

        private void TickCharging()
        {
            if (chargeHitbox != null)
            {
                chargeHitbox.Open();
                chargeHitbox.CheckHits(chargeDamage, chargeKnockback);
            }

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                if (chargeHitbox != null) chargeHitbox.Close();
                if (telegraphVisual != null) telegraphVisual.gameObject.SetActive(false);
                state = State.Recover;
                stateTimer = recoverTime;
            }
        }

        private void TickTimer(State next)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                state = next;
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            if (state == State.KO) return;

            if (!calledReinforcements && health != null && health.CurrentHP <= health.MaxHP * 0.5f)
            {
                calledReinforcements = true;
                CallReinforcements();
            }

            if (state == State.SlamTelegraph || state == State.ChargeTelegraph || state == State.Charging)
            {
                return; // don't interrupt a telegraphed attack once it's committed
            }

            if (spriteRenderer != null) spriteRenderer.color = hurtColor;
            state = State.Hurt;
            stateTimer = hurtStunTime;
            Invoke(nameof(RestoreColor), hurtStunTime);
        }

        private void RestoreColor()
        {
            if (spriteRenderer != null && state != State.KO) spriteRenderer.color = originalColor;
        }

        private void CallReinforcements()
        {
            if (reinforcementPrefab == null) return;
            int count = reinforcementPoints != null && reinforcementPoints.Length > 0 ? reinforcementPoints.Length : 2;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = reinforcementPoints != null && reinforcementPoints.Length > i
                    ? reinforcementPoints[i].position
                    : transform.position + (Vector3)(Random.insideUnitCircle.normalized * 2f);
                Instantiate(reinforcementPrefab, pos, Quaternion.identity);
            }
        }

        private void OnDied()
        {
            state = State.KO;
            if (chargeHitbox != null) chargeHitbox.Close();
            if (telegraphVisual != null) telegraphVisual.gameObject.SetActive(false);
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (solidCollider != null) solidCollider.enabled = false;
            if (hurtboxCollider != null) hurtboxCollider.enabled = false;
            transform.localScale = originalScale;
            if (spriteRenderer != null) spriteRenderer.color = koColor;
        }
    }
}
