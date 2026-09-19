using System.Collections;
using TMPro;
using UnityEngine;
using ButecoDosDevs.Combat;
using ButecoDosDevs.Player;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Floating world-space damage numbers. Hooks every Health present in the scene
    /// at Start (FindObjectsByType, once — never in Update) and spawns a rising/fading
    /// TextMeshPro number whenever one takes damage &gt; 0. Also exposes a static
    /// SpawnParry for PlayerBlock's explicit "PARRY!" callout (zero-damage hits are
    /// skipped by the automatic hook, since Health.Damaged fires with amount 0 there).
    /// Uses a small fixed pool of pre-built TextMeshPro objects (round-robin reuse)
    /// so this never allocates per hit.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        [SerializeField] private int poolSize = 12;
        [SerializeField] private float duration = 0.7f;
        [SerializeField] private float riseDistance = 0.7f;
        [SerializeField] private float fontSize = 3.2f;
        [SerializeField] private Color enemyColor = Color.white;
        [SerializeField] private Color playerColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color parryColor = Color.yellow;

        private static DamageNumbers instance;

        private TextMeshPro[] pool;
        private Coroutine[] poolRoutines;
        private int nextIndex;

        private void Awake()
        {
            instance = this;
            BuildPool();
        }

        private void Start()
        {
            HookExistingHealths();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void BuildPool()
        {
            pool = new TextMeshPro[poolSize];
            poolRoutines = new Coroutine[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("DamageNumber_" + i);
                go.transform.SetParent(transform, false);
                var tmp = go.AddComponent<TextMeshPro>();
                tmp.fontSize = fontSize;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.sortingOrder = 500;
                go.SetActive(false);
                pool[i] = tmp;
            }
        }

        private void HookExistingHealths()
        {
            Health[] all = FindObjectsByType<Health>(FindObjectsInactive.Exclude);
            for (int i = 0; i < all.Length; i++)
            {
                Health h = all[i];
                if (h == null)
                {
                    continue;
                }
                bool isPlayer = h.GetComponent<PlayerMovement>() != null;
                h.Damaged += info => OnAnyDamaged(h, isPlayer, info);
            }
        }

        private void OnAnyDamaged(Health target, bool isPlayer, DamageInfo info)
        {
            if (target == null || info.amount <= 0f)
            {
                return; // 0-damage hits are parries; PlayerBlock calls SpawnParry explicitly
            }

            Color color = isPlayer ? playerColor : enemyColor;
            Vector3 pos = target.transform.position + Vector3.up * 0.7f;
            Spawn(pos, Mathf.RoundToInt(info.amount).ToString(), color);
        }

        /// <summary>Explicit "PARRY!" callout, in yellow, at the given world position.</summary>
        public static void SpawnParry(Vector3 worldPosition)
        {
            if (instance != null)
            {
                instance.Spawn(worldPosition, "PARRY!", instance.parryColor);
            }
        }

        private void Spawn(Vector3 worldPosition, string text, Color color)
        {
            if (pool == null || pool.Length == 0)
            {
                return;
            }

            int index = nextIndex;
            nextIndex = (nextIndex + 1) % pool.Length;

            TextMeshPro tmp = pool[index];
            if (tmp == null)
            {
                return;
            }

            if (poolRoutines[index] != null)
            {
                StopCoroutine(poolRoutines[index]);
            }

            tmp.text = text;
            tmp.color = color;
            tmp.transform.position = worldPosition;
            tmp.gameObject.SetActive(true);

            poolRoutines[index] = StartCoroutine(AnimateAndRecycle(index));
        }

        private IEnumerator AnimateAndRecycle(int index)
        {
            TextMeshPro tmp = pool[index];
            Vector3 start = tmp.transform.position;
            Vector3 end = start + Vector3.up * riseDistance;
            Color baseColor = tmp.color;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float frac = Mathf.Clamp01(t / duration);
                tmp.transform.position = Vector3.Lerp(start, end, frac);
                Color c = baseColor;
                c.a = 1f - frac;
                tmp.color = c;
                yield return null;
            }

            tmp.gameObject.SetActive(false);
            poolRoutines[index] = null;
        }
    }
}
