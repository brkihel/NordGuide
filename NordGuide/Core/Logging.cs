using BepInEx.Logging;

namespace NordGuide.Core
{
    internal static class Logging
    {
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource log)
        {
            _logger = log;
        }

        public static void Info(string msg) => _logger?.LogInfo(msg);
        public static void Warning(string msg) => _logger?.LogWarning(msg);
        public static void Error(string msg) => _logger?.LogError(msg);
    }
}
