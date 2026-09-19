using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ButecoDosDevs.NPC;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Bottom dialogue panel: NPC name, portrait (south sprite) and current line.
    /// Advances with E/J/mouse-left/gamepad-South; Escape closes. Self-monitors the
    /// player's distance to the talking NPC and closes (anti-soft-lock) if the player
    /// leaves range or the NPC is destroyed. The same E press that opened the dialogue
    /// (via PlayerInteractor) can never also advance/skip the first line — guarded by
    /// comparing Time.frameCount against the frame Open() ran on.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private float closeRadius = 1.8f;

        private InputAction advanceAction;
        private InputAction cancelAction;

        private Interactable current;
        private Transform player;
        private string[] lines;
        private int index;
        private System.Action onClosed;
        private int openFrame;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public Interactable CurrentInteractable => current;

        private void Awake()
        {
            advanceAction = new InputAction(name: "DialogueAdvance", type: InputActionType.Button);
            advanceAction.AddBinding("<Keyboard>/e");
            advanceAction.AddBinding("<Keyboard>/j");
            advanceAction.AddBinding("<Mouse>/leftButton");
            advanceAction.AddBinding("<Gamepad>/buttonSouth");
            advanceAction.performed += OnAdvance;

            cancelAction = new InputAction(name: "DialogueCancel", type: InputActionType.Button);
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.performed += OnCancel;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            advanceAction.performed -= OnAdvance;
            cancelAction.performed -= OnCancel;
            advanceAction?.Dispose();
            cancelAction?.Dispose();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            // Anti-soft-lock: NPC destroyed -> Unity's overloaded null check catches it.
            if (current == null)
            {
                Close();
                return;
            }

            if (player != null)
            {
                float dist = Vector2.Distance(player.position, current.transform.position);
                if (dist > closeRadius)
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Opens the panel for the given NPC. onClosedCallback fires exactly once,
        /// whenever the dialogue ends (natural end, Escape, or anti-soft-lock close).
        /// </summary>
        public void Open(Interactable target, Transform playerTransform, System.Action onClosedCallback)
        {
            if (target == null)
            {
                return;
            }

            current = target;
            player = playerTransform;
            onClosed = onClosedCallback;
            lines = target.DialogueLines;
            index = 0;

            if (nameText != null)
            {
                nameText.text = target.DisplayName;
            }
            if (bodyText != null)
            {
                bodyText.text = (lines != null && lines.Length > 0) ? lines[0] : string.Empty;
            }
            if (portraitImage != null)
            {
                portraitImage.sprite = target.Portrait;
                portraitImage.enabled = target.Portrait != null;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            openFrame = Time.frameCount;
            advanceAction.Enable();
            cancelAction.Enable();
        }

        private void OnAdvance(InputAction.CallbackContext ctx)
        {
            if (!IsOpen)
            {
                return;
            }

            // The same key press that opened the dialogue must not also advance it.
            if (Time.frameCount == openFrame)
            {
                return;
            }

            index++;
            if (lines == null || index >= lines.Length)
            {
                Close();
                return;
            }

            if (bodyText != null)
            {
                bodyText.text = lines[index];
            }
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (IsOpen)
            {
                Close();
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            advanceAction.Disable();
            cancelAction.Disable();

            Interactable endedTarget = current;
            System.Action callback = onClosed;

            current = null;
            player = null;
            lines = null;
            onClosed = null;

            // Plain != null (not ?.): catches a destroyed-but-not-yet-nulled Interactable.
            if (endedTarget != null)
            {
                endedTarget.RaiseConversationEnded();
            }
            callback?.Invoke();
        }
    }
}
