using System.Collections.Generic;
using UnityEngine;
using NordGuide.Core;

namespace NordGuide.UI {
    internal static class CompassPOIs {
        // ----- CONTROLES DE VISÃO NA BARRA -----
        private const float VisibleSpanDeg = 60f;   // campo de visão total da barra (±30°)
        private const float EdgeFadeInnerFrac = 0.20f; // começa a sumir antes de encostar
        private const float UsableSpanFrac = 0.82f; // % da largura efetivamente usada

        // ----- TAMANHO RELATIVO -----
        private const float PoiHeightFactor = 0.50f; // % da altura "útil" ocupada pelo POI
        private const float PoiYOffsetFrac = 0.02f; // leve ajuste vertical
        private const float UsableHeightFrac = 0.62f; // % da altura da barra usada pros POIs

        // ----- ESCALA POR DISTÂNCIA -----
        private const float ScaleNearDist = 0f;
        private const float ScaleFarDist = 350f;
        private const float PoiMinScale = 0.20f;
        private const float PoiMaxScale = 1.40f;

        // ----- FADE POR DISTÂNCIA -----
        private const float FadeStartFraction = 0.75f;

        // ----- PULSO (PING/SHOUT) -----
        private const float PulseAmplitude = 0.15f;
        private const float PulseSpeed = 6.0f;

        // Suavização leve do alpha (corta “cortes secos” quando os mods trocam pins)
        // Guardamos só o último alpha por pin; não enumeramos esse dicionário.
        private static readonly Dictionary<Minimap.PinData, float> s_lastAlpha = new( 256 );
        private const float AlphaSmoothHz = 14f; // maior = responde mais rápido (14~20 fica bom)

        public static void Draw( Rect barRect, float heading, float uiAlpha ) {
            if (!PinsProvider.TryGetPins( out List<Minimap.PinData> pins ))
                return;

            Vector3 playerPos = Player.m_localPlayer.transform.position;

            float halfSpan = VisibleSpanDeg * 0.5f;
            float midX = barRect.x + barRect.width * 0.5f;
            float usableW = barRect.width * UsableSpanFrac;
            float pxPerDeg = usableW / VisibleSpanDeg;

            foreach (var pin in pins) {
                if (pin == null || pin.m_icon == null)
                    continue;

                // --- direção/ângulo relativo ---
                Vector3 dir = pin.m_pos - playerPos;
                float pinYaw = Mathf.Atan2( dir.x, dir.z ) * Mathf.Rad2Deg;
                float diff = Mathf.DeltaAngle( heading, pinYaw );
                float adiff = Mathf.Abs( diff );
                if (adiff > halfSpan)
                    continue;

                // --- posição X na barra ---
                float x = midX + diff * pxPerDeg;

                // --- escala por distância ---
                float dist = dir.magnitude;
                float tDist = Mathf.InverseLerp( ScaleNearDist, ScaleFarDist, dist ); // 0 perto → 1 longe
                float tNear = Mathf.SmoothStep( 0f, 1f, 1f - tDist );                 // curva suave
                float scale = Mathf.Lerp( PoiMinScale, PoiMaxScale, tNear );

                // --- fade por distância (sem alpha global ainda) ---
                float fadeStart = ConfigManager.PoiDisappearDistance.Value * FadeStartFraction;
                float fadeEnd = ConfigManager.PoiDisappearDistance.Value;
                float fadeDist = 1f - Mathf.InverseLerp( fadeStart, fadeEnd, dist );
                fadeDist = Mathf.Clamp01( fadeDist );

                // --- fade nas bordas + alpha global ---
                float edgeFade = Mathf.InverseLerp( halfSpan, halfSpan * EdgeFadeInnerFrac, adiff );
                float baseAlpha = fadeDist * edgeFade * uiAlpha;

                // === sprite / UV / aspecto ===
                var sprite = pin.m_icon;
                var tex = sprite.texture;
                if (tex == null)
                    continue;

                Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV( sprite );
                Rect uv = new Rect( outer.x, outer.y, outer.z - outer.x, outer.w - outer.y );
                Rect tr = sprite.textureRect;
                float ar = tr.width / tr.height;

                // --- pulso (ping/shout) ---
                string iconName = sprite.name ?? string.Empty;
                bool isPing = iconName.IndexOf( "ping", System.StringComparison.OrdinalIgnoreCase ) >= 0;
                bool isShout = iconName.IndexOf( "shout", System.StringComparison.OrdinalIgnoreCase ) >= 0
                            || iconName.IndexOf( "exclam", System.StringComparison.OrdinalIgnoreCase ) >= 0;
                if (isPing || isShout)
                    scale *= 1f + PulseAmplitude * Mathf.Sin( Time.time * PulseSpeed );

                // --- destino proporcional à barra ---
                float hUsable = barRect.height * UsableHeightFrac;
                float drawH = (hUsable * PoiHeightFactor) * scale;
                float drawW = drawH * ar;
                float yOffset = barRect.height * PoiYOffsetFrac;

                Rect dst = new Rect(
                    x - (drawW * 0.5f),
                    barRect.y + (barRect.height - drawH) * 0.5f + yOffset,
                    drawW, drawH
                );

                // --- suavização de alpha (low-pass) ---
                float alpha = baseAlpha;
                if (s_lastAlpha.TryGetValue( pin, out float prevA )) {
                    // lerp exponencial estável (frame-rate independente)
                    float k = 1f - Mathf.Exp( -AlphaSmoothHz * Time.deltaTime );
                    alpha = Mathf.Lerp( prevA, baseAlpha, k );
                }
                s_lastAlpha[pin] = alpha;

                if (alpha <= 0.01f)
                    continue;

                GUI.color = new Color( 1f, 1f, 1f, alpha );
                GUI.DrawTextureWithTexCoords( dst, tex, uv );
            }

            // Opcional: limpamos entradas antigas de s_lastAlpha ocasionalmente?
            // Não é necessário para estabilidade; se quiser, podemos varrer a cada X segundos.
        }
    }
}
