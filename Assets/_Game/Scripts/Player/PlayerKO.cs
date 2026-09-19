using System.Collections;
using UnityEngine;
using ButecoDosDevs.Combat;

namespace ButecoDosDevs.Player
{
    /// <summary>
    /// Player KO/respawn: on Health.Died, disables movement/attack, lays the sprite
    /// down and darkens it, then after koDuration respawns at checkpoint with full HP
    /// and control restored. No instant death, no soft-lock — the coroutine always ends.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerKO : MonoBehaviour
    {
        [SerializeField] private Transform checkpoint;
        [SerializeField] private float koDuration = 2f;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Color koColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        private Health health;
        private PlayerMovement movement;
        private PlayerAttack attack;
        private Rigidbody2D body;
        private Color originalColor;
        private Quaternion originalRotation;
        private Coroutine koRoutine;

        public bool IsDown { get; private set; }

        /// <summary>Fires once per KO, right when it happens (before the down/respawn coroutine
        /// runs). Used by RuaFlow/BarRivalFlow to count lives toward a defeat screen.</summary>
        public System.Action Knocked;

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
            attack = GetComponent<PlayerAttack>();
            body = GetComponent<Rigidbody2D>();
            if (sprite == null)
            {
                sprite = GetComponentInChildren<SpriteRenderer>();
            }
            if (sprite != null)
            {
                originalColor = sprite.color;
            }
            originalRotation = transform.rotation;
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
        }

        private void OnDied()
        {
            if (koRoutine != null)
            {
                StopCoroutine(koRoutine);
            }
            // KORoutine sets IsDown = true synchronously (before its first yield), so
            // starting it first means listeners of Knocked observe IsDown already true.
            koRoutine = StartCoroutine(KORoutine());
            Knocked?.Invoke();
        }

        private IEnumerator KORoutine()
        {
            IsDown = true;

            if (movement != null)
            {
                movement.enabled = false;
            }
            if (attack != null)
            {
                attack.enabled = false;
            }

            transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, 90f);
            if (sprite != null)
            {
                sprite.color = koColor;
            }

            float t = 0f;
            while (t < koDuration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (checkpoint != null)
            {
                // Teleport through the physics body too, otherwise interpolation drags it back.
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                    body.position = checkpoint.position;
                }
                transform.position = checkpoint.position;
            }
            transform.rotation = originalRotation;
            if (sprite != null)
            {
                sprite.color = originalColor;
            }

            health.ResetHealth();

            if (movement != null)
            {
                movement.enabled = true;
            }
            if (attack != null)
            {
                attack.enabled = true;
            }

            IsDown = false;
            koRoutine = null;
        }
    }
}
