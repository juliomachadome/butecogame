using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Small marker component: registers/unregisters the sibling Health with
    /// CombatantRegistry under the given Team for as long as this object is enabled.
    /// Add to Player, Ally and Enemy prefabs (not Neutral targets like training dummies).
    /// In its own file (not CombatantRegistry.cs) so Unity generates a MonoScript asset
    /// for it — a MonoBehaviour that only exists as a second class in another file has no
    /// importable MonoScript and shows up as "missing script" on any GameObject it's added to.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Combatant : MonoBehaviour
    {
        [SerializeField] private Team team;

        private Health health;

        public Team Team => team;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            CombatantRegistry.Register(team, health);
        }

        private void OnDisable()
        {
            CombatantRegistry.Unregister(team, health);
        }
    }
}
