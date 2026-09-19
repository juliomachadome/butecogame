using UnityEngine;
using ButecoDosDevs.Player;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Listens to the sibling Health's Damaged event and applies knockback impulse.
    /// If a PlayerMovement is present, delegates to its wall-safe ApplyKnockback
    /// (player moves via MovePosition, so it needs an explicit "being pushed" state).
    /// Otherwise applies velocity directly to a Dynamic Rigidbody2D (e.g. TrainingDummy),
    /// which relies on its own solid collider + linearDamping to stop at walls/decelerate.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Knockback : MonoBehaviour
    {
        [SerializeField] private float duration = 0.15f;

        private Health health;
        private PlayerMovement playerMovement;
        private Rigidbody2D rb;

        private void Awake()
        {
            health = GetComponent<Health>();
            playerMovement = GetComponent<PlayerMovement>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        private void OnDamaged(DamageInfo info)
        {
            Vector2 velocity = info.knockbackDir * info.knockbackForce;

            if (playerMovement != null)
            {
                playerMovement.ApplyKnockback(velocity, duration);
            }
            else if (rb != null)
            {
                rb.linearVelocity = velocity;
            }
        }
    }
}
