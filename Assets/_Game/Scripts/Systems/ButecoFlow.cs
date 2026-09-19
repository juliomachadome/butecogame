using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using ButecoDosDevs.NPC;
using ButecoDosDevs.Player;
using ButecoDosDevs.UI;

namespace ButecoDosDevs.Systems
{
    /// <summary>
    /// The Buteco's story script: enum + switch state machine driving Fases 5/6-lean/7
    /// entirely inside the Buteco scene (arrival gag, free exploration, Pedro leaving
    /// and coming back indignant, weapon arsenal, "Bora." send-off). Every wait in this
    /// class has an explicit timeout/fallback — nothing here can block the player
    /// forever (anti-soft-lock). All references are Inspector-wired except the player,
    /// which is found once by tag (same pattern as AllyController/NerfChaos).
    /// </summary>
    public class ButecoFlow : MonoBehaviour
    {
        private enum State
        {
            Arrival,
            MeetPedro,
            Explore,
            PedroLeaves,
            PedroReturns,
            Arsenal,
            Bora,
            Done
        }

        [Header("Cast (Transforms, for balloons/movement)")]
        [SerializeField] private Transform pedro;
        [SerializeField] private NPCSprite pedroSprite;
        [SerializeField] private Transform moe;
        [SerializeField] private Transform julio;
        [SerializeField] private Transform funnie;
        [SerializeField] private Transform reiLuiz;

        [Header("Interactables (to count 'talked to' during Explore)")]
        [SerializeField] private Interactable pedroInteractable;
        [SerializeField] private Interactable moeInteractable;
        [SerializeField] private Interactable julioInteractable;
        [SerializeField] private Interactable funnieInteractable;
        [SerializeField] private Interactable reiLuizInteractable;

        [Header("Player refs")]
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private PlayerHUD playerHUD;
        [SerializeField] private SpriteRenderer heldWeaponRenderer;
        [SerializeField] private GameObject heldWeaponGO;

        [Header("UI")]
        [SerializeField] private ObjectiveUI objectiveUI;

        [Header("World points")]
        [SerializeField] private Vector3 doorPosition = new Vector3(1.1f, 6f, 0f);
        [SerializeField] private Transform arsenalSpawnPoint; // e.g. Prop_PoolTable

        [Header("Arsenal weapon visuals (placeholders)")]
        [SerializeField] private Sprite swordSprite;
        [SerializeField] private Sprite bottleSprite;

        [Header("Chaos / stranger event")]
        [SerializeField] private GameObject genericNpcPrefab;
        [SerializeField] private float strangerIntervalMin = 60f;
        [SerializeField] private float strangerIntervalMax = 90f;

        [Header("Timings")]
        [SerializeField] private float pedroWalkSpeed = 3.5f;
        [SerializeField] private float walkTimeout = 8f;
        [SerializeField] private float exploreTimeout = 90f;
        [SerializeField] private float pedroAwayDuration = 25f;
        [SerializeField] private float weaponChoiceFallbackSeconds = 300f;

        [Header("Next scene (fallback if not in Build Settings)")]
        [SerializeField] private string ruaScenePath = "Assets/_Game/Scenes/Rua.unity";

        [Header("Audio (all slots empty until the team records real SFX)")]
        [SerializeField] private SoundLibrary soundLibrary;
        [SerializeField] private float pedroBanterInterval = 20f;
        [SerializeField] private float pedroBanterRadius = 4f;
        private static readonly string[] PedroBanterLines =
        {
            "500 anos e ainda não aprendi a acender isso.",
            "Falou besteira? TIMEOUT."
        };

        private State state = State.Arrival;
        private Transform player;

        /// <summary>Exposed as an int for QA/debug eval checks (State is a private nested enum).</summary>
        public int CurrentStateIndex => (int)state;

        private bool[] talkedTo = new bool[5]; // Pedro, Moe, Julio, Funnie, ReiLuiz
        private Coroutine strangerRoutine;

