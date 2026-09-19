using TMPro;
using UnityEngine;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Top-right "OBJETIVO" box. Not a quest system — just a label other scripts
    /// (or Inspector-wired UnityEvents, e.g. Interactable.onConversationEnded) push
    /// text into via SetObjective.
    /// </summary>
    public class ObjectiveUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private string initialObjective = "Converse com o Pedro";

        private void Awake()
        {
            SetObjective(initialObjective);
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null)
            {
                objectiveText.text = text;
            }
        }
    }
}
