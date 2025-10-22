using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NordGuide.Core
{
    internal static class PinsProvider
    {
        private static AccessTools.FieldRef<Minimap, List<Minimap.PinData>> _pinsRef;
        private static Func<Minimap, List<Minimap.PinData>> _fallbackGetter;
        private static bool _loggedFail;
        private static bool _resolved;

        public static bool TryGetPins(out List<Minimap.PinData> pins)
        {
            pins = null;
            var mm = Minimap.instance;
            if (mm == null || Player.m_localPlayer == null)
                return false;

            if (!_resolved)
                TryResolveFieldRef();

            try
            {
                if (_pinsRef != null)
                {
                    pins = _pinsRef(mm);
                    if (pins != null && pins.Count > 0)
                        return true;
                }
                else if (_fallbackGetter != null)
                {
                    pins = _fallbackGetter(mm);
                    if (pins != null && pins.Count > 0)
                        return true;
                }
            }
            catch { }

            if (!_loggedFail)
            {
                _loggedFail = true;
                Logging.Warning("[PinsProvider] Falha ao acessar Minimap.m_pins. POIs desativados.");
            }
            return false;
        }

        private static void TryResolveFieldRef()
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                _pinsRef = AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");
                if (_pinsRef != null)
                {
                    Logging.Info("[PinsProvider] Campo 'm_pins' resolvido com sucesso.");
                    return;
                }

                foreach (var name in new[] { "_pins", "pins", "m_AllPins" })
                {
                    try
                    {
                        _pinsRef = AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>(name);
                        if (_pinsRef != null)
                        {
                            Logging.Info($"[PinsProvider] Campo resolvido via '{name}'.");
                            return;
                        }
                    }
                    catch { }
                }

                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                FieldInfo fi =
                       typeof(Minimap).GetField("m_pins", flags)
                    ?? typeof(Minimap).GetField("_pins", flags)
                    ?? typeof(Minimap).GetField("pins", flags)
                    ?? typeof(Minimap).GetField("m_AllPins", flags);

                if (fi != null)
                {
                    _fallbackGetter = (mm) => (List<Minimap.PinData>)fi.GetValue(mm);
                    Logging.Info("[PinsProvider] Campo de pins resolvido via reflection fallback.");
                }
                else
                {
                    Logging.Warning("[PinsProvider] Nenhum campo de pins encontrado em Minimap.");
                }
            }
            catch (Exception e)
            {
                Logging.Warning($"[PinsProvider] Erro ao resolver campo de pins: {e.Message}");
            }
        }

        public static bool TryGetSpriteTextureAndUv(Sprite sprite, out Texture2D tex, out Rect uv)
        {
            tex = null;
            uv = default;

            if (sprite == null) return false;
            tex = sprite.texture;
            if (tex == null) return false;

            Rect sr = sprite.rect;
            uv = new Rect(sr.x / tex.width, sr.y / tex.height, sr.width / tex.width, sr.height / tex.height);
            return true;
        }

        public static bool TryGetIMGUIRect(Sprite s, out Texture2D tex, out Rect uv, out bool rotated)
        {
            tex = null; uv = default; rotated = false;
            if (s == null) return false;
            tex = s.texture; if (tex == null) return false;

            var u = s.uv;
            float minU = Mathf.Min(u[0].x, u[1].x, u[2].x, u[3].x);
            float maxU = Mathf.Max(u[0].x, u[1].x, u[2].x, u[3].x);
            float minV = Mathf.Min(u[0].y, u[1].y, u[2].y, u[3].y);
            float maxV = Mathf.Max(u[0].y, u[1].y, u[2].y, u[3].y);
            uv = new Rect(minU, minV, maxU - minU, maxV - minV);
            rotated = Mathf.Abs(u[1].y - u[0].y) > Mathf.Abs(u[1].x - u[0].x);
            return true;
        }
    }
}
