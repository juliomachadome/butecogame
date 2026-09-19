using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Marks a trigger Collider2D as the receiving area for damage. Lives on a child
    /// GameObject named "Hurtbox" with explicit references to the owner's Health/Team.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Team team;

        public Health Health => health;
        public Team Team => team;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }
}
