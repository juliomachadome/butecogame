using UnityEngine;
using UnityEngine.InputSystem;

namespace ButecoDosDevs.Player
{
    /// <summary>
    /// Top-down 2D player movement + dash. Input built entirely in code
    /// (no .inputactions asset) using the new Input System.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Dash")]
        [SerializeField] private float dashDistance = 3f;
        [SerializeField] private float dashDuration = 0.12f;
        [SerializeField] private float dashCooldown = 0.8f;

        [Header("Knockback")]
        [SerializeField] private float knockbackSkin = 0.05f;

        private const float DashSkin = 0.05f;

        private Rigidbody2D rb;
        private InputAction moveAction;
        private InputAction dashAction;

        private Vector2 moveInput;
        private Vector2 lastMoveDirection = Vector2.down;

        private bool isDashing;
        private float dashTimer;
        private float dashCooldownTimer;
        private Vector2 dashVelocity;

        private bool isKnockedBack;
        private float knockbackTimer;
        private float knockbackDuration;
        private Vector2 knockbackVelocity;

        private readonly RaycastHit2D[] dashCastResults = new RaycastHit2D[4];
        private ContactFilter2D dashCastFilter;

        public Vector2 LastMoveDirection => lastMoveDirection;
        public bool IsDashing => isDashing;
        public bool IsKnockedBack => isKnockedBack;
        public bool IsMoving => moveInput.sqrMagnitude > 0.0001f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            dashCastFilter = ContactFilter2D.noFilter;
            dashCastFilter.useTriggers = false;

            BuildInputActions();
        }

        private void BuildInputActions()
        {
            moveAction = new InputAction(name: "Move", type: InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            dashAction = new InputAction(name: "Dash", type: InputActionType.Button);
            dashAction.AddBinding("<Keyboard>/space");
            dashAction.AddBinding("<Keyboard>/leftShift");
            dashAction.performed += OnDashPerformed;
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            dashAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            dashAction?.Disable();

            // Drop any in-flight motion so nothing resumes (or drifts) when re-enabled (e.g. after KO).
            moveInput = Vector2.zero;
            isDashing = false;
            isKnockedBack = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            if (dashAction != null)
            {
                dashAction.performed -= OnDashPerformed;
            }
            moveAction?.Dispose();
            dashAction?.Dispose();
        }

        private void Update()
        {
            moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            if (moveInput.sqrMagnitude > 0.0001f)
            {
                lastMoveDirection = moveInput.normalized;
            }

            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;
            }
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            TryDash(moveInput);
        }

        /// <summary>
        /// Attempts to start a dash in the given direction (falls back to
        /// lastMoveDirection when direction is zero). Public for test/reflection use.
        /// </summary>
        public bool TryDash(Vector2 direction)
        {
            if (isDashing || isKnockedBack || dashCooldownTimer > 0f)
            {
                return false;
            }

            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : lastMoveDirection;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector2.down;
            }

            float distance = dashDistance;
            int hitCount = rb.Cast(dir, dashCastFilter, dashCastResults, distance);
            if (hitCount > 0)
            {
                float closest = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    if (dashCastResults[i].distance < closest)
                    {
                        closest = dashCastResults[i].distance;
                    }
                }
                distance = Mathf.Max(0f, closest - DashSkin);
            }

            if (distance <= 0.001f)
            {
                // Can't move at all without hitting the wall immediately; skip dash.
                return false;
            }

            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashVelocity = dir * (distance / Mathf.Max(dashDuration, 0.0001f));

            return true;
        }

        /// <summary>
        /// Starts a knockback push: for ~duration seconds the given velocity (decaying
        /// linearly to zero) replaces input-driven movement, resolved with the same
        /// wall-safe Rigidbody2D.Cast used by dash so it can never cross a wall.
        /// Cancels any in-progress dash.
        /// </summary>
        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            isDashing = false;
            isKnockedBack = true;
            knockbackDuration = Mathf.Max(duration, 0.0001f);
            knockbackTimer = knockbackDuration;
            knockbackVelocity = velocity;
        }

        private void FixedUpdate()
        {
            // Movement is fully position-driven; clear velocity picked up from contacts so the player never drifts.
            rb.linearVelocity = Vector2.zero;

            if (isKnockedBack)
            {
                float t = knockbackTimer / knockbackDuration;
                Vector2 currentVelocity = knockbackVelocity * t;
                MoveWithWallCheck(currentVelocity * Time.fixedDeltaTime);

                knockbackTimer -= Time.fixedDeltaTime;
                if (knockbackTimer <= 0f)
                {
                    isKnockedBack = false;
                }
                return;
            }

            if (isDashing)
            {
                rb.MovePosition(rb.position + dashVelocity * Time.fixedDeltaTime);
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0f)
                {
                    isDashing = false;
                }
                return;
            }

            Vector2 velocity = moveInput * moveSpeed;
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Moves by delta, clamped by a Rigidbody2D.Cast (useTriggers=false) so it
        /// never crosses a solid wall. Used by knockback.
        /// </summary>
        private void MoveWithWallCheck(Vector2 delta)
        {
            float distance = delta.magnitude;
            if (distance < 0.0001f)
            {
                return;
            }

            Vector2 dir = delta / distance;
            int hitCount = rb.Cast(dir, dashCastFilter, dashCastResults, distance);
            if (hitCount > 0)
            {
                float closest = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    if (dashCastResults[i].distance < closest)
                    {
                        closest = dashCastResults[i].distance;
                    }
                }
                distance = Mathf.Max(0f, closest - knockbackSkin);
            }

            rb.MovePosition(rb.position + dir * distance);
        }
    }
}
