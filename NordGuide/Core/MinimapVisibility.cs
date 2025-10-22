using System.Reflection;
using UnityEngine;

namespace NordGuide.Core {
    /// <summary>
    /// Liga/desliga o minimapa HUD (o "mini" no canto) conforme a config.
    /// Robusto contra recriações do HUD: re-resolve periodicamente e reaplica.
    /// </summary>
    internal static class MinimapVisibility {
        // cache leve do root do minimapa pequeno
        private static FieldInfo _fiSmallRoot;
        private static GameObject _smallRootGO;

        // resolver a cada N frames (evita custo por frame e pega recriações do HUD)
        private const int ResolveIntervalFrames = 30;
        private static int _lastResolveFrame;

        public static void Tick() {
            var mm = Minimap.instance;
            if (mm == null)
                return;

            bool hide = ConfigManager.HideMinimapHud.Value;

            // re-resolve periodicamente ou se perdemos a referência
            if (_smallRootGO == null || Time.frameCount - _lastResolveFrame >= ResolveIntervalFrames) {
                ResolveSmallRoot( mm );
                _lastResolveFrame = Time.frameCount;
            }

            // se achamos o smallRoot, aplica nele; senão, apenas aguarda próxima resolução
            if (_smallRootGO != null) {
                bool desiredActive = !hide;
                if (_smallRootGO.activeSelf != desiredActive) {
                    _smallRootGO.SetActive( desiredActive );
                }
            }
            // IMPORTANTE: não desabilitar mm.gameObject aqui.
            // Queremos esconder somente o HUD pequeno, não o mapa grande (tecla M).
        }

        private static void ResolveSmallRoot( Minimap mm ) {
            try {
                // Tenta os campos comuns via reflection
                if (_fiSmallRoot == null) {
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                    _fiSmallRoot =
                           typeof( Minimap ).GetField( "m_smallRoot", flags )
                        ?? typeof( Minimap ).GetField( "_smallRoot", flags );
                }

                GameObject resolved = null;

                if (_fiSmallRoot != null) {
                    var obj = _fiSmallRoot.GetValue( mm );
                    if (obj is GameObject g)
                        resolved = g;
                    else if (obj is Transform t)
                        resolved = t?.gameObject;
                    else if (obj is RectTransform rt)
                        resolved = rt?.gameObject;
                }

                // Se o campo não existe nesta versão, tenta heurísticas leves (opcional)
                if (resolved == null) {
                    // Procura por um filho típico do HUD do minimapa pequeno
                    // (evita custos altos: só quando precisa e a cada ResolveIntervalFrames)
                    var hud = mm.gameObject;
                    var trs = hud.GetComponentsInChildren<RectTransform>( true );
                    foreach (var tr in trs) {
                        // nomes comuns em builds do Valheim para o mini minimapa
                        string n = tr.name.ToLowerInvariant();
                        if (n.Contains( "minimap_small" ) || n == "small" || n.Contains( "minimaphud" )) {
                            resolved = tr.gameObject;
                            break;
                        }
                    }
                }

                _smallRootGO = resolved; // pode ficar null; Tick tratará no próximo ciclo
            } catch {
                _smallRootGO = null;
            }
        }
    }
}