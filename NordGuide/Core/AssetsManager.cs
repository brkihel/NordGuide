using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NordGuide.Core
{
    internal static class AssetsManager
    {
        private static readonly Dictionary<string, Texture2D> _cache = new();
        private static string _assetPath;

        public static void Initialize()
        {
            _assetPath = Path.Combine(Paths.PluginPath, "NordGuide", "Assets").Replace("\\", "/");
            if (!Directory.Exists(_assetPath))
                Directory.CreateDirectory(_assetPath);

            Logging.Info($"[AssetsManager] Assets path: {_assetPath}");
        }

        public static Texture2D LoadTexture(string fileName)
        {
            if (_cache.TryGetValue(fileName, out var cached))
                return cached;

            string fullPath = Path.Combine(_assetPath, fileName);
            if (!File.Exists(fullPath))
            {
                Logging.Warning($"[AssetsManager] Missing texture: {fileName}");
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                var imageConvType = Type.GetType("UnityEngine.ImageConversion, UnityEngine");
                var loadImageMethod = imageConvType?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
                loadImageMethod?.Invoke(null, new object[] { tex, data, false });

                _cache[fileName] = tex;
                return tex;
            }
            catch (Exception e)
            {
                Logging.Error($"[AssetsManager] Failed to load {fileName}: {e.Message}");
                return null;
            }
        }
    }
}
