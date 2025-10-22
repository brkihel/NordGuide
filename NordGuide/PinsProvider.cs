using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NordGuide
{
    /// <summary>
    /// Acessa Minimap.m_pins de forma resiliente (sem depender de campo público),
    /// usando Harmony AccessTools quando disponível e caindo para reflection simples.
    /// </summary>
    internal static class PinsProvider
    {
        // delegate que lê o campo de instância m_pins
        private static AccessTools.FieldRef<Minimap, List<Minimap.PinData>> _pinsRef;
        // fallback via reflection (getter rápido)
        private static Func<Minimap, List<Minimap.PinData>> _fallbackGetter;

        private static bool _loggedFail;

        /// <summary>
        /// Tenta obter a lista de pins do Minimap. Retorna false se não conseguir.
        /// </summary>
        public static bool TryGetPins(out List<Minimap.PinData> pins)
        {
            pins = null;

            var mm = Minimap.instance;
            if (mm == null || Player.m_localPlayer == null)
                return false;

            // Primeira tentativa: usar o delegate gerado pelo Harmony (mais rápido/seguro)
            if (_pinsRef == null && _fallbackGetter == null)
                TryResolveFieldRef();

            if (_pinsRef != null)
            {
                try
                {
                    pins = _pinsRef(mm);
                    return pins != null;
                }
                catch { /* continua para fallback */ }
            }

            // Fallback reflection
            if (_fallbackGetter != null)
            {
                try
                {
                    pins = _fallbackGetter(mm);
                    return pins != null;
                }
                catch { /* cai para log */ }
            }

            if (!_loggedFail)
            {
                _loggedFail = true;
                Debug.LogWarning("[NordGuide] Não foi possível acessar Minimap.m_pins. POIs desativados.");
            }
            return false;
        }

        /// <summary>
        /// Resolve o acesso ao campo m_pins usando nomes alternativos e, se necessário, reflection pura.
        /// </summary>
        private static void TryResolveFieldRef()
        {
            try
            {
                // Nome original atual
                _pinsRef = AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");
                if (_pinsRef != null) return;

                // Alguns nomes alternativos que já apareceram em builds/dev
                foreach (var candidate in new[] { "_pins", "pins", "m_AllPins" })
                {
                    try
                    {
                        _pinsRef = AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>(candidate);
                        if (_pinsRef != null)
                        {
                            Debug.Log($"[NordGuide] Minimap pins field resolvido via '{candidate}'.");
                            return;
                        }
                    }
                    catch { /* tenta o próximo */ }
                }

                // Fallback: reflection tradicional (cria um getter)
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                FieldInfo fi =
                       typeof(Minimap).GetField("m_pins", flags)
                    ?? typeof(Minimap).GetField("_pins", flags)
                    ?? typeof(Minimap).GetField("pins", flags)
                    ?? typeof(Minimap).GetField("m_AllPins", flags);

                if (fi != null)
                {
                    _fallbackGetter = (mm) => (List<Minimap.PinData>)fi.GetValue(mm);
                    Debug.Log("[NordGuide] Minimap pins resolvido via reflection fallback.");
                }
                else
                {
                    Debug.LogWarning("[NordGuide] Não encontrei nenhum campo de pins em Minimap.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NordGuide] Erro resolvendo pins: {e.Message}");
            }
        }
    }
}
