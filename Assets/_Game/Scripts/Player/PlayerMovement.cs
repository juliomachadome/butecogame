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

        private readonly RaycastHit2D[] dashCastResults = new RaycastHit2D[4];
        private ContactFilter2D dashCastFilter;

        public Vector2 LastMoveDirection => lastMoveDirection;
        public bool IsDashing => isDashing;

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
            if (isDashing || dashCooldownTimer > 0f)
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

        private void FixedUpdate()
        {
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
    }
}
