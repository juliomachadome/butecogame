using UnityEngine;
using ButecoDosDevs.NPC;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// A door usable via [E] (Interactable + a trigger collider on the same
    /// GameObject): either loads another scene (optionally flipping
    /// GameState.StreetVisit) or, when 'locked' is set, just shows a message and never
    /// loads anything. Reused for three spots: the Buteco's street exit, the Rua's
    /// "voltar pro Buteco" door, and the Rua's locked rival-bar door before the war —
    /// all driven by the same small script instead of three near-duplicates.
    ///
    /// Self-gates via 'onlyDuringStreetVisit': when set and GameState.StreetVisit is
    /// false at Awake, the whole GameObject disables itself, so it can never interfere
    /// with the main story flow (e.g. the Rua's real war-mode door trigger, which is
    /// handled separately by RuaFlow's own Update check).
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class SceneDoor : MonoBehaviour
    {
        [SerializeField] private bool onlyDuringStreetVisit;
        [SerializeField] private bool locked;
        [SerializeField] private string targetScene;
        [SerializeField] private bool setStreetVisitOnLoad;
        [SerializeField] private bool clearStreetVisitOnLoad;

        [Tooltip("Optional: only the Buteco's exit door needs this, to refuse leaving mid-cutscene or outside free-roam.")]
        [SerializeField] private ButecoFlow gateFlow;

        private Interactable interactable;

        private void Awake()
        {
            if (onlyDuringStreetVisit && !GameState.StreetVisit)
            {
                gameObject.SetActive(false);
                return;
            }

            interactable = GetComponent<Interactable>();
            interactable.OnConversationEndedEvent.AddListener(OnTalked);
        }

        private void OnTalked()
        {
            if (locked || string.IsNullOrEmpty(targetScene))
            {
                return;
            }

            if (gateFlow != null && (!gateFlow.CanVisitStreet || CutsceneMode.IsActive))
            {
                SpeechBubble.Say(transform, "Melhor não sair agora.", 1.6f);
                return;
            }

            if (setStreetVisitOnLoad) GameState.StreetVisit = true;
            if (clearStreetVisitOnLoad) GameState.StreetVisit = false;

            SceneTransition.Load(targetScene);
        }
    }
}
