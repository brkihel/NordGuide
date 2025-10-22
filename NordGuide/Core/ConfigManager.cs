using BepInEx.Configuration;
using System.IO;
using UnityEngine;

namespace NordGuide.Core
{
    internal static class ConfigManager
    {
        public static ConfigEntry<bool> EnableCompass;
        private static FileSystemWatcher watcher;
        public static BepInEx.Configuration.ConfigEntry<bool> HideMinimapHud;
        public static BepInEx.Configuration.ConfigEntry<float> PoiDisappearDistance;

        public static void Initialize(ConfigFile config)
        {
            EnableCompass = config.Bind(
                "HUD",
                "Enable Compass",
                true,
                "Activates or deactivates NordGuide’s compass." 
            );

            HideMinimapHud = config.Bind(
                "HUD",
                "Hide Minimap (Small HUD)",
                false,
                "If set to true, the small HUD minimap is hidden (the large world map still works)."
            );

            PoiDisappearDistance = config.Bind(
                "HUD",
                "POI Disappear Distance",
                500f, // valor padrão - ajuste se quiser
                "Distance (in meters) at which POI icons fully disappear."
            );

            string configPath = config.ConfigFilePath;
            watcher = new FileSystemWatcher(Path.GetDirectoryName(configPath), Path.GetFileName(configPath))
            {
                NotifyFilter = NotifyFilters.LastWrite
            };
            watcher.Changed += (_, _) => ReloadConfig(config);
            watcher.EnableRaisingEvents = true;

            Logging.Info("[ConfigManager] Config loaded and watcher active.");
        }

        private static void ReloadConfig(ConfigFile config)
        {
            try
            {
                config.Reload();
                Logging.Info("[ConfigManager] Config reloaded successfully.");
            }
            catch
            {
                Logging.Warning("[ConfigManager] Failed to reload config.");
            }
        }
    }
}