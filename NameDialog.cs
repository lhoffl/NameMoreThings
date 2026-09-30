using GUIFramework;
using HarmonyLib;
using Jotunn.Managers;
using Splatform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NameMoreThings
{
    internal class NameReceiver : TextReceiver
    {
        private readonly NamedObject _target;

        public NameReceiver(NamedObject target)
        {
            _target = target;
        }

        public string GetText() => _target ? _target.DisplayName : "";

        public void SetText(string text)
        {
            if (!_target) return;
            bool showName = _target.Kind == NamedKind.Boat && NameDialog.ShowNameChecked;
            _target.SetName(text, showName, NameDialog.ShowOnMapChecked);
        }
    }

    internal static class NameDialog
    {
        private const float StripHeight = 52f;
        private const float StripGap = 6f;
        private const float BoxSize = 26f;
        private const float LabelGap = 10f;
        private const float ToggleSpacing = 40f;
        private const float FontSize = 20f;

        internal static bool ShowNameChecked;
        internal static bool ShowOnMapChecked;

        private static TextInput s_builtFor;
        private static RectTransform s_strip;
        private static Toggle s_showNameToggle;
        private static Toggle s_showOnMapToggle;
        private static bool s_selectAllOnFocus;
        private static NamedKind s_kind;

        public static void CheckNameKey()
        {
            Player player = Player.m_localPlayer;
            if (player == null || !ZInput.GetButtonDown(Plugin.NameButton.Name) || !player.TakeInput() || player.InPlaceMode()) return;
            GameObject hover = player.GetHoverObject();
            NamedObject target = hover ? hover.GetComponentInParent<NamedObject>() : null;
            if (target && target.IsValid && ModConfig.Enabled(target.Kind)) Open(target, player);
        }

        public static bool Open(NamedObject target, Humanoid user)
        {
            if (!PrivateArea.CheckAccess(target.transform.position))
            {
                user.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                return true;
            }
            var platform = PlatformManager.DistributionPlatform;
            if (!platform.PrivilegeProvider.CheckPrivilege(Privilege.ViewUserGeneratedContent).IsGranted())
            {
                platform.UIProvider.ResolvePrivilege?.Open(Privilege.ViewUserGeneratedContent);
                return false;
            }
            TextInput input = TextInput.instance;
            if (!input) return false;

            bool named = target.HasName;
            s_kind = target.Kind;
            ShowNameChecked = named ? target.ShowName : ModConfig.BoatDefaultShowName.Value;
            ShowOnMapChecked = named ? target.ShowOnMap : ModConfig.DefaultShowOnMap(target.Kind);
            string topic = target.Kind == NamedKind.Boat ? "$namemorethings_topic_boat" : "$namemorethings_topic_door";
            input.RequestText(new NameReceiver(target), topic, ModConfig.MaxNameLength.Value);
            return true;
        }

        [HarmonyPatch(typeof(TextInput), nameof(TextInput.RequestText))]
        private static class RequestTextPatch
        {
            private static void Postfix(TextInput __instance, TextReceiver sign)
            {
                bool ours = sign is NameReceiver;
                if (ours && s_builtFor != __instance) Build(__instance);
                if (s_builtFor != __instance || !s_strip) return;

                __instance.m_inputField.onFocusSelectAll = s_selectAllOnFocus;
                s_strip.gameObject.SetActive(ours);
                if (ours) Layout();
            }
        }

        private static void Layout()
        {
            bool showName = s_kind == NamedKind.Boat && ModConfig.LabelsEnabled.Value;
            bool showOnMap = ModConfig.MapEnabled.Value;
            s_strip.gameObject.SetActive(showName || showOnMap);
            s_showNameToggle.gameObject.SetActive(showName);
            s_showOnMapToggle.gameObject.SetActive(showOnMap);
            s_showNameToggle.SetIsOnWithoutNotify(ShowNameChecked);
            s_showOnMapToggle.SetIsOnWithoutNotify(ShowOnMapChecked);

            float nameWidth = showName ? PreferredWidth(s_showNameToggle) : 0f;
            float mapWidth = showOnMap ? PreferredWidth(s_showOnMapToggle) : 0f;
            float total = nameWidth + mapWidth + (showName && showOnMap ? ToggleSpacing : 0f);
            float available = s_strip.rect.width - 40f;
            float scale = total > available ? available / total : 1f;
            float x = -total * scale / 2f;
            if (showName) x = Place(s_showNameToggle, x, nameWidth * scale) + ToggleSpacing * scale;
            if (showOnMap) Place(s_showOnMapToggle, x, mapWidth * scale);
        }

        private static float PreferredWidth(Toggle toggle)
        {
            Transform labelTransform = toggle.transform.Find("Label");
            TMP_Text label = labelTransform ? labelTransform.GetComponent<TMP_Text>() : null;
            float textWidth = label ? label.GetPreferredValues(label.text).x : 0f;
            return BoxSize + LabelGap + (textWidth > 1f ? textWidth : 150f);
        }

        private static float Place(Toggle toggle, float left, float width)
        {
            var rt = (RectTransform)toggle.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(width, BoxSize + 6f);
            rt.anchoredPosition = new Vector2(left, 0f);
            return left + width;
        }

        private static void Build(TextInput input)
        {
            if (s_strip) Object.Destroy(s_strip.gameObject);
            s_builtFor = input;
            s_selectAllOnFocus = input.m_inputField.onFocusSelectAll;

            var panel = (RectTransform)input.m_panel.transform;
            var strip = new GameObject("NameMoreThingsOptions", typeof(RectTransform));
            s_strip = (RectTransform)strip.transform;
            s_strip.SetParent(panel, false);
            s_strip.anchorMin = s_strip.anchorMax = new Vector2(0.5f, 0f);
            s_strip.pivot = new Vector2(0.5f, 1f);
            s_strip.sizeDelta = new Vector2(panel.rect.width, StripHeight);
            s_strip.anchoredPosition = new Vector2(0f, -StripGap);

            var background = strip.AddComponent<Image>();
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage)
            {
                background.sprite = panelImage.sprite;
                background.type = panelImage.type;
                background.color = panelImage.color;
                background.material = panelImage.material;
                background.fillCenter = panelImage.fillCenter;
                background.pixelsPerUnitMultiplier = panelImage.pixelsPerUnitMultiplier;
            }
            else
            {
                GUIManager.Instance.ApplyWoodpanelStyle(strip.transform);
            }

            Transform cancelText = panel.Find("Cancel/Text");
            TMP_Text textTemplate = cancelText ? cancelText.GetComponent<TMP_Text>() : null;
            if (!textTemplate) textTemplate = input.m_topic;
            Transform cancelHint = panel.Find("Cancel/gamepad_hint");
            GameObject hintTemplate = cancelHint ? cancelHint.gameObject : null;
            s_showNameToggle = CreateToggle(s_strip, "$namemorethings_show_name", "JoyButtonY", "$KEY_ButtonY", textTemplate, hintTemplate, on => ShowNameChecked = on);
            s_showOnMapToggle = CreateToggle(s_strip, "$namemorethings_show_on_map", "JoyLTrigger", "$KEY_LTrigger", textTemplate, hintTemplate, on => ShowOnMapChecked = on);
            strip.SetActive(false);
        }

        private static Toggle CreateToggle(Transform parent, string labelToken, string joyButton, string joyHint, TMP_Text textTemplate, GameObject hintTemplate, System.Action<bool> onChanged)
        {
            var root = new GameObject("Toggle", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var box = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var boxRt = (RectTransform)box.transform;
            boxRt.SetParent(root.transform, false);
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0f, 0.5f);
            boxRt.pivot = new Vector2(0f, 0.5f);
            boxRt.sizeDelta = new Vector2(BoxSize, BoxSize);
            boxRt.anchoredPosition = Vector2.zero;

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            var checkRt = (RectTransform)check.transform;
            checkRt.SetParent(boxRt, false);
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = checkRt.offsetMax = Vector2.zero;

            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            GUIManager.Instance.ApplyToogleStyle(toggle);

            TMP_Text label;
            if (textTemplate)
            {
                label = Object.Instantiate(textTemplate.gameObject, root.transform, false).GetComponent<TMP_Text>();
            }
            else
            {
                label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                label.transform.SetParent(root.transform, false);
            }
            label.name = "Label";
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.offsetMin = new Vector2(BoxSize + LabelGap, 0f);
            labelRt.offsetMax = Vector2.zero;
            label.enableAutoSizing = false;
            label.fontSize = FontSize;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = true;
            label.text = Localization.instance.Localize(labelToken);

            if (hintTemplate)
            {
                GameObject hint = Object.Instantiate(hintTemplate, boxRt, false);
                var hintRt = (RectTransform)hint.transform;
                hintRt.anchorMin = hintRt.anchorMax = new Vector2(0f, 1f);
                hintRt.anchoredPosition = Vector2.zero;
                TMP_Text hintText = hint.GetComponentInChildren<TMP_Text>(true);
                if (hintText) hintText.text = Localization.instance.Localize(joyHint);
                var gamepad = root.AddComponent<UIGamePad>();
                gamepad.m_zinputKey = joyButton;
                gamepad.m_hint = hint;
            }

            toggle.onValueChanged.AddListener(on =>
            {
                onChanged(on);
                RefocusInput();
            });
            return toggle;
        }

        private static void RefocusInput()
        {
            GuiInputField field = s_builtFor ? s_builtFor.m_inputField : null;
            if (!field || field.isFocused) return;
            field.onFocusSelectAll = false;
            field.ActivateInputField();
        }
    }
}
