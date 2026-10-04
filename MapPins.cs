using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace NameMoreThings
{
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateDynamicPins))]
    internal static class MapPins
    {
        private const float PinSpeed = 200f;
        private const float StaticRefreshInterval = 1f;
        private static readonly string[] FallbackIconPrefabs = { "Karve", "wood_door", "portal_wood" };

        private class Entry
        {
            public Minimap.PinData Pin;
            public string Name;
        }

        private struct Thing
        {
            public ZDOID Id;
            public string Name;
            public Vector3 Position;
            public int Prefab;
            public NamedKind Kind;
        }

        private static readonly Dictionary<ZDOID, Entry> s_pins = new Dictionary<ZDOID, Entry>();
        private static readonly HashSet<ZDOID> s_loaded = new HashSet<ZDOID>();
        private static readonly HashSet<ZDOID> s_shown = new HashSet<ZDOID>();
        private static readonly List<ZDOID> s_remove = new List<ZDOID>();
        private static readonly Dictionary<int, Sprite> s_icons = new Dictionary<int, Sprite>();
        private static readonly List<Thing> s_staticPins = new List<Thing>();
        private static readonly List<Thing> s_candidates = new List<Thing>();
        private static float s_nextStaticRefresh;
        private static Minimap s_map;

        private static void Postfix(Minimap __instance, float dt)
        {
            if (s_map != __instance)
            {
                s_map = __instance;
                s_pins.Clear();
                s_icons.Clear();
                s_nextStaticRefresh = 0f;
            }

            s_loaded.Clear();
            s_shown.Clear();
            if (ModConfig.MapEnabled.Value && (__instance.m_mode == Minimap.MapMode.Large || ModConfig.ShowOnMinimap.Value))
            {
                foreach (NamedObject thing in NamedObject.All)
                {
                    if (!thing.IsValid) continue;
                    s_loaded.Add(thing.Id);
                    if (thing.Kind == NamedKind.Boat && IsShown(thing))
                    {
                        Show(__instance, thing.Id, thing.DisplayName, thing.transform.position, thing.PrefabHash, thing.Kind, dt);
                    }
                }
                foreach (KeyValuePair<ZDOID, NameTracker.Remote> remote in NameTracker.Remotes[(int)NamedKind.Boat])
                {
                    NameTracker.Remote boat = remote.Value;
                    if (IsShown(remote.Key, boat, NamedKind.Boat)) Show(__instance, remote.Key, boat.Name, boat.Position, boat.Prefab, NamedKind.Boat, dt);
                }

                if (Time.time >= s_nextStaticRefresh)
                {
                    s_nextStaticRefresh = Time.time + StaticRefreshInterval;
                    RefreshStaticPins();
                }
                foreach (Thing pin in s_staticPins) Show(__instance, pin.Id, pin.Name, pin.Position, pin.Prefab, pin.Kind, dt);
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

        private static bool IsShown(NamedObject thing) =>
            thing.HasName && DisplayPrefs.ShowOnMap(thing.Uid, thing.Kind) && thing.DisplayName.Length > 0;

        private static bool IsShown(ZDOID id, NameTracker.Remote thing, NamedKind kind) =>
            !s_loaded.Contains(id) && thing.Name.Length > 0 && DisplayPrefs.ShowOnMap(thing.Uid, kind);

        private static void RefreshStaticPins()
        {
            s_staticPins.Clear();

            s_candidates.Clear();
            foreach (NamedObject thing in NamedObject.All)
            {
                if (thing.IsValid && thing.Kind == NamedKind.Door && IsShown(thing))
                {
                    s_candidates.Add(new Thing { Id = thing.Id, Name = thing.DisplayName, Position = thing.transform.position, Prefab = thing.PrefabHash, Kind = NamedKind.Door });
                }
            }
            AddRemoteCandidates(NamedKind.Door);
            AddMerged(ModConfig.MergeDoors.Value, sameNameOnly: true);

            s_candidates.Clear();
            AddRemoteCandidates(NamedKind.Portal);
            AddMerged(ModConfig.MergePortals.Value, sameNameOnly: false);
        }

        private static void AddRemoteCandidates(NamedKind kind)
        {
            foreach (KeyValuePair<ZDOID, NameTracker.Remote> remote in NameTracker.Remotes[(int)kind])
            {
                NameTracker.Remote thing = remote.Value;
                if (IsShown(remote.Key, thing, kind))
                {
                    s_candidates.Add(new Thing { Id = remote.Key, Name = thing.Name, Position = thing.Position, Prefab = thing.Prefab, Kind = kind });
                }
            }
        }

        private static void AddMerged(bool merge, bool sameNameOnly)
        {
            int count = s_candidates.Count;
            if (!merge || count < 2)
            {
                s_staticPins.AddRange(s_candidates);
                return;
            }

            var group = new int[count];
            for (int i = 0; i < count; i++) group[i] = i;
            float maxDistance = ModConfig.MergeDistance.Value;
            float maxSqr = maxDistance * maxDistance;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    Thing a = s_candidates[i];
                    Thing b = s_candidates[j];
                    if (sameNameOnly && a.Name != b.Name) continue;
                    float dx = a.Position.x - b.Position.x;
                    float dz = a.Position.z - b.Position.z;
                    if (dx * dx + dz * dz <= maxSqr) Union(group, i, j);
                }
            }

            var sums = new Vector3[count];
            var members = new int[count];
            var keys = new int[count];
            for (int i = 0; i < count; i++)
            {
                int root = Find(group, i);
                if (members[root] == 0 || s_candidates[i].Id.CompareTo(s_candidates[keys[root]].Id) < 0) keys[root] = i;
                sums[root] += s_candidates[i].Position;
                members[root]++;
            }
            for (int root = 0; root < count; root++)
            {
                if (members[root] == 0) continue;
                Thing pin = s_candidates[keys[root]];
                if (members[root] > 1)
                {
                    pin.Position = sums[root] / members[root];
                    if (!sameNameOnly) pin.Name = "";
                }
                s_staticPins.Add(pin);
            }
        }

        private static int Find(int[] group, int i)
        {
            while (group[i] != i)
            {
                group[i] = group[group[i]];
                i = group[i];
            }
            return i;
        }

        private static void Union(int[] group, int a, int b)
        {
            a = Find(group, a);
            b = Find(group, b);
            if (a != b) group[b] = a;
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
