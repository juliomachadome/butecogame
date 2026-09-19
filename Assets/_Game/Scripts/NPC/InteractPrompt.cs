using TMPro;
using UnityEngine;

namespace ButecoDosDevs.NPC
{
    /// <summary>
    /// Builds a world-space "[E] ..." prompt (no name line) at runtime and wires it
    /// into this GameObject's Interactable, for props/doors that don't have a
    /// hand-authored NameTag child in the scene (Prop_NerfBox, Door_Street, the Rua's
    /// two doors). Mirrors NPCNameTag's look but skips the name label. Reuses
    /// NPCNameTag/Interactable.SetNameTag so PlayerInteractor's existing Show/Hide-on-
    /// proximity logic just works, no special-casing needed there.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class InteractPrompt : MonoBehaviour
    {
        [SerializeField] private string promptLabel = "[E] Usar";
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.1f, 0f);
        [SerializeField] private float fontSize = 2.4f;
        [SerializeField] private Color color = Color.white;

        private NPCNameTag tag;

        private void Awake()
        {
            Interactable interactable = GetComponent<Interactable>();
            if (interactable == null)
            {
                return;
            }

            GameObject tagGo = new GameObject("NameTag_Auto");
            tagGo.transform.SetParent(transform, false);
            tagGo.transform.localPosition = offset;
            tag = tagGo.AddComponent<NPCNameTag>();

            GameObject promptGo = new GameObject("PromptText", typeof(RectTransform));
            promptGo.transform.SetParent(tagGo.transform, false);
            TextMeshPro promptText = promptGo.AddComponent<TextMeshPro>();
            promptText.fontSize = fontSize;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = color;
            RectTransform rt = promptGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(4f, 0.6f);
            }

            tag.Configure(null, promptText, promptLabel);
            tagGo.SetActive(false);

            interactable.SetNameTag(tag);
        }

        /// <summary>Lets a flow script change the label at runtime (e.g. a door that
        /// starts locked and later becomes "[E] Invadir com a tropa").</summary>
        public void SetLabel(string label)
        {
            promptLabel = label;
            if (tag != null)
            {
                // Re-Configure with the same refs, cheapest way to push the new text
                // through NPCNameTag's single text-setting path.
                TextMeshPro promptTextRef = tag.GetComponentInChildren<TextMeshPro>();
                tag.Configure(null, promptTextRef, promptLabel);
            }
        }
    }
}
