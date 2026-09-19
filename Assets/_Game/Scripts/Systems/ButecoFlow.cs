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
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerHUD playerHUD;
        [SerializeField] private Transform playerVisual; // e.g. Player/Visual, for the arrival gag's cartoon fly-out spin
        [SerializeField] private SpriteRenderer heldWeaponRenderer;
        [SerializeField] private GameObject heldWeaponGO;

        [Header("UI")]
        [SerializeField] private ObjectiveUI objectiveUI;

        [Header("Camera (cutscene focus)")]
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private float pedroFocusOrthoSize = 4.5f;

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

        /// <summary>True while it's safe to pop out the Buteco's street door: free-roam
        /// (Explore) or the "wait for Pedro" period, and never mid-cutscene. Read by
        /// SceneDoor on the entrance so the exit only opens outside of scripted beats.</summary>
        public bool CanVisitStreet => (state == State.Explore || state == State.PedroLeaves) && !CutsceneMode.IsActive;

        private bool[] talkedTo = new bool[5]; // Pedro, Moe, Julio, Funnie, ReiLuiz
        private Coroutine strangerRoutine;

        private void Awake()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                player = playerGo.transform;
                if (playerAttack == null) playerAttack = playerGo.GetComponent<PlayerAttack>();
                if (playerMovement == null) playerMovement = playerGo.GetComponent<PlayerMovement>();
            }

            if (cameraFollow == null)
            {
                Camera main = Camera.main;
                cameraFollow = main != null ? main.GetComponent<CameraFollow2D>() : FindAnyObjectByType<CameraFollow2D>();
            }

            WireConversationCounters();
            Sfx.SetLibrary(soundLibrary);

            // Pedro stands still facing south (his idle activity anim) until he actually walks somewhere.
            if (pedroSprite != null)
            {
                pedroSprite.SetFacing(Vector2.down);
            }
        }

        private void Start()
        {
            StartCoroutine(RunFlow());
            StartCoroutine(PedroBanterLoop());
            StartCoroutine(FunnieBanterLoop());

            // Only start the combat HUD (HP/Coragem/action bar) once a weapon is chosen;
            // before that, the Buteco is exploration-only (OBJETIVO + [E] prompt suffice).
            if (playerHUD != null)
            {
                playerHUD.SetCombatHudVisible(GameState.ChosenWeapon != GameState.Weapon.None || GameState.WarFinished);
            }
        }

        // ---------------- Funnie ("SaaS em 1 segundo") periodic banter ----------------

        private static readonly string[] FunnieLines =
        {
            "Deploy feito.",
            "Mais um SaaS no ar.",
            "Quem mexeu na main?",
            "Tá rodando na minha máquina."
        };

        [Header("Funnie")]
        [SerializeField] private float funnieBanterInterval = 15f;
        [SerializeField] private float funnieBanterRadius = 4f;

        private IEnumerator FunnieBanterLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(funnieBanterInterval);
                if (funnie != null && funnie.gameObject.activeInHierarchy && player != null)
                {
                    float dist = Vector2.Distance(funnie.position, player.position);
                    if (dist <= funnieBanterRadius)
                    {
                        string line = FunnieLines[Random.Range(0, FunnieLines.Length)];
                        Say(funnie, line, 2.4f);
                    }
                }
            }
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
            if (GameState.WarFinished)
            {
                yield return RunEpilogue();
                state = State.Done;
                yield break;
            }

            if (!GameState.IntroDone)
            {
                yield return RunArrival();
                GameState.IntroDone = true;
            }
            else
            {
                // Returning from a street visit: skip Pedro's "Tem 18?" gag, resume free-roam.
                state = State.Explore;
                NerfChaos.SetAllActive(true);
                objectiveUI?.SetObjective("Converse com a galera do Buteco");
            }

            yield return RunExplore();
            yield return RunPedroLeaves();
            yield return RunPedroReturns();
            yield return RunArsenal();
            yield return RunBora();
            state = State.Done;
        }

        // ---------------- Epilogue (after the war, Fase 9) ----------------

        private IEnumerator RunEpilogue()
        {
            state = State.Explore; // reuse Explore's free-roam behaviour (soundboard, stranger event, etc.)
            objectiveUI?.SetObjective("Modo livre. Fale com o Pedro para uma nova Guerra Púnica");

            yield return new WaitForSeconds(1f);

            BeginCutscene();

            Say(moe, "Rodada por conta da casa. Só hoje.", 2.6f);
            yield return new WaitForSeconds(1.4f);
            Say(reiLuiz, "Essa foi a melhor jam.", 2.4f);
            yield return new WaitForSeconds(1.4f);
            Say(julio, "Criador aprova.", 2f);
            yield return new WaitForSeconds(1.2f);
            Say(pedro, "*tsc tsc*... agora sim acendeu.", 2.6f);
            yield return new WaitForSeconds(2.8f);
            Say(julio, "Parabéns, você zerou meu jogo. Agora vai gravar os sons.", 3.2f);
            yield return new WaitForSeconds(1f);

            CutsceneMode.End();

            NerfChaos.SetAllActive(true);
            strangerRoutine = StartCoroutine(StrangerLoop());

            WireNewWarPrompt();
        }

        /// <summary>Appends "E aí, mais uma Guerra Púnica?" as Pedro's last epilogue line and
        /// listens for that conversation ending to offer the SIM/NÃO restart prompt.</summary>
        private void WireNewWarPrompt()
        {
            if (pedroInteractable == null)
            {
                return;
            }

            string[] baseLines = pedroInteractable.DialogueLines;
            string[] lines;
            if (baseLines != null && baseLines.Length > 0)
            {
                lines = new string[baseLines.Length + 1];
                baseLines.CopyTo(lines, 0);
                lines[baseLines.Length] = "E aí, mais uma Guerra Púnica?";
            }
            else
            {
                lines = new[] { "E aí, mais uma Guerra Púnica?" };
            }
            pedroInteractable.SetDialogue(pedroInteractable.DisplayName, lines);
            pedroInteractable.OnConversationEndedEvent.AddListener(OnEpiloguePedroTalked);
        }

        private void OnEpiloguePedroTalked()
        {
            NewWarPrompt.Show(OnNewWarConfirmed);
        }

        private void OnNewWarConfirmed()
        {
            GameState.ResetAll();
            SceneTransition.Load(SceneManager.GetActiveScene().name);
        }

        // ---------------- Arrival ----------------

        private IEnumerator RunArrival()
        {
            state = State.Arrival;
            GameState.CurrentButecoStage = GameState.ButecoStage.Arrival;
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

            Vector3 arrivalSpot = player != null ? player.position : doorPosition;

            BeginCutscene();
            FaceEachOther(pedro, pedroSprite);

            Say(pedro, "Opa, opa. Quem é você?", 2.2f);
            yield return new WaitForSeconds(2.2f);

            yield return AskGateQuestion(
                "Tem 18? Mostra o alistamento.",
                "Tenho sim.",
                new[] { "Tenho 17 e meio.", "Idade é só um número." },
                arrivalSpot);

            yield return AskGateQuestion(
                "E como foi o alistamento?",
                "Fui dispensado. Excesso de contingente.",
                new[] { "Nem fui, tava codando.", "Alistamento? Sou dev, não soldado." },
                arrivalSpot);

            Say(pedro, "Tá liberado. Bem-vindo ao Buteco, novato.", 2.6f);
            yield return new WaitForSeconds(2.6f);

            CutsceneMode.End();

            NerfChaos.SetAllActive(true);
            objectiveUI?.SetObjective("Converse com a galera do Buteco");
        }

        /// <summary>
        /// Asks a multiple-choice gate question via ChoicePrompt and loops until the
        /// player picks the correct option. A wrong pick triggers TIMEOUT/"RUA!!!" and
        /// the cartoon fly-out gag, then re-asks the same question (bounded by
        /// ChoicePrompt's own input timeout, so this can never hang forever).
        /// </summary>
        private IEnumerator AskGateQuestion(string question, string correctAnswer, string[] wrongAnswers, Vector3 returnSpot)
        {
            while (true)
            {
                Say(pedro, question, 2f);

                string[] options = BuildShuffledOptions(correctAnswer, wrongAnswers, out int correctIndex);

                int chosen = -1;
                ChoicePrompt.Show(question, options, i => chosen = i);
                yield return new WaitUntil(() => chosen >= 0);

                if (chosen == correctIndex)
                {
                    yield break;
                }

                Say(pedro, "TIMEOUT.", 1.4f);
                Sfx.Play(SoundId.Timeout, pedro != null ? pedro.position : doorPosition);
                yield return new WaitForSeconds(1.4f);
                Say(moe, "RUA!!!", 1.4f);
                Sfx.Play(SoundId.Rua, moe != null ? moe.position : doorPosition);
                yield return new WaitForSeconds(1.4f);
                Sfx.Play(SoundId.PortaAbre, doorPosition);

                yield return FlyPlayerOutAndBack(returnSpot);

                FaceEachOther(pedro, pedroSprite);
            }
        }

        private static string[] BuildShuffledOptions(string correct, string[] wrongs, out int correctIndex)
        {
            string[] options = new string[wrongs.Length + 1];
            options[0] = correct;
            for (int i = 0; i < wrongs.Length; i++)
            {
                options[i + 1] = wrongs[i];
            }

            for (int i = options.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (options[i], options[j]) = (options[j], options[i]);
            }

            correctIndex = System.Array.IndexOf(options, correct);
            return options;
        }

        /// <summary>Cartoon "kicked out to the door and back" gag: spins the player's
        /// Visual child while the root flies to doorPosition, then snaps back to
        /// returnSpot facing Pedro. Bounded duration; never destroys the player.</summary>
        private IEnumerator FlyPlayerOutAndBack(Vector3 returnSpot)
        {
            if (player == null)
            {
                yield break;
            }

            Vector3 startPos = player.position;
            float outDuration = 0.6f;
            float elapsed = 0f;
            while (elapsed < outDuration)
            {
                if (player == null)
                {
                    yield break;
                }
                elapsed += Time.deltaTime;
                float t01 = Mathf.Clamp01(elapsed / outDuration);
                player.position = Vector3.Lerp(startPos, doorPosition, t01);
                if (playerVisual != null)
                {
                    playerVisual.localRotation = Quaternion.Euler(0f, 0f, 720f * t01);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            if (playerVisual != null)
            {
                playerVisual.localRotation = Quaternion.identity;
            }
            if (player != null)
            {
                player.position = returnSpot;
            }
        }

        /// <summary>Shared shorthand for CutsceneMode.Begin with this flow's standard
        /// player/HUD refs (movement, attack, HUD, objective panel).</summary>
        private void BeginCutscene()
        {
            CutsceneMode.Begin(playerMovement, playerAttack, playerHUD, objectiveUI != null ? objectiveUI.gameObject : null);
        }

        /// <summary>Turns the player (via PlayerMovement.SetFacingOverride) and npc
        /// toward each other. Only meaningful while movement is disabled (cutscene).</summary>
        private void FaceEachOther(Transform npc, NPCSprite npcSprite)
        {
            if (player == null || npc == null)
            {
                return;
            }
            Vector2 toNpc = (Vector2)(npc.position - player.position);
            if (playerMovement != null)
            {
                playerMovement.SetFacingOverride(toNpc);
            }
            if (npcSprite != null)
            {
                npcSprite.SetFacing(-toNpc);
            }
        }

        // ---------------- Explore ----------------

        private IEnumerator RunExplore()
        {
            state = State.Explore;
            GameState.CurrentButecoStage = GameState.ButecoStage.Explore;
            // The stranger loop is started here but deliberately NOT stopped at the end
            // of Explore anymore: it keeps running through PedroLeaves too (the "wait for
            // Pedro" period is still free-roam), and only stops once RunPedroLeaves hands
            // off to RunPedroReturns (see RunFlow).
            if (strangerRoutine == null)
            {
                strangerRoutine = StartCoroutine(StrangerLoop());
            }

            float elapsed = 0f;
            while (TalkedCount() < 3 && elapsed < exploreTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Drives the "desconhecido aleatório" sandbox vignette. The very first
        /// encounter fires early (5-8s into free-roam, right after Pedro's "Bem-vindo ao
        /// Buteco, novato.") and is always a TIMEOUT, to show the joke right away;
        /// later ones repeat every 45-70s with a 50/50 outcome. Only fires while the
        /// story is actually free-roaming (Explore/PedroLeaves) and no other cutscene is
        /// playing — CutsceneMode.IsActive pauses the countdown rather than skipping it,
        /// so a beat that runs long just delays the next stranger instead of losing it.
        /// </summary>
        private IEnumerator StrangerLoop()
        {
            bool isFirst = true;
            while (state == State.Explore || state == State.PedroLeaves)
            {
                float wait = isFirst ? Random.Range(5f, 8f) : Random.Range(strangerIntervalMin, strangerIntervalMax);
                yield return WaitFreeRoam(wait);

                if (state != State.Explore && state != State.PedroLeaves)
                {
                    yield break;
                }

                if (genericNpcPrefab != null && pedro != null && !CutsceneMode.IsActive)
                {
                    yield return RunStrangerEvent(forceTimeout: isFirst);
                }
                isFirst = false;
            }
        }

        /// <summary>Counts down 'seconds' of free-roam time, pausing (not skipping) while
        /// CutsceneMode is active. Bails out immediately if the story has moved past
        /// Explore/PedroLeaves (e.g. Pedro just walked back in) so the caller's loop
        /// condition re-check ends things cleanly — never blocks anything else.</summary>
        private IEnumerator WaitFreeRoam(float seconds)
        {
            float remaining = seconds;
            while (remaining > 0f)
            {
                if (state != State.Explore && state != State.PedroLeaves)
                {
                    yield break;
                }
                if (!CutsceneMode.IsActive)
                {
                    remaining -= Time.deltaTime;
                }
                yield return null;
            }
        }

        private IEnumerator RunStrangerEvent(bool forceTimeout = false)
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

            bool timeoutOutcome = forceTimeout || Random.value < 0.5f;
            if (!timeoutOutcome)
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
            GameState.CurrentButecoStage = GameState.ButecoStage.PedroLeaves;

            BeginCutscene();
            FaceEachOther(pedro, pedroSprite);

            Say(pedro, "Vou dar um pulo no bar da frente chamar a galera pra cá. O que pode dar errado?", 3f);
            yield return new WaitForSeconds(3f);

            CutsceneMode.End();

            if (pedro != null)
            {
                yield return WalkTo(pedro, pedroSprite, doorPosition, pedroWalkSpeed, walkTimeout);
                Sfx.Play(SoundId.PortaAbre, doorPosition);
                pedro.gameObject.SetActive(false);
            }

            yield return WaitWithCountdownObjective();
        }

        /// <summary>Shows "Espere o Pedro voltar... (mm:ss)" ticking down every second, with two
        /// small sub-objective hints underneath, for pedroAwayDuration seconds. Purely a text
        /// countdown (no gameplay is gated by it) — anti-soft-lock is inherent since it's just
        /// a fixed-length wait either way.</summary>
        private IEnumerator WaitWithCountdownObjective()
        {
            float remaining = pedroAwayDuration;
            while (remaining > 0f)
            {
                int totalSeconds = Mathf.CeilToInt(remaining);
                int mm = totalSeconds / 60;
                int ss = totalSeconds % 60;
                objectiveUI?.SetObjective(
                    $"Espere o Pedro voltar... ({mm:00}:{ss:00})\n<size=80%>• Converse com a galera\n• Teste a soundboard</size>");

                float step = Mathf.Min(1f, remaining);
                yield return new WaitForSeconds(step);
                remaining -= step;
            }
        }

        // ---------------- Pedro Returns ----------------

        private IEnumerator RunPedroReturns()
        {
            state = State.PedroReturns;
            GameState.CurrentButecoStage = GameState.ButecoStage.PedroReturns;

            // The sandbox "desconhecido" vignette only belongs to free-roam
            // (Explore/PedroLeaves); StrangerLoop's own while-condition would stop it on
            // its next check anyway, but stopping it here immediately prevents a
            // straggler stranger from popping in mid-"VOCÊS NÃO VÃO ACREDITAR" cutscene.
            if (strangerRoutine != null)
            {
                StopCoroutine(strangerRoutine);
                strangerRoutine = null;
            }

            if (pedro != null)
            {
                pedro.position = doorPosition;
                pedro.gameObject.SetActive(true);
                Sfx.Play(SoundId.PortaAbre, doorPosition);

                // Walk in from the door so he's not standing on the doormat facing away,
                // then turn to face the player/room before talking.
                Vector3 stopPoint = doorPosition + Vector3.down * 2.5f;
                yield return WalkTo(pedro, pedroSprite, stopPoint, pedroWalkSpeed, walkTimeout);
                if (pedroSprite != null)
                {
                    pedroSprite.SetFacing(Vector2.down);
                }
            }

            // The whole bar stops (Nerf war included) and turns to face Pedro; the camera
            // pushes in on him for the story beats, then both revert at the end.
            NerfChaos.SetAllActive(false);
            FaceCrowdAtPedro();

            BeginCutscene();
            FaceEachOther(pedro, pedroSprite);

            if (cameraFollow != null && pedro != null)
            {
                cameraFollow.BeginFocus(pedro, pedroFocusOrthoSize, 0.6f);
            }

            Say(pedro, "VOCÊS NÃO VÃO ACREDITAR NO QUE ACONTECEU!", 2.6f);
            yield return new WaitForSeconds(2.6f);
            Say(pedro, "Fui lá no Script Kiddies, de boa, divulgar o Buteco...", 2.8f);
            yield return new WaitForSeconds(2.8f);
            Say(pedro, "Mandei o link do nosso Discord no grupo deles...", 2.6f);
            yield return new WaitForSeconds(2.6f);
            Say(pedro, "E os caras disseram que era LINK COM VÍRUS!", 2.6f);
            yield return new WaitForSeconds(2.6f);
            Say(pedro, "VÍRUS! EU! O MODERADOR! 500 ANOS DE COMUNIDADE!", 2.8f);
            yield return new WaitForSeconds(2.8f);
            Say(pedro, "E ainda falaram que o Buteco é coisa de júnior...", 2.6f);
            yield return new WaitForSeconds(2.6f);
            Say(moe, "...ninguém fala assim do meu bar.", 2.2f);
            yield return new WaitForSeconds(2.2f);
            Say(funnie, "Mexeu com o Pedro, mexeu com o Buteco inteiro.", 2.4f);
            yield return new WaitForSeconds(2.4f);
            Say(reiLuiz, "Então é guerra. E essa guerra é nossa.", 2.4f);
            yield return new WaitForSeconds(2.4f);
            Say(pedro, "Então bora pegar as armas. E VAMOS LÁ!", 2.6f);
            yield return new WaitForSeconds(2.6f);

            if (cameraFollow != null)
            {
                cameraFollow.EndFocus(0.6f);
            }
            CutsceneMode.End();
            EndCrowdFocus();

            objectiveUI?.SetObjective("Pegue uma arma no arsenal (mesa de sinuca)");
        }

        /// <summary>Turns every non-fighting community NPC (Moe/Julio/Funnie/Rei Luiz) to
        /// face Pedro via their NPCController's existing Talk-state facing, reused here
        /// purely for the visual (no dialogue is actually opened).</summary>
        private void FaceCrowdAtPedro()
        {
            BeginTalkSafe(moeInteractable);
            BeginTalkSafe(julioInteractable);
            BeginTalkSafe(funnieInteractable);
            BeginTalkSafe(reiLuizInteractable);
        }

        private void EndCrowdFocus()
        {
            EndTalkSafe(moeInteractable);
            EndTalkSafe(julioInteractable);
            EndTalkSafe(funnieInteractable);
            EndTalkSafe(reiLuizInteractable);
            NerfChaos.SetAllActive(true);
        }

        private void BeginTalkSafe(Interactable interactable)
        {
            if (interactable != null && interactable.Controller != null && pedro != null)
            {
                interactable.Controller.BeginTalk(pedro);
            }
        }

        private void EndTalkSafe(Interactable interactable)
        {
            if (interactable != null && interactable.Controller != null)
            {
                interactable.Controller.EndTalk();
            }
        }

        // ---------------- Arsenal ----------------

        [Header("Arsenal recruits (walk to the sinuca, equip a sword, then head to the door)")]
        [SerializeField] private Transform[] genericRecruits; // NPC_GenericButeco_1/2/3
        [SerializeField] private float recruitPickupPause = 0.5f;

        private readonly System.Collections.Generic.List<Transform> weaponIndicators = new System.Collections.Generic.List<Transform>();

        private IEnumerator RunArsenal()
        {
            state = State.Arsenal;
            GameState.CurrentButecoStage = GameState.ButecoStage.Arsenal;

            // Short cutscene: Moe reveals the hidden arsenal under the counter.
            BeginCutscene();
            Say(moe, "Tem umas coisas debaixo do balcão...", 2.2f);
            yield return new WaitForSeconds(2.2f);
            Sfx.Play(SoundId.PortaAbre, arsenalSpawnPoint != null ? arsenalSpawnPoint.position : doorPosition);
            Say(moe, "Escolham com sabedoria.", 1.8f);
            yield return new WaitForSeconds(1.8f);
            CutsceneMode.End();

            SpawnWeaponPickups();
            objectiveUI?.SetObjective("Escolha sua arma na sinuca [E]");
            StartCoroutine(IndicatorBobLoop());

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

            yield return SendAlliesToArsenal();
        }

        private void SpawnWeaponPickups()
        {
            Vector3 origin = arsenalSpawnPoint != null ? arsenalSpawnPoint.position + Vector3.up * 0.4f : transform.position;

            // Spaced well apart along the pool table so each one reads as its own pickup.
            SpawnWeaponPickup("Espada Equilibrada", GameState.Weapon.Balanced, 30f, 1f, 1f, swordSprite, 1f, origin + new Vector3(-1.5f, 0.3f, 0f));
            SpawnWeaponPickup("Espadona", GameState.Weapon.BigSlow, 45f, 1.5f, 1f, swordSprite, 1.4f, origin + new Vector3(-0.5f, 0.35f, 0f));
            SpawnWeaponPickup("Espada Curta", GameState.Weapon.ShortFast, 22f, 0.65f, 1f, swordSprite, 0.7f, origin + new Vector3(0.5f, 0.3f, 0f));
            SpawnWeaponPickup("Garrafa", GameState.Weapon.Bottle, 26f, 1f, 2f, bottleSprite != null ? bottleSprite : swordSprite, 0.8f, origin + new Vector3(1.5f, 0.3f, 0f));
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

            // Name label + "[E] Pegar <arma>" prompt, same look as NPC tags.
            InteractPrompt prompt = go.AddComponent<InteractPrompt>();
            prompt.SetLabel("[E] Pegar " + label);

            // A small bobbing diamond above the weapon while the choice is still open
            // (IndicatorBobLoop below hides/destroys these once a weapon is chosen).
            GameObject indicatorGo = new GameObject("Indicator");
            indicatorGo.transform.SetParent(go.transform, false);
            indicatorGo.transform.localPosition = new Vector3(0f, 1.6f / Mathf.Max(visualScale, 0.01f), 0f);
            indicatorGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            SpriteRenderer indicatorSr = indicatorGo.AddComponent<SpriteRenderer>();
            indicatorSr.sprite = GetOrCreateSolidSprite();
            indicatorSr.color = new Color(1f, 0.85f, 0.2f, 1f);
            indicatorSr.sortingOrder = 20;
            indicatorGo.transform.localScale = Vector3.one * 0.25f;
            weaponIndicators.Add(indicatorGo.transform);
        }

        private static Sprite solidSpriteCache;

        /// <summary>Cheap 1x1-pixel white sprite, tinted per-use via SpriteRenderer.color —
        /// the project's established "quadrado colorido" placeholder style, used here for
        /// the arsenal's bobbing pick-a-weapon indicator (no art asset needed).</summary>
        private static Sprite GetOrCreateSolidSprite()
        {
            if (solidSpriteCache != null)
            {
                return solidSpriteCache;
            }
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            solidSpriteCache = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return solidSpriteCache;
        }

        /// <summary>Bobs every weapon-pickup indicator up/down while the player still has
        /// no weapon; stops and destroys them the moment GameState.ChosenWeapon is set
        /// (self-terminating, no external stop call needed — can't leak forever).</summary>
        private IEnumerator IndicatorBobLoop()
        {
            Vector3[] basePositions = new Vector3[weaponIndicators.Count];
            for (int i = 0; i < weaponIndicators.Count; i++)
            {
                if (weaponIndicators[i] != null)
                {
                    basePositions[i] = weaponIndicators[i].localPosition;
                }
            }

            float t = 0f;
            while (GameState.ChosenWeapon == GameState.Weapon.None)
            {
                t += Time.deltaTime;
                float bob = Mathf.Sin(t * 3f) * 0.1f;
                for (int i = 0; i < weaponIndicators.Count; i++)
                {
                    Transform ind = weaponIndicators[i];
                    if (ind != null)
                    {
                        ind.localPosition = basePositions[i] + Vector3.up * bob;
                    }
                }
                yield return null;
            }

            for (int i = 0; i < weaponIndicators.Count; i++)
            {
                if (weaponIndicators[i] != null)
                {
                    Destroy(weaponIndicators[i].gameObject);
                }
            }
            weaponIndicators.Clear();
        }

        /// <summary>
        /// Once a weapon is chosen, walks Pedro, Rei Luiz and the generic Buteco members
        /// to the arsenal one at a time (staggered, not simultaneous), equips a sword
        /// HeldItem on each, then sends them toward the door to wait for "Bora.". Every
        /// leg uses WalkTo, which already has its own per-walk timeout, so a stuck
        /// recruit just teleports to its target instead of stalling the send-off
        /// (anti-soft-lock).
        /// </summary>
        private IEnumerator SendAlliesToArsenal()
        {
            System.Collections.Generic.List<Transform> recruits = new System.Collections.Generic.List<Transform>();
            if (pedro != null) recruits.Add(pedro);
            if (reiLuiz != null) recruits.Add(reiLuiz);
            if (genericRecruits != null)
            {
                foreach (Transform t in genericRecruits)
                {
                    if (t != null) recruits.Add(t);
                }
            }

            Vector3 arsenalPos = arsenalSpawnPoint != null ? arsenalSpawnPoint.position : transform.position;

            for (int i = 0; i < recruits.Count; i++)
            {
                Transform recruit = recruits[i];
                if (recruit == null || !recruit.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // Generic Buteco members may still have their Nerf-war loop active
                // (EndCrowdFocus turns it back on after Pedro's return speech); stop it
                // so it can't fight this scripted walk for control of their position.
                NerfChaos nerfChaos = recruit.GetComponent<NerfChaos>();
                if (nerfChaos != null)
                {
                    nerfChaos.enabled = false;
                }

                NPCSprite sprite = recruit == pedro ? pedroSprite : recruit.GetComponent<NPCSprite>();
                Vector3 pickupSpot = arsenalPos + new Vector3(Mathf.Sin(i * 1.3f) * 0.6f, 0.4f + (i % 2) * 0.2f, 0f);

                yield return WalkTo(recruit, sprite, pickupSpot, pedroWalkSpeed, walkTimeout);
                EquipSword(recruit, sprite);
                yield return new WaitForSeconds(recruitPickupPause);

                Vector3 doorSpot = doorPosition + new Vector3(-1f + i * 0.4f, 1.2f, 0f);
                yield return WalkTo(recruit, sprite, doorSpot, pedroWalkSpeed, walkTimeout);
            }
        }

        /// <summary>Attaches a runtime sword HeldItem child to an ally NPC so the arsenal
        /// pickup reads visually (same idea as the player's Visual/HeldItem_Weapon).
        /// No-op if the ally already has one (e.g. re-entering this step twice).</summary>
        private void EquipSword(Transform recruit, NPCSprite sprite)
        {
            if (recruit == null || recruit.Find("HeldItem_Sword_Auto") != null)
            {
                return;
            }

            GameObject go = new GameObject("HeldItem_Sword_Auto");
            go.transform.SetParent(recruit, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = swordSprite;
            HeldItem held = go.AddComponent<HeldItem>();
            // HeldItem reads its facing source and item renderer via reflection-free
            // public setters isn't available (Inspector-only fields), so wire the two it
            // needs through SetRefs instead of leaving it faceless.
            held.SetRefs(sprite, sr);
        }

        private void ApplyWeapon(GameState.Weapon weapon)
        {
            if (weapon == GameState.Weapon.None)
            {
                weapon = GameState.Weapon.Balanced;
            }
            GameState.WeaponStats stats = GameState.GetWeaponStats(weapon);
            Sprite sprite = weapon == GameState.Weapon.Bottle && bottleSprite != null ? bottleSprite : swordSprite;
            ApplyWeapon(weapon, stats.damage, stats.timeMultiplier, stats.knockbackMultiplier, sprite, stats.visualScale, GameState.WeaponDisplayName(weapon));
        }

        private void ApplyWeapon(GameState.Weapon weapon, float damage, float timeMult, float kbMult, Sprite sprite, float visualScale, string label)
        {
            if (GameState.ChosenWeapon != GameState.Weapon.None)
            {
                // Already chosen (e.g. a second pickup fired after the fallback kicked in) — ignore.
                return;
            }

            GameState.ChosenWeapon = weapon;
            GameState.ChosenWeaponSprite = sprite;

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
                playerHUD.SetCombatHudVisible(true);
            }
        }

        // ---------------- Bora ----------------

        private IEnumerator RunBora()
        {
            state = State.Bora;
            GameState.CurrentButecoStage = GameState.ButecoStage.Bora;
            NerfChaos.SetAllActive(false);

            BeginCutscene();

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
            FaceEachOther(pedro, pedroSprite);
            Say(pedro, "Bora.", 1.6f);
            Sfx.Play(SoundId.BoraGrito, pedro != null ? pedro.position : doorPosition);
            yield return new WaitForSeconds(1.6f);

            CutsceneMode.End();

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
