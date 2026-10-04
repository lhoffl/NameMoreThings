using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;

namespace NameMoreThings
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Minor)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.lhoffl.NameMoreThings";
        public const string PluginName = "NameMoreThings";
        public const string PluginVersion = "1.2.0";

        internal static ManualLogSource Log;
        internal static ButtonConfig NameButton;

        private Harmony _harmony;
        private FileSystemWatcher _configWatcher;
        private volatile bool _configFileChanged;

        private void Awake()
        {
            Log = Logger;
            ModConfig.Bind(Config);
            AddTranslations();
            NameButton = new ButtonConfig { Name = "Name", Config = ModConfig.NameKey };
            InputManager.Instance.AddButton(PluginGUID, NameButton);
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGUID);
            WatchConfigFile();
        }

        private void Update()
        {
            if (_configFileChanged)
            {
                _configFileChanged = false;
                Config.Reload();
                Log.LogInfo("Config reloaded");
            }
            NameTracker.Update();
            NameDialog.Update();
        }

        private void OnDestroy()
        {
            _configWatcher?.Dispose();
            _harmony?.UnpatchSelf();
        }

        private static void AddTranslations()
        {
            LocalizationManager.Instance.GetLocalization().AddTranslation("English", new Dictionary<string, string>
            {
                { "namemorethings_topic_boat", "Boat name" },
                { "namemorethings_topic_door", "Door name" },
                { "namemorethings_name_boat", "Name boat" },
                { "namemorethings_name_door", "Name door" },
                { "namemorethings_display", "Display options" },
                { "namemorethings_show_name", "Show name above boat" },
                { "namemorethings_show_on_map", "Show on map" },
            });
        }

        private void WatchConfigFile()
        {
            string path = Config.ConfigFilePath;
            _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };
            _configWatcher.Changed += (_, _) => _configFileChanged = true;
            _configWatcher.Created += (_, _) => _configFileChanged = true;
            _configWatcher.Renamed += (_, _) => _configFileChanged = true;
        }
    }
}
