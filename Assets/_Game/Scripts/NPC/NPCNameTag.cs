using TMPro;
using UnityEngine;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// World-space name + "[E] Conversar" prompt shown above an NPC's head while the
    /// player is within interaction range. Two small 3D TextMeshPro labels (no
    /// Canvas needed — the camera never rotates, so flat world text reads fine).
    /// Hidden by default (the GameObject starts inactive in the prefab/scene itself —
    /// Awake must NOT also call SetActive(false): Show() activates this object for the
    /// first time, which runs Awake() synchronously before SetActive(true) returns, so
    /// an Awake-time SetActive(false) here would immediately undo that same Show() call).
    /// PlayerInteractor calls Show/Hide based on proximity.
    /// </summary>
    public class NPCNameTag : MonoBehaviour
    {
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private TextMeshPro promptText;
        [SerializeField] private string promptLabel = "[E] Conversar";

        private void Awake()
        {
            if (promptText != null)
            {
                promptText.text = promptLabel;
            }
        }

        public void Show(string displayName)
        {
            if (nameText != null)
            {
                nameText.text = displayName;
            }
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Runtime construction hook for tags built purely in code (e.g. InteractPrompt
        /// on props/doors, which have no hand-authored NameTag child in the scene).
        /// Assigns the text refs directly and applies promptLabel immediately (Awake,
        /// which would normally apply it, may already have run by the time this is
        /// called since AddComponent triggers Awake synchronously on an active object).
        /// </summary>
        public void Configure(TextMeshPro nameTextRef, TextMeshPro promptTextRef, string promptLabelText)
        {
            nameText = nameTextRef;
            promptText = promptTextRef;
            promptLabel = promptLabelText;
            if (promptText != null)
            {
                promptText.text = promptLabel;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
