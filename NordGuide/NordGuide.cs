using BepInEx;
using BepInEx.Logging;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace NordGuide
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class NordGuide : BaseUnityPlugin
    {
        public const string PluginGuid = "genesisproj.nordguide";
        public const string PluginName = "NordGuide";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Core.Logging.Initialize(Logger);

            Log.LogInfo("[NordGuide] Initializing...");

            Core.AssetsManager.Initialize();
            Core.ConfigManager.Initialize(Config);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "main") return;

            GameObject go = new GameObject("NordGuideCompass");
            DontDestroyOnLoad(go);
            go.AddComponent<Core.CompassRenderer>();

            Log.LogInfo("[NordGuide] Compass initialized successfully!");
        }

        private void OnDestroy()
        {
            Log.LogInfo("[NordGuide] Unloaded.");
        }
    }
}
