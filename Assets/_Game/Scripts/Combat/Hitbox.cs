using System;
using System.Collections.Generic;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Attack damage-dealing area. Detection uses Physics2D.OverlapBox with the
    /// transform's own position/size (useTriggers=true), independent of any
    /// Collider2D component on this object — so it never interferes with other
    /// physics casts (e.g. the player's dash Rigidbody2D.Cast with useTriggers=false).
    /// Call Open() at the start of the active window and CheckHits() every frame
    /// while active; a target's Health is only ever hit once per window.
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(0.9f, 0.6f);
        [SerializeField] private GameObject owner;
        [SerializeField] private Team ownerTeam;

        private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];
        private ContactFilter2D overlapFilter;

        private readonly HashSet<Health> hitThisWindow = new HashSet<Health>();
        private bool isActive;

        public bool IsActive => isActive;
        public GameObject Owner => owner;

        /// <summary>Fired for every target actually hit in the current window.</summary>
        public event Action<Health, DamageInfo> OnHit;

        private void Awake()
        {
            overlapFilter = ContactFilter2D.noFilter;
            overlapFilter.useTriggers = true;
        }

        public void SetOwner(GameObject newOwner)
        {
            owner = newOwner;
        }

        public void SetOwnerTeam(Team newTeam)
        {
            ownerTeam = newTeam;
        }

        public void Open()
        {
            hitThisWindow.Clear();
            isActive = true;
        }

        public void Close()
        {
            isActive = false;
        }

        public void CheckHits(float damage, float knockbackForce)
        {
            if (!isActive)
            {
                return;
            }

            int count = Physics2D.OverlapBox(transform.position, size, transform.eulerAngles.z, overlapFilter, OverlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = OverlapBuffer[i];
                if (col == null)
                {
                    continue;
                }

                if (owner != null && col.transform.IsChildOf(owner.transform))
                {
                    continue; // never hit the attacker's own colliders
                }

                Hurtbox hurtbox = col.GetComponent<Hurtbox>();
                if (hurtbox == null || hurtbox.Health == null)
                {
                    continue; // ignore colliders without a Hurtbox
                }

                if (hurtbox.Team == ownerTeam)
                {
                    continue; // never hit same team
                }

                Health targetHealth = hurtbox.Health;
                if (hitThisWindow.Contains(targetHealth))
                {
                    continue; // already hit this window
                }

                Vector2 dir = (Vector2)(targetHealth.transform.position - transform.position);
                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = Vector2.right;
                }
                dir.Normalize();

                var info = new DamageInfo(damage, dir, knockbackForce, owner);
                bool applied = targetHealth.TakeDamage(info);
                if (applied)
                {
                    hitThisWindow.Add(targetHealth);
                    OnHit?.Invoke(targetHealth, info);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
#endif
    }
}
