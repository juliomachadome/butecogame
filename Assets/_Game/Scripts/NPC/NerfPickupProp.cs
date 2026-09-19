using UnityEngine;
using ButecoDosDevs.Player;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// The Nerf-war pickup: talking to it ([E] near the chaos) equips the player's toy
    /// Nerf gun (PlayerNerf) via a code-side listener on Interactable's existing
    /// OnConversationEndedEvent — same pattern ButecoFlow uses for its counters/weapon
    /// pickups, so nothing needs wiring through the Inspector's UnityEvent UI. The
    /// player reference is resolved once (by tag) and cached, never via GameObject.Find
    /// inside a loop.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class NerfPickupProp : MonoBehaviour
    {
        private static PlayerNerf cachedNerf;

        private void Awake()
        {
            Interactable interactable = GetComponent<Interactable>();
            interactable.OnConversationEndedEvent.AddListener(OnTalked);
        }

        private void OnTalked()
        {
            if (cachedNerf == null)
            {
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    cachedNerf = playerGo.GetComponent<PlayerNerf>();
                }
            }
            if (cachedNerf != null)
            {
                cachedNerf.Equip();
            }
        }
    }
}
