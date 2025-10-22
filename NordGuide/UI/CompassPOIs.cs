using System.Collections.Generic;
using UnityEngine;
using NordGuide.Core;

namespace NordGuide.UI
{
    internal static class CompassPOIs
    {
        // limite angular visível da bússola (mesmo do CompassRenderer)
        private const float VisibleSpanDeg = 90f;

        // Tamanho/escala controlados pelo mod (não configuráveis pelo player)
        private const float BasePoiHeight = 28f;    // “tamanho médio” do ícone na barra
        private const float ScaleNearDist = 0f;     // onde a escala atinge o máximo (perto)
        private const float ScaleFarDist = 300f;   // onde a escala atinge o mínimo (longe)
        private const float PoiMinScale = 0.20f;  // quão pequeno pode ficar
        private const float PoiMaxScale = 2.50f;  // quão grande pode ficar

        // O fade por distância começa um pouco antes de sumir por completo
        private const float FadeStartFraction = 0.75f; // 75% da distância de desaparecer

        // Ping pulsando (amplitude e velocidade)
        private const float PingPulseAmplitude = 0.15f; // 15%
        private const float PingPulseSpeed = 6.0f;  // rad/s
        // cache para classificar "é shout do próprio jogador?" apenas uma vez
        private static readonly Dictionary<Minimap.PinData, bool> _localShoutCache = new();
        // prune periódico (para não crescer)
        private static int _lastPruneFrame;

        public static void Draw( Rect barRect, float heading, float uiAlpha ) {
            if (!PinsProvider.TryGetPins( out List<Minimap.PinData> pins ))
                return;
            PruneLocalShoutCache( pins );
            Vector3 playerPos = Player.m_localPlayer.transform.position;

            float halfSpan = VisibleSpanDeg * 0.5f;
            float midX = barRect.x + barRect.width * 0.5f;
            float pxPerDeg = barRect.width / VisibleSpanDeg;

            foreach (var pin in pins) {
                if (pin == null || pin.m_icon == null)
                    continue;

                // --- 0) Ocultar o SHOUT do PRÓPRIO player ---
                if (IsLocalPlayersShout( pin, playerPos ))
                    continue;

                // --- 1) Ângulo relativo do pin ---
                Vector3 dir = pin.m_pos - playerPos;
                float pinYaw = Mathf.Atan2( dir.x, dir.z ) * Mathf.Rad2Deg;
                float diff = Mathf.DeltaAngle( heading, pinYaw );
                float adiff = Mathf.Abs( diff );
                if (adiff > halfSpan)
                    continue;

                // --- 2) Posição X na barra ---
                float x = midX + diff * pxPerDeg;

                // ====== BLOCO 3/4 SUBSTITUÍDO AQUI ======

                // --- 3) Escala por distância (definida PELO MOD) ---
                float dist = dir.magnitude;

                // perto -> PoiMaxScale | longe -> PoiMinScale
                float scaleT = Mathf.InverseLerp( ScaleNearDist, ScaleFarDist, dist );
                float scale = Mathf.Lerp( PoiMaxScale, PoiMinScale, scaleT );

                // Fade por distância: só o fim é configurável via .cfg
                float fadeStart = Core.ConfigManager.PoiDisappearDistance.Value * FadeStartFraction; // ex.: 75% da distância final
                float fadeEnd = Core.ConfigManager.PoiDisappearDistance.Value;

                // antes de fadeStart = 1 | entre = 1→0 | depois de fadeEnd = 0
                float fadeDist = 1f - Mathf.InverseLerp( fadeStart, fadeEnd, dist );
                fadeDist = Mathf.Clamp01( fadeDist );

                // --- 4) Fade nas bordas + alpha global (uiAlpha) ---
                float edgeFade = Mathf.InverseLerp( halfSpan, halfSpan * 0.6f, adiff );
                float alpha = fadeDist * edgeFade * uiAlpha;
                if (alpha <= 0.01f)
                    continue;

                // ==========================================

                // === Sprite do minimapa ===
                var sprite = pin.m_icon;
                var tex = sprite.texture;
                if (tex == null)
                    continue;

                // UVs oficiais (tratam trim/packing/rotação)
                Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV( sprite ); // (uMin, vMin, uMax, vMax)
                Rect uv = new Rect( outer.x, outer.y, outer.z - outer.x, outer.w - outer.y );

                // Aspecto verdadeiro do recorte em pixels
                Rect tr = sprite.textureRect;
                float ar = tr.width / tr.height;

                // Ping (ex.: "mapicon_ping") pulsa como no minimapa
                bool isPing = sprite.name.IndexOf( "ping", System.StringComparison.OrdinalIgnoreCase ) >= 0;
                if (isPing) {
                    scale *= 1f + PingPulseAmplitude * Mathf.Sin( Time.time * PingPulseSpeed );
                }

                // Retângulo destino preservando aspecto
                float baseH = BasePoiHeight * scale;   // << em vez de 52f * scale
                float drawH = baseH;
                float drawW = baseH * ar;

                float drawX = x - (drawW * 0.5f);
                float drawY = barRect.y + (barRect.height - drawH) * 0.5f;
                Rect dst = new Rect( drawX, drawY, drawW, drawH );

                GUI.color = new Color( 1f, 1f, 1f, alpha );
                GUI.DrawTextureWithTexCoords( dst, tex, uv );
            }
        }

