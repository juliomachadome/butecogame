using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ButecoDosDevs.Combat;
using ButecoDosDevs.NPC;

namespace ButecoDosDevs.UI
{
    /// <summary>
    /// Lean left-side group panel for Rua/BarRival: one row per ally (name + HP bar),
    /// greyed out on KO, a small "+" tag for Support allies. Builds its rows once from
    /// the wired AllyController list (no Find in Update — a couple of cheap event
    /// subscriptions per row instead).
    /// </summary>
    public class GroupPanelUI : MonoBehaviour
    {
        [System.Serializable]
        private class Row
        {
            public AllyController ally;
            public string displayName;
        }

        [SerializeField] private RectTransform rowsParent;
        [SerializeField] private GameObject rowPrefab; // optional; built procedurally if null
        [SerializeField] private List<Row> allies = new List<Row>();

        private readonly List<(Health health, Image fill, Text label, GameObject go)> built = new List<(Health, Image, Text, GameObject)>();

        private void Start()
        {
            foreach (Row row in allies)
            {
                if (row.ally == null)
                {
                    continue;
                }
                Health health = row.ally.GetComponent<Health>();
                if (health == null)
                {
                    continue;
                }
                BuildRow(row.displayName, health, row.ally.CurrentRole == AllyController.Role.Support);
            }
        }

        private void BuildRow(string label, Health health, bool isSupport)
        {
            GameObject rowGo = new GameObject("Row_" + label);
            rowGo.transform.SetParent(rowsParent != null ? rowsParent : transform, false);
            var rt = rowGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 26f);

            GameObject bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(rowGo.transform, false);
            Image bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.4f);
            Stretch(bg.rectTransform);

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(rowGo.transform, false);
            Image fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.3f, 0.85f, 0.3f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            Stretch(fill.rectTransform);

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(rowGo.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.text = label + (isSupport ? " +" : "");
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            Stretch(text.rectTransform);

            built.Add((health, fill, text, rowGo));

            health.Damaged += (_) => Refresh(health);
            health.Died += () => Refresh(health);
            Refresh(health);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void Refresh(Health health)
        {
            for (int i = 0; i < built.Count; i++)
            {
                if (built[i].health != health)
                {
                    continue;
                }
                float fraction = health.MaxHP > 0f ? Mathf.Clamp01(health.CurrentHP / health.MaxHP) : 0f;
                if (built[i].fill != null)
                {
                    built[i].fill.fillAmount = fraction;
                }
                if (built[i].go != null)
                {
                    built[i].go.GetComponent<CanvasGroup>();
                    CanvasGroup cg = built[i].go.GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        cg = built[i].go.AddComponent<CanvasGroup>();
                    }
                    cg.alpha = health.IsDead ? 0.35f : 1f;
                }
                return;
            }
        }
    }
}
