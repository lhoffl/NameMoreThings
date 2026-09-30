using BepInEx.Configuration;
using UnityEngine;

namespace NameMoreThings
{
    internal static class ModConfig
    {
        private const string ServerSection = "1 - Server";
        private const string ControlsSection = "2 - Controls";
        private const string LabelSection = "3 - Names above boats";
        private const string MapSection = "4 - Map";

        public static ConfigEntry<int> MaxNameLength;
        public static ConfigEntry<bool> AllowNamingBoats;
        public static ConfigEntry<bool> AllowNamingDoors;
        public static ConfigEntry<float> MapUpdateInterval;

        public static ConfigEntry<KeyCode> NameKey;

        public static ConfigEntry<bool> LabelsEnabled;
        public static ConfigEntry<bool> LabelShowByDefault;
        public static ConfigEntry<float> LabelMaxDistance;
        public static ConfigEntry<int> LabelFontSize;
        public static ConfigEntry<float> LabelHeightOffset;

        public static ConfigEntry<bool> MapEnabled;
        public static ConfigEntry<bool> ShowOnMinimap;
        public static ConfigEntry<bool> MapShowBoatsByDefault;
        public static ConfigEntry<bool> MapShowDoorsByDefault;

        public static void Bind(ConfigFile config)
        {
            MaxNameLength = BindSynced(config, ServerSection, "MaxNameLength", 24, "Longest name allowed, in characters.", new AcceptableValueRange<int>(1, 64));
            AllowNamingBoats = BindSynced(config, ServerSection, "AllowNamingBoats", true, "Allow players to name boats. Existing names are kept either way.");
            AllowNamingDoors = BindSynced(config, ServerSection, "AllowNamingDoors", true, "Allow players to name doors. Existing names are kept either way.");
            MapUpdateInterval = BindSynced(config, ServerSection, "MapUpdateInterval", 2f, "Seconds between the server sending far-away boat positions to players.", new AcceptableValueRange<float>(0.5f, 30f));

            NameKey = config.Bind(ControlsSection, "NameKey", KeyCode.N, "Key that opens the naming menu for the boat or door you're looking at. Controllers use the alternate interact instead (the same button that renames tamed creatures).");

            LabelsEnabled = config.Bind(LabelSection, "Enabled", true, "Show names floating above boats. false hides them all and removes the toggle from the naming menu.");
            LabelShowByDefault = config.Bind(LabelSection, "ShowByDefault", true, "Show the name above boats you haven't set yourself in the naming menu.");
            LabelMaxDistance = config.Bind(LabelSection, "MaxDistance", 100f, new ConfigDescription("Names are shown for boats within this many meters of the camera.", new AcceptableValueRange<float>(5f, 300f)));
            LabelFontSize = config.Bind(LabelSection, "FontSize", 22, new ConfigDescription("Font size of the name above the boat.", new AcceptableValueRange<int>(8, 64)));
            LabelHeightOffset = config.Bind(LabelSection, "HeightOffset", 1f, new ConfigDescription("Meters between the top of the boat (usually the mast) and its name.", new AcceptableValueRange<float>(-20f, 20f)));

            MapEnabled = config.Bind(MapSection, "Enabled", true, "Show named boats and doors on the map. false hides them all and removes the toggle from the naming menu.");
            ShowOnMinimap = config.Bind(MapSection, "ShowOnMinimap", true, "Also show them on the small minimap, not just the large map.");
            MapShowBoatsByDefault = config.Bind(MapSection, "ShowBoatsByDefault", true, "Show named boats you haven't set yourself in the naming menu.");
            MapShowDoorsByDefault = config.Bind(MapSection, "ShowDoorsByDefault", true, "Show named doors you haven't set yourself in the naming menu.");
        }

        public static bool NamingAllowed(NamedKind kind) => kind == NamedKind.Boat ? AllowNamingBoats.Value : AllowNamingDoors.Value;

        public static bool ShowOnMapByDefault(NamedKind kind) => kind == NamedKind.Boat ? MapShowBoatsByDefault.Value : MapShowDoorsByDefault.Value;

        private static ConfigEntry<T> BindSynced<T>(ConfigFile config, string section, string key, T value, string description, AcceptableValueBase range = null) =>
            config.Bind(section, key, value, new ConfigDescription(description, range, new ConfigurationManagerAttributes { IsAdminOnly = true }));
    }
}