        /// <summary>
        /// Tenta identificar o "shout" (exclamação/alerta) feito pelo PRÓPRIO jogador,
        /// para não desenhar na bússola. Mantém shouts/pings de terceiros.
        /// Estrutura defensiva: usa vários sinais e cai em fallback.
        /// </summary>
        private static bool IsLocalPlayersShout( Minimap.PinData pin, Vector3 playerPos ) {
            if (pin == null || pin.m_icon == null)
                return false;

            // 1) Só processa se o sprite "parecer" shout
            string iconName = pin.m_icon.name ?? string.Empty;
            bool looksLikeShout =
                iconName.IndexOf( "shout", System.StringComparison.OrdinalIgnoreCase ) >= 0 ||
                iconName.IndexOf( "exclam", System.StringComparison.OrdinalIgnoreCase ) >= 0;
            if (!looksLikeShout)
                return false;

            // 2) Se já classificamos esse pin antes, usa o cache
            if (_localShoutCache.TryGetValue( pin, out bool cachedIsMine ))
                return cachedIsMine;

            // 3) Tenta pegar o ID do dono (quando o jogo/mod fornece)
            try {
                var t = pin.GetType();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var fi =
                    t.GetField( "m_ownerID", flags ) ??
                    t.GetField( "m_owner", flags ) ??
                    t.GetField( "m_authorID", flags );

                if (fi != null && Player.m_localPlayer != null) {
                    long localId = Player.m_localPlayer.GetPlayerID();
                    object v = fi.GetValue( pin );
                    if (v is long l) {
                        bool mine = (l == localId);
                        _localShoutCache[pin] = mine;
                        return mine;
                    }
                    if (v is int i) {
                        bool mine = (i == (int)localId);
                        _localShoutCache[pin] = mine;
                        return mine;
                    }
                }
            } catch { /* best-effort */ }

            // 4) Fallback: classifica UMA VEZ por proximidade no momento da 1ª observação
            //    (se nasceu praticamente "em cima" de mim, considero meu)
            bool nearNow = (pin.m_pos - playerPos).sqrMagnitude <= (3f * 3f);
            _localShoutCache[pin] = nearNow;
            return nearNow;
        }

        private static void PruneLocalShoutCache( List<Minimap.PinData> pins ) {
            // roda no máx. a cada ~120 frames
            if (Time.frameCount - _lastPruneFrame < 120)
                return;
            _lastPruneFrame = Time.frameCount;

            // remove do cache o que não existe mais
            var live = new HashSet<Minimap.PinData>( pins );
            var toRemove = new List<Minimap.PinData>();
            foreach (var kv in _localShoutCache)
                if (!live.Contains( kv.Key ))
                    toRemove.Add( kv.Key );
            foreach (var p in toRemove)
                _localShoutCache.Remove( p );
        }


    }
}
