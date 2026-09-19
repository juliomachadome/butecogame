using UnityEngine;
using UnityEngine.InputSystem;
using ButecoDosDevs.NPC;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Player
{
    /// <summary>
    /// Finds the nearest Interactable within range every frame via
    /// Physics2D.OverlapCircle (NonAlloc, useTriggers=true) — never GameObject.Find.
    /// Shows/hides that NPC's name+prompt tag and opens DialogueUI on E (or gamepad
    /// buttonSouth), input built in code like PlayerMovement/PlayerAttack. While a
    /// dialogue is open, disables PlayerMovement/PlayerAttack and re-enables them
    /// (and ends the NPC's Talk state) exactly once, whenever DialogueUI reports closed.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 1.6f;
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerAttack attack;

        private InputAction interactAction;
        private Interactable nearest;
        private NPCController talkingController;

        private readonly Collider2D[] overlapResults = new Collider2D[8];
        private ContactFilter2D filter;

        public Interactable Nearest => nearest;

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (attack == null) attack = GetComponent<PlayerAttack>();

            filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = false;

            interactAction = new InputAction(name: "Interact", type: InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonSouth");
            interactAction.performed += OnInteractPerformed;
        }

        private void OnEnable()
        {
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
        }

        private void OnDestroy()
        {
            if (interactAction != null)
            {
                interactAction.performed -= OnInteractPerformed;
            }
            interactAction?.Dispose();
        }

        private void Update()
        {
            UpdateNearest();
        }

        private void UpdateNearest()
        {
            // While a dialogue is open we keep tracking who's nearest for when it closes,
            // but we don't need to keep re-showing/hiding tags mid-conversation.
            int count = Physics2D.OverlapCircle(transform.position, interactRadius, filter, overlapResults);

            Interactable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Interactable candidate = overlapResults[i].GetComponent<Interactable>();
                if (candidate == null)
                {
                    continue;
                }
                float dist = Vector2.Distance(transform.position, candidate.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }

            if (best != nearest)
            {
                // Plain != / == (not the null-conditional ?.) so a destroyed-but-not-yet-
                // nulled Interactable (Unity's "fake null") is caught correctly; ?. would
                // call into it anyway and throw, aborting this Update before nearest updates.
                if (nearest != null)
                {
                    nearest.HideTag();
                }
                nearest = best;
                if ((dialogueUI == null || !dialogueUI.IsOpen) && nearest != null)
                {
                    nearest.ShowTag();
                }
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            TryInteract();
        }

        /// <summary>
        /// Attempts to start a conversation with the nearest Interactable. Public so
        /// QA/tests can trigger it without simulating raw input. Returns false if no
        /// target is in range or a dialogue is already open.
        /// </summary>
        public bool TryInteract()
        {
            if (dialogueUI == null || dialogueUI.IsOpen || nearest == null)
            {
                return false;
            }

            StartInteraction(nearest);
            return true;
        }

        private void StartInteraction(Interactable target)
        {
            talkingController = target.Controller;

            if (movement != null) movement.enabled = false;
            if (attack != null) attack.enabled = false;

            if (talkingController != null)
            {
                talkingController.BeginTalk(transform);
            }
            target.HideTag();
            dialogueUI.Open(target, transform, OnDialogueClosed);
        }

        private void OnDialogueClosed()
        {
            if (movement != null) movement.enabled = true;
            if (attack != null) attack.enabled = true;

            if (talkingController != null)
            {
                talkingController.EndTalk();
            }
            talkingController = null;

            // Re-evaluate immediately so the tag reappears if we're still in range.
            if (nearest != null)
            {
                nearest.ShowTag();
            }
        }
    }
}
