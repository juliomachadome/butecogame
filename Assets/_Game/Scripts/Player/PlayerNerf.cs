using System.Collections;
using UnityEngine;
using ButecoDosDevs.NPC;
using ButecoDosDevs.Systems;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Player
{
    /// <summary>
    /// The player's toy Nerf gun, picked up from the Nerf-war chaos before the real
    /// arsenal opens (Fase 5). While equipped and no real weapon has been chosen yet,
    /// PlayerAttack.TryAttack fires a harmless foam dart via Fire() instead of a real
    /// melee swing — 0 damage, purely cosmetic, same dart visual as NerfChaos. Cleared
    /// automatically the moment a real weapon is picked up (see PlayerAttack.SetWeapon).
    /// </summary>
    public class PlayerNerf : MonoBehaviour
    {
        [Header("Dart")]
        [SerializeField] private Sprite dartSprite;
        [SerializeField] private float dartRange = 3.2f;
        [SerializeField] private float dartTravelTime = 0.22f;
        [SerializeField] private float dartScale = 0.12f;
        [SerializeField] private Color dartTint = new Color(1f, 0.55f, 0.15f, 1f);
        [SerializeField] private float hitCheckRadius = 0.8f;

        [Header("Visual while equipped (optional)")]
        [SerializeField] private SpriteRenderer heldNerfRenderer;

        public bool HasNerf { get; private set; }

        /// <summary>Grants the Nerf gun. No-ops once a real weapon has been chosen (the
        /// toy phase is over by then).</summary>
        public void Equip()
        {
            if (GameState.ChosenWeapon != GameState.Weapon.None)
            {
                return;
            }
            if (HasNerf)
            {
                return;
            }
            HasNerf = true;
            if (heldNerfRenderer != null)
            {
                heldNerfRenderer.gameObject.SetActive(true);
            }
            SpeechBubble.Say(transform, "Nerf equipada!", 1.4f);
        }

        /// <summary>Called by PlayerAttack once a real weapon replaces the toy.</summary>
        public void Clear()
        {
            HasNerf = false;
            if (heldNerfRenderer != null)
            {
                heldNerfRenderer.gameObject.SetActive(false);
            }
        }

        public void Fire(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.down;
            }
            StartCoroutine(FireRoutine(direction.normalized));
        }

        private IEnumerator FireRoutine(Vector2 dir)
        {
            Vector3 from = transform.position;
            Vector3 to = from + (Vector3)(dir * dartRange);

            GameObject dart = new GameObject("FX_PlayerNerfDart");
            dart.transform.position = from;
            dart.transform.localScale = Vector3.one * dartScale;
            SpriteRenderer sr = dart.AddComponent<SpriteRenderer>();
            sr.sprite = dartSprite;
            sr.color = dartTint;
            sr.sortingOrder = 25;

            Sfx.Play(SoundId.DardoNerf, from);

            NerfChaos hitTarget = NerfChaos.FindNearest(to, hitCheckRadius);

            float t = 0f;
            while (t < dartTravelTime)
            {
                if (dart == null)
                {
                    yield break;
                }
                t += Time.deltaTime;
                float t01 = Mathf.Clamp01(t / dartTravelTime);
                dart.transform.position = Vector3.Lerp(from, to, t01);
                yield return null;
            }

            if (dart != null)
            {
                Destroy(dart);
            }
            if (hitTarget != null)
            {
                hitTarget.ReactHit();
            }
        }
    }
}
