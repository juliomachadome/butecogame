using UnityEngine;
using UnityEngine.Events;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Data + hooks for a talkable NPC: display name, dialogue lines (walked in
    /// sequence, restarting from the first line on every new conversation), the
    /// portrait sprite for DialogueUI, and the world-space name/prompt tag.
    /// Detected by PlayerInteractor via a trigger Collider2D on this GameObject
    /// (Physics2D.OverlapCircle from the player) — never GameObject.Find.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private string displayName = "NPC";
        [SerializeField] private string[] dialogueLines;
        [SerializeField] private Sprite portrait;
        [SerializeField] private NPCController npcController;
        [SerializeField] private NPCNameTag nameTag;

        [Tooltip("Fired every time a conversation with this NPC ends (naturally, Escape, or anti-soft-lock close). Wire simple one-off reactions here (e.g. Pedro updates ObjectiveUI) instead of a generic quest system.")]
        [SerializeField] private UnityEvent onConversationEnded;

        public string DisplayName => displayName;
        public string[] DialogueLines => dialogueLines;
        public Sprite Portrait => portrait;
        public NPCController Controller => npcController;

        public void RaiseConversationEnded()
        {
            onConversationEnded?.Invoke();
        }

        public void ShowTag()
        {
            // Plain != null (not ?.): Unity's overloaded equality correctly catches a
            // destroyed-but-not-yet-nulled nameTag, where ?. would call into it and throw.
            if (nameTag != null)
            {
                nameTag.Show(displayName);
            }
        }

        public void HideTag()
        {
            if (nameTag != null)
            {
                nameTag.Hide();
            }
        }
    }
}
