using System.Collections.Generic;
using UnityEngine;

namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Static registry of live Health components grouped by Team, so AllyController and
    /// EnemyController can find nearby targets without GameObject.Find/FindObjectsByType
    /// in Update. This is the SECOND accepted exception to "no singletons" in this
    /// project (the first is HitStop) — it only holds plain lists that self-maintain via
    /// OnEnable/OnDisable on the small "Combatant" marker component below, so there is
    /// no persistent GameObject, no DontDestroyOnLoad, and nothing to leak across scenes
    /// (a domain reload / scene load simply empties the static lists again).
    /// </summary>
    public static class CombatantRegistry
    {
        private static readonly List<Health> players = new List<Health>();
        private static readonly List<Health> allies = new List<Health>();
        private static readonly List<Health> enemies = new List<Health>();

        public static IReadOnlyList<Health> Players => players;
        public static IReadOnlyList<Health> Allies => allies;
        public static IReadOnlyList<Health> Enemies => enemies;

        public static void Register(Team team, Health health)
        {
            List<Health> list = ListFor(team);
            if (list != null && !list.Contains(health))
            {
                list.Add(health);
            }
        }

        public static void Unregister(Team team, Health health)
        {
            ListFor(team)?.Remove(health);
        }

        private static List<Health> ListFor(Team team)
        {
            switch (team)
            {
                case Team.Player: return players;
                case Team.Ally: return allies;
                case Team.Enemy: return enemies;
                default: return null; // Neutral targets (training dummies) aren't tracked.
            }
        }

        /// <summary>
        /// Finds the closest alive Health in the given team's list to origin, within
        /// maxDistance. Returns null if none. Skips destroyed/disabled entries defensively
        /// (registration should keep the lists clean via OnDisable, but this is cheap
        /// insurance against the Unity "fake null" pitfall).
        /// </summary>
        public static Health FindClosest(Team team, Vector2 origin, float maxDistance)
        {
            List<Health> list = ListFor(team);
            if (list == null)
            {
                return null;
            }

            Health best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < list.Count; i++)
            {
                Health candidate = list[i];
                if (candidate == null || candidate.IsDead)
                {
                    continue;
                }

                float sqrDist = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
                if (sqrDist <= bestSqr)
                {
                    bestSqr = sqrDist;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
