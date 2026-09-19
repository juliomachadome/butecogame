using UnityEngine;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Small state machine (enum + switch) for non-fighting Buteco NPCs. Only
    /// Idle/Activity/Talk are implemented this phase; the rest of the CLAUDE.md
    /// convention (Wander, Prepare, Follow, Combat, Flee, Timeout, Knocked) is
    /// listed so a later phase can extend the same enum without renaming states.
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Activity,
            Talk,
            Wander,
            Prepare,
            Follow,
            Combat,
            Flee,
            Timeout,
            Knocked
        }

        [SerializeField] private NPCSprite npcSprite;
        [SerializeField] private Vector2 defaultFacing = Vector2.down;

        private State state = State.Activity;
        private Transform talkTarget;

        public State CurrentState => state;

        private void Awake()
        {
            if (npcSprite == null)
            {
                npcSprite = GetComponentInChildren<NPCSprite>();
            }
        }

        private void Start()
        {
            if (npcSprite != null)
            {
                npcSprite.SetFacing(defaultFacing);
                npcSprite.SetActivityActive(state == State.Activity);
            }
        }

        private void Update()
        {
            switch (state)
            {
                case State.Talk:
                    if (talkTarget != null && npcSprite != null)
                    {
                        Vector2 dir = (Vector2)(talkTarget.position - transform.position);
                        npcSprite.SetFacing(dir);
                    }
                    break;

                case State.Idle:
                case State.Activity:
                    // Static placeholder NPCs: facing stays fixed, breathing/activity
                    // bob is handled procedurally inside NPCSprite.
                    break;

                default:
                    // Reserved for a later phase (4c+): Wander/Prepare/Follow/Combat/Flee/Timeout/Knocked.
                    break;
            }
        }

        /// <summary>
        /// Called by PlayerInteractor when a conversation starts: the NPC turns to
        /// face the player and its background activity bob pauses.
        /// </summary>
        public void BeginTalk(Transform player)
        {
            talkTarget = player;
            state = State.Talk;
            if (npcSprite != null)
            {
                npcSprite.SetActivityActive(false);
            }
        }

        /// <summary>
        /// Called when the conversation ends (naturally, Escape, out-of-range or the
        /// NPC/player getting destroyed) — always returns to the background activity loop.
        /// </summary>
        public void EndTalk()
        {
            talkTarget = null;
            state = State.Activity;
            if (npcSprite != null)
            {
                npcSprite.SetFacing(defaultFacing);
                npcSprite.SetActivityActive(true);
            }
        }
    }
}