        private void Awake()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                player = playerGo.transform;
                if (playerAttack == null) playerAttack = playerGo.GetComponent<PlayerAttack>();
            }

            WireConversationCounters();
            Sfx.SetLibrary(soundLibrary);
        }

        private void Start()
        {
            StartCoroutine(RunFlow());
            StartCoroutine(PedroBanterLoop());
        }

        private IEnumerator PedroBanterLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(pedroBanterInterval);
                if (state == State.Explore && pedro != null && pedro.gameObject.activeInHierarchy && player != null)
                {
                    float dist = Vector2.Distance(pedro.position, player.position);
                    if (dist <= pedroBanterRadius)
                    {
                        string line = PedroBanterLines[Random.Range(0, PedroBanterLines.Length)];
                        Say(pedro, line, 2.4f);
                    }
                }
            }
        }

        private void WireConversationCounters()
        {
            WireCounter(pedroInteractable, 0);
            WireCounter(moeInteractable, 1);
            WireCounter(julioInteractable, 2);
            WireCounter(funnieInteractable, 3);
            WireCounter(reiLuizInteractable, 4);
        }

        private void WireCounter(Interactable interactable, int index)
        {
            if (interactable == null)
            {
                return;
            }
            interactable.OnConversationEndedEvent.AddListener(() => talkedTo[index] = true);
        }

        private int TalkedCount()
        {
            int count = 0;
            for (int i = 0; i < talkedTo.Length; i++)
            {
                if (talkedTo[i]) count++;
            }
            return count;
        }

        private IEnumerator RunFlow()
        {
            yield return RunArrival();
            yield return RunExplore();
            yield return RunPedroLeaves();
            yield return RunPedroReturns();
            yield return RunArsenal();
            yield return RunBora();
            state = State.Done;
        }

        // ---------------- Arrival ----------------

        private IEnumerator RunArrival()
        {
            state = State.Arrival;
            objectiveUI?.SetObjective("Explore o Buteco...");

            yield return new WaitForSeconds(2f);

            Say(pedro, "OPA!", 1.2f);
            NerfChaos.SetAllActive(false);

            yield return new WaitForSeconds(0.6f);

            if (pedro != null && player != null)
            {
                // Stop about 1 unit short of the player so Pedro doesn't overlap them.
                Vector3 dirToPlayer = (player.position - pedro.position);
                Vector3 stopPoint = dirToPlayer.sqrMagnitude > 1f
                    ? player.position - dirToPlayer.normalized * 1f
                    : pedro.position;
                yield return WalkTo(pedro, pedroSprite, stopPoint, pedroWalkSpeed, walkTimeout);
            }

            Say(pedro, "Opa, opa. Quem é você?", 2.2f);
            yield return new WaitForSeconds(2.2f);
            Say(pedro, "Tem 18? Mostra o alistamento.", 2.4f);
            yield return new WaitForSeconds(2.4f);
            if (player != null)
            {
                Say(player, "...sério?", 1.6f);
                yield return new WaitForSeconds(1.6f);
            }
            Say(pedro, "Regra da casa. Sem alistamento, sem cerveja.", 2.6f);
            yield return new WaitForSeconds(2.6f);

            NerfChaos.SetAllActive(true);
            objectiveUI?.SetObjective("Converse com a galera do Buteco");
        }

        // ---------------- Explore ----------------

        private IEnumerator RunExplore()
        {
            state = State.Explore;
            strangerRoutine = StartCoroutine(StrangerLoop());

            float elapsed = 0f;
            while (TalkedCount() < 3 && elapsed < exploreTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (strangerRoutine != null)
            {
                StopCoroutine(strangerRoutine);
                strangerRoutine = null;
            }
        }

        private IEnumerator StrangerLoop()
        {
            while (true)
            {
                float wait = Random.Range(strangerIntervalMin, strangerIntervalMax);
                yield return new WaitForSeconds(wait);
                if (genericNpcPrefab != null && pedro != null)
                {
                    yield return RunStrangerEvent();
                }
            }
        }

        private IEnumerator RunStrangerEvent()
        {
            Sfx.Play(SoundId.PortaAbre, doorPosition);
            GameObject stranger = Instantiate(genericNpcPrefab, doorPosition, Quaternion.identity);
            NerfChaos strangerChaos = stranger.GetComponent<NerfChaos>();
            if (strangerChaos != null)
            {
                strangerChaos.enabled = false; // don't join the dart chaos, this is a scripted vignette
            }

            Transform strangerT = stranger.transform;
            Vector3 pedroOrigin = pedro.position;

            yield return WalkTo(pedro, pedroSprite, strangerT.position + Vector3.down * 0.6f, pedroWalkSpeed, walkTimeout);

            Say(pedro, "Quem é você?", 1.8f);
            yield return new WaitForSeconds(1.8f);
            Say(pedro, "Tem 18?", 1.4f);
            yield return new WaitForSeconds(1.4f);
            Say(pedro, "Cadê o alistamento?", 1.8f);
            yield return new WaitForSeconds(1.8f);

            string[] answers = { "Perdi.", "Tá no gov.br.", "Tenho 17 e meio." };
            string answer = answers[Random.Range(0, answers.Length)];
            Say(strangerT, answer, 1.8f);
            yield return new WaitForSeconds(1.8f);

            if (Random.value < 0.5f)
            {
                Say(pedro, "Pode entrar.", 1.6f);
                yield return new WaitForSeconds(1.6f);
                if (strangerChaos != null)
                {
                    strangerChaos.enabled = true;
                }
            }
            else
            {
                Say(pedro, "TIMEOUT.", 1.4f);
                Sfx.Play(SoundId.Timeout, pedro != null ? pedro.position : doorPosition);
                yield return new WaitForSeconds(1.4f);
                Say(moe, "RUA!!!", 1.4f);
                Sfx.Play(SoundId.Rua, moe != null ? moe.position : doorPosition);
                yield return new WaitForSeconds(1.4f);
                Sfx.Play(SoundId.PortaAbre, doorPosition);
                yield return FlyOutAndDestroy(stranger);
            }

            // Pedro wanders back toward where he started (best-effort, timed out).
            if (pedro != null)
            {
                yield return WalkTo(pedro, pedroSprite, pedroOrigin, pedroWalkSpeed, walkTimeout);
            }
        }

        private IEnumerator FlyOutAndDestroy(GameObject go)
        {
            if (go == null) yield break;
            Transform t = go.transform;
            Vector3 start = t.position;
            Vector3 end = doorPosition;
            float duration = 0.8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (t == null) yield break;
                elapsed += Time.deltaTime;
                float t01 = Mathf.Clamp01(elapsed / duration);
                t.position = Vector3.Lerp(start, end, t01);
                t.Rotate(0f, 0f, 720f * Time.deltaTime);
                t.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t01);
                yield return null;
            }
            if (go != null)
            {
                Destroy(go);
            }
        }

        // ---------------- Pedro Leaves ----------------

        private IEnumerator RunPedroLeaves()
        {
            state = State.PedroLeaves;
            Say(pedro, "Vou dar um pulo no bar da frente chamar a galera pra cá. O que pode dar errado?", 3f);
            yield return new WaitForSeconds(3f);

            if (pedro != null)
            {
                yield return WalkTo(pedro, pedroSprite, doorPosition, pedroWalkSpeed, walkTimeout);
                Sfx.Play(SoundId.PortaAbre, doorPosition);
                pedro.gameObject.SetActive(false);
            }

            objectiveUI?.SetObjective("Aproveite o Buteco (soundboard, jukebox...)");
            yield return new WaitForSeconds(pedroAwayDuration);
        }

        // ---------------- Pedro Returns ----------------

        private IEnumerator RunPedroReturns()
        {
            state = State.PedroReturns;

            if (pedro != null)
            {
                pedro.position = doorPosition;
                pedro.gameObject.SetActive(true);
                Sfx.Play(SoundId.PortaAbre, doorPosition);
            }

            Say(pedro, "ME ACUSARAM DE MANDAR LINK COM VÍRUS!", 2.6f);
            yield return new WaitForSeconds(2.6f);
            Say(pedro, "EU! O MODERADOR!", 2.2f);
            yield return new WaitForSeconds(2.2f);
            Say(pedro, "Isso não vai ficar assim.", 2f);
            yield return new WaitForSeconds(1.4f);
            Say(reiLuiz, "Então é guerra.", 2f);
            yield return new WaitForSeconds(2f);

            objectiveUI?.SetObjective("Pegue uma arma no arsenal (mesa de sinuca)");
        }

        // ---------------- Arsenal ----------------

        private IEnumerator RunArsenal()
        {
            state = State.Arsenal;
            Say(moe, "Tem umas coisas debaixo do balcão...", 2.4f);
            yield return new WaitForSeconds(2.4f);

            SpawnWeaponPickups();

            float elapsed = 0f;
            while (GameState.ChosenWeapon == GameState.Weapon.None && elapsed < weaponChoiceFallbackSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (GameState.ChosenWeapon == GameState.Weapon.None)
            {
                // Anti-soft-lock fallback: nobody chose in time, default to the balanced sword.
                ApplyWeapon(GameState.Weapon.Balanced);
            }
        }

        private void SpawnWeaponPickups()
        {
            Vector3 origin = arsenalSpawnPoint != null ? arsenalSpawnPoint.position + Vector3.up * 0.4f : transform.position;

            SpawnWeaponPickup("Espada Equilibrada", GameState.Weapon.Balanced, 30f, 1f, 1f, swordSprite, 1f, origin + new Vector3(-0.6f, 0.3f, 0f));
            SpawnWeaponPickup("Espadona", GameState.Weapon.BigSlow, 45f, 1.5f, 1f, swordSprite, 1.4f, origin + new Vector3(-0.2f, 0.35f, 0f));
            SpawnWeaponPickup("Espada Curta", GameState.Weapon.ShortFast, 22f, 0.65f, 1f, swordSprite, 0.7f, origin + new Vector3(0.2f, 0.3f, 0f));
            SpawnWeaponPickup("Garrafa", GameState.Weapon.Bottle, 26f, 1f, 2f, bottleSprite != null ? bottleSprite : swordSprite, 0.8f, origin + new Vector3(0.6f, 0.3f, 0f));
        }

        private void SpawnWeaponPickup(string label, GameState.Weapon weapon, float damage, float timeMult, float kbMult, Sprite sprite, float visualScale, Vector3 position)
        {
            GameObject go = new GameObject("Weapon_" + weapon);
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * visualScale;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            Interactable interactable = go.AddComponent<Interactable>();
            interactable.SetDialogue(label, new[] { "Você pega: " + label + "." });
            interactable.OnConversationEndedEvent.AddListener(() => ApplyWeapon(weapon, damage, timeMult, kbMult, sprite, visualScale, label));
        }

        private void ApplyWeapon(GameState.Weapon weapon)
        {
            switch (weapon)
            {
                case GameState.Weapon.BigSlow:
                    ApplyWeapon(weapon, 45f, 1.5f, 1f, swordSprite, 1.4f, GameState.WeaponDisplayName(weapon));
                    break;
                case GameState.Weapon.ShortFast:
                    ApplyWeapon(weapon, 22f, 0.65f, 1f, swordSprite, 0.7f, GameState.WeaponDisplayName(weapon));
                    break;
                case GameState.Weapon.Bottle:
                    ApplyWeapon(weapon, 26f, 1f, 2f, bottleSprite, 0.8f, GameState.WeaponDisplayName(weapon));
                    break;
                default:
                    ApplyWeapon(GameState.Weapon.Balanced, 30f, 1f, 1f, swordSprite, 1f, GameState.WeaponDisplayName(GameState.Weapon.Balanced));
                    break;
            }
        }

        private void ApplyWeapon(GameState.Weapon weapon, float damage, float timeMult, float kbMult, Sprite sprite, float visualScale, string label)
        {
            if (GameState.ChosenWeapon != GameState.Weapon.None)
            {
                // Already chosen (e.g. a second pickup fired after the fallback kicked in) — ignore.
                return;
            }

            GameState.ChosenWeapon = weapon;

            if (playerAttack != null)
            {
                playerAttack.SetWeapon(damage, timeMult, kbMult);
            }
            if (heldWeaponRenderer != null)
            {
                heldWeaponRenderer.sprite = sprite;
            }
            if (heldWeaponGO != null)
            {
                heldWeaponGO.transform.localScale = Vector3.one * visualScale;
                heldWeaponGO.SetActive(true);
            }
            if (playerHUD != null)
            {
                playerHUD.SetWeaponName(label);
            }
        }

        // ---------------- Bora ----------------

        private IEnumerator RunBora()
        {
            state = State.Bora;
            NerfChaos.SetAllActive(false);

            Say(reiLuiz, "Pelo reino!", 2f);
            yield return new WaitForSeconds(1f);
            Say(funnie, "Deploy da armadura em 1 segundo.", 2.2f);
            yield return new WaitForSeconds(1f);
            Say(julio, "Eu cuido da vida de vocês.", 2.2f);
            yield return new WaitForSeconds(1.4f);

            if (pedro != null)
            {
                yield return WalkTo(pedro, pedroSprite, doorPosition, pedroWalkSpeed, walkTimeout);
            }
            Say(pedro, "Bora.", 1.6f);
            Sfx.Play(SoundId.BoraGrito, pedro != null ? pedro.position : doorPosition);
            yield return new WaitForSeconds(1.6f);

            objectiveUI?.SetObjective("Siga o Pedro até a rua!");

            LoadNextScene();
        }

        private void LoadNextScene()
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(ruaScenePath);
            if (buildIndex < 0)
            {
                // Fallback: scene not built yet — don't soft-lock, just say so.
                Debug.LogWarning("[ButecoFlow] Cena '" + ruaScenePath + "' ainda não está em Build Settings. Continua...");
                objectiveUI?.SetObjective("Continua...");
                return;
            }

            SceneManager.LoadScene(buildIndex);
        }

        // ---------------- Helpers ----------------

        private static void Say(Transform anchor, string text, float seconds)
        {
            if (anchor != null && anchor.gameObject.activeInHierarchy)
            {
                SpeechBubble.Say(anchor, text, seconds);
            }
        }

        /// <summary>
        /// Walks a Transform toward target at speed, updating an optional NPCSprite's
        /// facing along the way. Capped at timeoutSeconds so a blocked/edge-case path
        /// (e.g. target unreachable) can never hang the flow forever.
        /// </summary>
        private static IEnumerator WalkTo(Transform mover, NPCSprite sprite, Vector3 target, float speed, float timeoutSeconds)
        {
            if (mover == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                if (mover == null)
                {
                    yield break;
                }

                Vector3 toTarget = target - mover.position;
                float dist = toTarget.magnitude;
                if (dist <= 0.1f)
                {
                    break;
                }

                Vector3 dir = toTarget / Mathf.Max(dist, 0.0001f);
                mover.position += dir * speed * Time.deltaTime;
                if (sprite != null)
                {
                    sprite.SetFacing(dir);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Snap to the target on completion/timeout so downstream logic can rely on the position.
            if (mover != null)
            {
                mover.position = target;
            }
        }
    }
}
