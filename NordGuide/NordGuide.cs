using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NordGuide
{
    // Metadados do plugin (aparecem no console do BepInEx)
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class NordGuidePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "genesisproj.nordguide";
        public const string PluginName = "NordGuide";
        public const string PluginVersion = "0.1.0";
        public static string ModPath;

        #pragma warning disable CS0649
        private Harmony _harmony;
        #pragma warning restore CS0649
        public static ManualLogSource Log;  // logger global opcional

        public void Awake()
        {
            Logger.LogInfo("NordGuide compass initialized!");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Cria a bússola apenas dentro do jogo (evita menu principal)
            if (scene.name == "main") // cena "main" é a do jogo
            {
                GameObject compassObj = new GameObject("NordGuideCompass");
                DontDestroyOnLoad(compassObj);
                compassObj.AddComponent<CompassRenderer>();
            }
        }

        private void OnDestroy()
        {
            // Remove apenas os patches deste mod, se tivermos aplicado algum
            _harmony?.UnpatchSelf();
        }
    }
}