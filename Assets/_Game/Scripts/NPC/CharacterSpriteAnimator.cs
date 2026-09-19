using UnityEngine;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Generic 4-direction sprite animator for characters that walk (allies today, any
    /// future NPC that moves tomorrow). Same approach as PlayerSpriteAnimator (direct
    /// SpriteRenderer.sprite swaps from arrays, no Animator Controller): idle S/E/N (1
    /// frame each) + walk S/E/N (N frames each); West reuses the East sprites with
    /// flipX so only 3 directions of walk art are needed.
    ///
    /// Direction/movement come from the Rigidbody2D's velocity by default. While
    /// attacking (or any other moment where velocity isn't a reliable facing — e.g.
    /// windup while stationary) call SetFacingOverride(dir) so the sprite still faces
    /// the target; ClearFacingOverride() returns to velocity-driven facing.
    /// No GetComponent in Update — everything is wired once in Awake/Inspector.
    /// </summary>
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        private enum FacingDirection
        {
            South,
            North,
            East,
            West
        }

        [Header("Refs")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;

        [Header("Idle (1 frame each: S/W/E/N)")]
        [SerializeField] private Sprite idleS;
        [SerializeField] private Sprite idleW;
        [SerializeField] private Sprite idleE;
        [SerializeField] private Sprite idleN;

        [Header("Walk")]
        [SerializeField] private Sprite[] walkS;
        [SerializeField] private Sprite[] walkE;
        [SerializeField] private Sprite[] walkN;

        [Header("Playback")]
        [SerializeField] private float fps = 8f;
        [SerializeField] private float moveThreshold = 0.05f;

        private float frameTimer;
        private int frameIndex;
        private FacingDirection lastDirection = FacingDirection.South;
        private bool wasMoving;

        private bool hasOverride;
        private Vector2 overrideFacing = Vector2.down;

        /// <summary>Current quantized facing as a unit-ish vector (for HeldItem, etc.).</summary>
        public Vector2 CurrentFacing => FacingToVector(lastDirection);

        /// <summary>Forces the facing direction (e.g. toward an attack target) regardless of velocity.</summary>
        public void SetFacingOverride(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }
            hasOverride = true;
            overrideFacing = direction;
        }

        public void ClearFacingOverride()
        {
            hasOverride = false;
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
            bool isMoving = !hasOverride && velocity.sqrMagnitude > moveThreshold * moveThreshold;

            Vector2 facingSource = hasOverride ? overrideFacing : (isMoving ? velocity : (Vector2)FacingToVector(lastDirection));
            FacingDirection direction = QuantizeDirection(facingSource, lastDirection);

            if (direction != lastDirection || isMoving != wasMoving)
            {
                frameTimer = 0f;
                frameIndex = 0;
            }

            lastDirection = direction;
            wasMoving = isMoving;

            spriteRenderer.flipX = direction == FacingDirection.West;

            Sprite[] walkFrames = GetWalkFrames(direction);

            if (isMoving && walkFrames != null && walkFrames.Length > 0)
            {
                frameTimer += Time.deltaTime;
                float secondsPerFrame = fps > 0f ? 1f / fps : 0f;
                if (secondsPerFrame > 0f)
                {
                    while (frameTimer >= secondsPerFrame)
                    {
                        frameTimer -= secondsPerFrame;
                        frameIndex = (frameIndex + 1) % walkFrames.Length;
                    }
                }

                spriteRenderer.sprite = walkFrames[frameIndex % walkFrames.Length];
            }
            else
            {
                Sprite idleSprite = GetIdleSprite(direction);
                if (idleSprite != null)
                {
                    spriteRenderer.sprite = idleSprite;
                }
            }
        }

        private static Vector2 FacingToVector(FacingDirection direction)
        {
            switch (direction)
            {
                case FacingDirection.North: return Vector2.up;
                case FacingDirection.East: return Vector2.right;
                case FacingDirection.West: return Vector2.left;
                default: return Vector2.down;
            }
        }

        private static FacingDirection QuantizeDirection(Vector2 direction, FacingDirection fallback)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return fallback;
            }

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0f ? FacingDirection.East : FacingDirection.West;
            }

            return direction.y >= 0f ? FacingDirection.North : FacingDirection.South;
        }

        private Sprite[] GetWalkFrames(FacingDirection direction)
        {
            switch (direction)
            {
                case FacingDirection.North:
                    return walkN;
                case FacingDirection.East:
                case FacingDirection.West:
                    return walkE;
                default:
                    return walkS;
            }
        }

        private Sprite GetIdleSprite(FacingDirection direction)
        {
            switch (direction)
            {
                case FacingDirection.North:
                    return idleN;
                case FacingDirection.West:
                    return idleW;
                case FacingDirection.East:
                    return idleE;
                default:
                    return idleS;
            }
        }
    }
}
