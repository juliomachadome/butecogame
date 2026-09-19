using UnityEngine;

namespace ButecoDosDevs.Player
{
    /// <summary>
    /// Drives the player's SpriteRenderer directly from arrays of Sprites (no Animator
    /// Controller — easier to debug). Picks idle/walk frames for the dominant facing
    /// direction (S/N/E/W) quantized from PlayerMovement.LastMoveDirection. West reuses
    /// the East sprites with flipX so only 3 directions of art are needed.
    /// </summary>
    public class PlayerSpriteAnimator : MonoBehaviour
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
        [SerializeField] private PlayerMovement movement;

        [Header("Idle (1 frame each)")]
        [SerializeField] private Sprite[] idleS;
        [SerializeField] private Sprite[] idleE;
        [SerializeField] private Sprite[] idleN;

        [Header("Walk")]
        [SerializeField] private Sprite[] walkS;
        [SerializeField] private Sprite[] walkE;
        [SerializeField] private Sprite[] walkN;

        [Header("Playback")]
        [SerializeField] private float fps = 8f;

        private float frameTimer;
        private int frameIndex;
        private FacingDirection lastDirection = FacingDirection.South;
        private bool wasMoving;

        /// <summary>Current quantized facing as a unit-ish vector (for HeldItem, etc.).</summary>
        public Vector2 CurrentFacing => FacingToVector(lastDirection);

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

        private void Update()
        {
            if (movement == null || spriteRenderer == null)
            {
                return;
            }

            FacingDirection direction = QuantizeDirection(movement.LastMoveDirection);
            bool isMoving = movement.IsMoving || movement.IsDashing;

            if (direction != lastDirection || isMoving != wasMoving)
            {
                frameTimer = 0f;
                frameIndex = 0;
            }

            lastDirection = direction;
            wasMoving = isMoving;

            spriteRenderer.flipX = direction == FacingDirection.West;

            Sprite[] walkFrames = GetWalkFrames(direction);
            Sprite[] idleFrames = GetIdleFrames(direction);

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
            else if (idleFrames != null && idleFrames.Length > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private static FacingDirection QuantizeDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return FacingDirection.South;
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

        private Sprite[] GetIdleFrames(FacingDirection direction)
        {
            switch (direction)
            {
                case FacingDirection.North:
                    return idleN;
                case FacingDirection.East:
                case FacingDirection.West:
                    return idleE;
                default:
                    return idleS;
            }
        }
    }
}
