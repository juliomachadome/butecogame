using UnityEngine;
using ButecoDosDevs.NPC;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// Sandbox soundboard prop (Prop_LoveTester / Prop_SlotMachineSlim): reuses the
    /// existing Interactable + DialogueUI [E] flow instead of a bespoke input path —
    /// each full interaction (open, then close via a second E/Escape) is one "press",
    /// cycling to the next reaction for next time. Applause/laughs give a little
    /// Coragem, boos take a little away; the reaction plays out as balloons on the
    /// crowd via SpeechBubble. AudioClips are optional placeholders (none required yet
    /// per the jam's "todos os sons são gravados pela equipe" rule).
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class SoundboardProp : MonoBehaviour
    {
        [System.Serializable]
        public class Reaction
        {
            public string label = "RUA!!!";
            public string balloonText = "RUA!!!";
            public Transform[] balloonTargets;
            public float courageDelta;
            public SoundId sound = SoundId.Rua;
        }

        [SerializeField] private Interactable interactable;
        [SerializeField] private CourageMeter playerCourage;
        [SerializeField]
        private Reaction[] reactions = new Reaction[]
        {
            new Reaction { label = "Moe: RUA!!!", balloonText = "RUA!!!", courageDelta = 0f, sound = SoundId.Rua },
            new Reaction { label = "Aplausos", balloonText = "AEEE!", courageDelta = 4f, sound = SoundId.Aplausos },
            new Reaction { label = "Vaias", balloonText = "UUUUH", courageDelta = -3f, sound = SoundId.Vaias },
            new Reaction { label = "Risadas", balloonText = "KKKKK", courageDelta = 2f, sound = SoundId.Risadas },
            new Reaction { label = "Burp", balloonText = "*burp*", courageDelta = 0f, sound = SoundId.Burp },
        };

        private int index;

        private void Awake()
        {
            if (interactable == null)
            {
                interactable = GetComponent<Interactable>();
            }
            if (playerCourage == null)
            {
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null)
                {
                    playerCourage = playerGo.GetComponent<CourageMeter>();
                }
            }

            ShowCurrentAsPrompt();
            if (interactable != null)
            {
                interactable.OnConversationEndedEvent.AddListener(OnInteracted);
            }
        }

        private void ShowCurrentAsPrompt()
        {
            if (interactable == null || reactions == null || reactions.Length == 0)
            {
                return;
            }
            Reaction r = reactions[index];
            interactable.SetDialogue("Soundboard", new[] { r.label + " (aperte de novo)" });
        }

        private void OnInteracted()
        {
            if (reactions == null || reactions.Length == 0)
            {
                return;
            }

            Reaction r = reactions[index];

            if (r.balloonTargets != null)
            {
                for (int i = 0; i < r.balloonTargets.Length; i++)
                {
                    Transform t = r.balloonTargets[i];
                    if (t != null)
                    {
                        SpeechBubble.Say(t, r.balloonText, 1.8f);
                    }
                }
            }

            if (playerCourage != null && !Mathf.Approximately(r.courageDelta, 0f))
            {
                playerCourage.Add(r.courageDelta);
            }

            Sfx.Play(r.sound, transform.position);

            index = (index + 1) % reactions.Length;
            ShowCurrentAsPrompt();
        }
    }
}
