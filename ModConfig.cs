using BepInEx.Configuration;
using UnityEngine;

namespace NameMoreThings
{
    internal static class ModConfig
    {
        private const string GeneralSection = "1 - General";
        private const string BoatSection = "2 - Boats";
        private const string LabelSection = "3 - Names above boats";
        private const string DoorSection = "4 - Doors";
        private const string MapSection = "5 - Map";

        public static ConfigEntry<int> MaxNameLength;
        public static ConfigEntry<KeyCode> NameKey;

        public static ConfigEntry<bool> BoatsEnabled;
        public static ConfigEntry<bool> BoatDefaultShowName;
        public static ConfigEntry<bool> BoatDefaultShowOnMap;

        public static ConfigEntry<bool> LabelsEnabled;
        public static ConfigEntry<float> LabelMaxDistance;
        public static ConfigEntry<int> LabelFontSize;
        public static ConfigEntry<float> LabelHeightOffset;

        public static ConfigEntry<bool> DoorsEnabled;
        public static ConfigEntry<bool> DoorDefaultShowOnMap;

        public static ConfigEntry<bool> MapEnabled;
        public static ConfigEntry<bool> ShowOnMinimap;
        public static ConfigEntry<float> MapUpdateInterval;

        public static void Bind(ConfigFile config)
        {
            MaxNameLength = Bind(config, GeneralSection, "MaxNameLength", 24, "Longest name allowed, in characters.", new AcceptableValueRange<int>(1, 64));
            NameKey = config.Bind(GeneralSection, "NameKey", KeyCode.N, "Key that names the boat or door you're looking at. Controllers use the game's alternate interact instead (the same button that renames tamed creatures). Not synced from the server.");

            BoatsEnabled = Bind(config, BoatSection, "Enabled", true, "Allow naming boats. false also hides existing boat names, but keeps them.");
            BoatDefaultShowName = Bind(config, BoatSection, "DefaultShowName", true, "Whether 'Show name above boat' starts ticked when naming a boat for the first time.");
            BoatDefaultShowOnMap = Bind(config, BoatSection, "DefaultShowOnMap", true, "Whether 'Show on map' starts ticked when naming a boat for the first time.");

            LabelsEnabled = Bind(config, LabelSection, "Enabled", true, "Show boat names above boats. false hides every name and removes the toggle from the naming menu.");
            LabelMaxDistance = Bind(config, LabelSection, "MaxDistance", 100f, "Names are shown for boats within this many meters of the camera.", new AcceptableValueRange<float>(5f, 300f));
            LabelFontSize = Bind(config, LabelSection, "FontSize", 22, "Font size of the name above the boat.", new AcceptableValueRange<int>(8, 64));
            LabelHeightOffset = Bind(config, LabelSection, "HeightOffset", 1f, "Meters between the top of the boat (usually the mast) and its name.", new AcceptableValueRange<float>(-20f, 20f));

            DoorsEnabled = Bind(config, DoorSection, "Enabled", true, "Allow naming doors. false also hides existing door names, but keeps them.");
            DoorDefaultShowOnMap = Bind(config, DoorSection, "DefaultShowOnMap", true, "Whether 'Show on map' starts ticked when naming a door for the first time.");

            MapEnabled = Bind(config, MapSection, "Enabled", true, "Show named boats and doors on the map. false hides every pin and removes the toggle from the naming menu.");
            ShowOnMinimap = Bind(config, MapSection, "ShowOnMinimap", true, "Also show named boats and doors on the small minimap, not just the large map.");
            MapUpdateInterval = Bind(config, MapSection, "UpdateInterval", 2f, "Seconds between the server sending far-away boat positions to players. Boats near you always move smoothly.", new AcceptableValueRange<float>(0.5f, 30f));
        }

        public static bool Enabled(NamedKind kind) => kind == NamedKind.Boat ? BoatsEnabled.Value : DoorsEnabled.Value;

        public static bool DefaultShowOnMap(NamedKind kind) => kind == NamedKind.Boat ? BoatDefaultShowOnMap.Value : DoorDefaultShowOnMap.Value;

        private static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T value, string description, AcceptableValueBase range = null) =>
            config.Bind(section, key, value, new ConfigDescription(description, range, new ConfigurationManagerAttributes { IsAdminOnly = true }));
    }
}
