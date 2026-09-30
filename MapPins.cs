using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace NameMoreThings
{
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateDynamicPins))]
    internal static class MapPins
    {
        private const float PinSpeed = 200f;
        private static readonly string[] FallbackIconPrefabs = { "Karve", "wood_door" };

        private class Entry
        {
            public Minimap.PinData Pin;
            public string Name;
        }

        private static readonly Dictionary<ZDOID, Entry> s_pins = new Dictionary<ZDOID, Entry>();
        private static readonly HashSet<ZDOID> s_loaded = new HashSet<ZDOID>();
        private static readonly HashSet<ZDOID> s_shown = new HashSet<ZDOID>();
        private static readonly List<ZDOID> s_remove = new List<ZDOID>();
        private static readonly Dictionary<int, Sprite> s_icons = new Dictionary<int, Sprite>();
        private static Minimap s_map;

        private static void Postfix(Minimap __instance, float dt)
        {
            if (s_map != __instance)
            {
                s_map = __instance;
                s_pins.Clear();
                s_icons.Clear();
            }

            s_loaded.Clear();
            s_shown.Clear();
            if (ModConfig.MapEnabled.Value && (__instance.m_mode == Minimap.MapMode.Large || ModConfig.ShowOnMinimap.Value))
            {
                foreach (NamedObject thing in NamedObject.All)
                {
                    if (!thing.IsValid) continue;
                    ZDOID id = thing.Id;
                    s_loaded.Add(id);
                    if (thing.HasName && DisplayPrefs.ShowOnMap(thing.Uid, thing.Kind) && thing.DisplayName.Length > 0)
                    {
                        Show(__instance, id, thing.DisplayName, thing.transform.position, thing.PrefabHash, thing.Kind, dt);
                    }
                }
                for (int kind = 0; kind < NameTracker.Remotes.Length; kind++)
                {
                    foreach (KeyValuePair<ZDOID, NameTracker.Remote> remote in NameTracker.Remotes[kind])
                    {
                        NameTracker.Remote thing = remote.Value;
                        if (!s_loaded.Contains(remote.Key) && thing.Name.Length > 0 && DisplayPrefs.ShowOnMap(thing.Uid, (NamedKind)kind))
                        {
                            Show(__instance, remote.Key, thing.Name, thing.Position, thing.Prefab, (NamedKind)kind, dt);
                        }
                    }
                }
            }

            s_remove.Clear();
            foreach (KeyValuePair<ZDOID, Entry> pin in s_pins)
            {
                if (!s_shown.Contains(pin.Key)) s_remove.Add(pin.Key);
            }
            foreach (ZDOID id in s_remove)
            {
                __instance.RemovePin(s_pins[id].Pin);
                s_pins.Remove(id);
            }
        }

        private static void Show(Minimap map, ZDOID id, string name, Vector3 position, int prefab, NamedKind kind, float dt)
        {
            s_shown.Add(id);
            if (s_pins.TryGetValue(id, out Entry entry) && entry.Name == name && map.m_pins.Contains(entry.Pin))
            {
                Vector3 moved = Vector3.MoveTowards(entry.Pin.m_pos, position, PinSpeed * dt);
                if (moved != entry.Pin.m_pos)
                {
                    entry.Pin.m_pos = moved;
                    map.m_pinUpdateRequired = true;
                }
                return;
            }

            if (entry != null) map.RemovePin(entry.Pin);
            Minimap.PinData pin = map.AddPin(position, Minimap.PinType.None, name, save: false, isChecked: false);
            pin.m_icon = GetIcon(map, prefab, kind);
            s_pins[id] = new Entry { Pin = pin, Name = name };
        }

        private static Sprite GetIcon(Minimap map, int prefab, NamedKind kind)
        {
            if (s_icons.TryGetValue(prefab, out Sprite icon)) return icon;
            icon = PieceIcon(prefab);
            if (!icon) icon = PieceIcon(FallbackIconPrefabs[(int)kind].GetStableHashCode());
            if (!icon) icon = map.GetSprite(Minimap.PinType.Icon3);
            s_icons[prefab] = icon;
            return icon;
        }

        private static Sprite PieceIcon(int prefab)
        {
            GameObject go = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefab) : null;
            Piece piece = go ? go.GetComponent<Piece>() : null;
            return piece ? piece.m_icon : null;
        }
    }
}
