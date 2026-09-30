using HarmonyLib;

namespace NameMoreThings
{
    [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
    internal static class ShipAwakePatch
    {
        private static void Postfix(Ship __instance) => Patches.AddNamedObject(__instance.m_nview);
    }

    [HarmonyPatch(typeof(Door), nameof(Door.Awake))]
    internal static class DoorAwakePatch
    {
        private static void Postfix(Door __instance) => Patches.AddNamedObject(__instance.m_nview);
    }

    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.Interact))]
    internal static class RudderInteractPatch
    {
        private static bool Prefix(ShipControlls __instance, Humanoid character, bool repeat, bool alt, ref bool __result) =>
            Patches.TryGamepadName(__instance.m_ship ? __instance.m_ship.GetComponent<NamedObject>() : null, character, repeat, alt, ref __result);
    }

    [HarmonyPatch(typeof(Door), nameof(Door.Interact))]
    internal static class DoorInteractPatch
    {
        private static bool Prefix(Door __instance, Humanoid character, bool hold, bool alt, ref bool __result) =>
            Patches.TryGamepadName(__instance.GetComponent<NamedObject>(), character, hold, alt, ref __result);
    }

    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.GetHoverText))]
    internal static class RudderHoverTextPatch
    {
        private static void Postfix(ShipControlls __instance, ref string __result)
        {
            NamedObject boat = __instance.m_ship ? __instance.m_ship.GetComponent<NamedObject>() : null;
            bool canName = Player.m_localPlayer != null && __instance.InUseDistance(Player.m_localPlayer);
            Patches.AddHoverText(boat, canName, ref __result);
        }
    }

    [HarmonyPatch(typeof(Door), nameof(Door.GetHoverText))]
    internal static class DoorHoverTextPatch
    {
        private static void Postfix(Door __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result)) return;
            bool canName = PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false);
            Patches.AddHoverText(__instance.GetComponent<NamedObject>(), canName, ref __result);
        }
    }

    internal static class Patches
    {
        public static void AddNamedObject(ZNetView nview)
        {
            if (nview && nview.GetZDO() != null && !nview.GetComponent<NamedObject>())
            {
                nview.gameObject.AddComponent<NamedObject>();
            }
        }

        public static bool TryGamepadName(NamedObject target, Humanoid character, bool hold, bool alt, ref bool result)
        {
            if (!alt || !ZInput.IsGamepadActive() || target == null || !target.IsValid || !ModConfig.Enabled(target.Kind)) return true;
            result = !hold && NameDialog.Open(target, character);
            return false;
        }

        public static void AddHoverText(NamedObject target, bool canName, ref string text)
        {
            if (target == null || !target.IsValid || !ModConfig.Enabled(target.Kind)) return;

            string name = target.DisplayName;
            if (name.Length > 0) text = "<b>" + name + "</b>\n" + text;
            if (!canName) return;

            string key;
            if (ZInput.IsGamepadActive()) key = ZInput.IsNonClassicFunctionality() ? "$KEY_AltKeys + $KEY_Use" : "$KEY_AltPlace + $KEY_Use";
            else key = ZInput.instance.GetBoundKeyString(Plugin.NameButton.Name, emptyStringOnMissing: true);
            if (key.Length == 0) return;
            string action = target.Kind == NamedKind.Boat ? "$namemorethings_name_boat" : "$namemorethings_name_door";
            text += Localization.instance.Localize("\n[<color=yellow><b>" + key + "</b></color>] " + action);
        }
    }
}
