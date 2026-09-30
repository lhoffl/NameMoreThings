using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace NameMoreThings
{
    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.LateUpdate))]
    internal static class BoatLabels
    {
        private static readonly Dictionary<NamedObject, TMP_Text> s_labels = new Dictionary<NamedObject, TMP_Text>();

        private static void Postfix(EnemyHud __instance)
        {
            Camera camera = Utils.GetMainCamera();
            bool enabled = ModConfig.LabelsEnabled.Value && camera != null;
            float maxDistance = ModConfig.LabelMaxDistance.Value;
            float fadeStart = maxDistance * 0.75f;

            foreach (NamedObject boat in NamedObject.Boats)
            {
                s_labels.TryGetValue(boat, out TMP_Text label);
                if (!enabled || !boat.HasName || !DisplayPrefs.ShowName(boat.Uid) || boat.DisplayName.Length == 0)
                {
                    Hide(label);
                    continue;
                }

                Vector3 point = boat.transform.position + Vector3.up * (boat.LabelHeight + ModConfig.LabelHeightOffset.Value);
                float distance = Vector3.Distance(camera.transform.position, point);
                Vector3 screen = camera.WorldToScreenPointScaled(point);
                if (distance > maxDistance || screen.z <= 0f)
                {
                    Hide(label);
                    continue;
                }

                if (!label)
                {
                    label = CreateLabel(__instance);
                    s_labels[boat] = label;
                }
                if (label.text != boat.DisplayName) label.text = boat.DisplayName;
                label.fontSize = ModConfig.LabelFontSize.Value;
                label.alpha = distance <= fadeStart ? 1f : Mathf.Clamp01((maxDistance - distance) / (maxDistance - fadeStart));
                label.transform.position = screen;
                if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            }
        }

        public static void Remove(NamedObject boat)
        {
            if (s_labels.TryGetValue(boat, out TMP_Text label))
            {
                if (label) Object.Destroy(label.gameObject);
                s_labels.Remove(boat);
            }
        }

        private static void Hide(TMP_Text label)
        {
            if (label && label.gameObject.activeSelf) label.gameObject.SetActive(false);
        }

        private static TMP_Text CreateLabel(EnemyHud hud)
        {
            Transform root = hud.m_hudRoot.transform;
            Transform template = hud.m_baseHud ? hud.m_baseHud.transform.Find("Name") : null;
            TMP_Text text = template ? Object.Instantiate(template.gameObject, root, false).GetComponent<TMP_Text>() : null;
            if (!text)
            {
                var go = new GameObject("Name", typeof(RectTransform));
                go.transform.SetParent(root, false);
                text = go.AddComponent<TextMeshProUGUI>();
            }
            text.name = "BoatName";
            RectTransform rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(800f, 100f);
            text.alignment = TextAlignmentOptions.Bottom;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.richText = false;
            text.raycastTarget = false;
            text.gameObject.SetActive(false);
            return text;
        }
    }
}
