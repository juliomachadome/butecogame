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

        [Header("Detection / Movement")]
        [SerializeField] private float detectRadius = 7f;
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
        [SerializeField] private float windupScale = 1.15f;

        [Header("Hurt / KO")]
        [SerializeField] private float hurtStunTime = 0.25f;
        [SerializeField] private Color koColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        [SerializeField] private bool respawnForTesting = true;
        [SerializeField] private float respawnDelay = 5f;

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

            if (hitbox != null)
            {
                hitbox.SetOwner(gameObject);
                hitbox.SetOwnerTeam(Team.Enemy);
                hitbox.Close();
            }
            if (hitboxSprite != null)
            {
                hitboxSprite.enabled = false;
            }

            lastStuckPosition = transform.position;
            stuckCheckTimer = stuckCheckInterval;
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

            Vector2 toTarget = (Vector2)target.position - rb.position;
            float dist = toTarget.magnitude;

            if (dist <= attackRange)
            {
                Vector2 dir = dist > 0.0001f ? toTarget / dist : Vector2.down;
                StartWindup(dir);
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
                return;
            }

            moveDir = dist > 0.0001f ? toTarget / dist : Vector2.zero;

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
            transform.localScale = new Vector3(originalScale.x * windupScale, originalScale.y * windupScale, originalScale.z);
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
}
