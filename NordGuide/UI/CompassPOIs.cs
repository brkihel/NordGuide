using System.Collections.Generic;
using UnityEngine;
using NordGuide.Core;

namespace NordGuide.UI {
    internal static class CompassPOIs {
        // Controladores de onde o fade dos POIs acontece na barra
        private const float VisibleSpanDeg = 60f;
        private const float EdgeFadeInnerFrac = 0.20f;
        private const float UsableSpanFrac = 0.82f;

        // Tamanho relativo dos POIs em relação à ALTURA da barra
        private const float PoiHeightFactor = 0.50f;   // % da altura do barRect ocupada pelo POI
        // Pequeno ajuste vertical para centralizar melhor (IMGUI cresce pra baixo)
        private const float PoiYOffsetFrac = 0.02f;    // 4% da altura da barra para baixo
        private const float UsableHeightFrac = 0.62f;  // 62% da altura da barra

        // Curva de escala por distância (mais sensível que antes)
        private const float ScaleNearDist = 0f;        // onde a escala é máxima (perto)
        private const float ScaleFarDist = 350f;      // onde a escala é mínima (longe)
        private const float PoiMinScale = 0.20f;      // quão pequeno pode ficar
        private const float PoiMaxScale = 1.40f;      // quão grande pode ficar

        // Fade por distância começa um pouco antes do desaparecer total
        private const float FadeStartFraction = 0.75f;

        // Pulso (usado para ping e shout)
        private const float PulseAmplitude = 0.15f;    // 15%
        private const float PulseSpeed = 6.0f;     // rad/s

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

                // 1) direção/ângulo relativo
                Vector3 dir = pin.m_pos - playerPos;
                float pinYaw = Mathf.Atan2( dir.x, dir.z ) * Mathf.Rad2Deg;
                float diff = Mathf.DeltaAngle( heading, pinYaw );
                float adiff = Mathf.Abs( diff );
                if (adiff > halfSpan)
                    continue;

                // 2) posição X na barra
                float x = midX + diff * pxPerDeg;

                // 3) escala dinâmica por distância (mais perceptível)
                float dist = dir.magnitude;
                float tDist = Mathf.InverseLerp( ScaleNearDist, ScaleFarDist, dist ); // 0 perto → 1 longe
                float tNear = 1f - tDist;                                          // 1 perto → 0 longe
                tNear = Mathf.SmoothStep( 0f, 1f, tNear );                     // curva suave
                float scale = Mathf.Lerp( PoiMinScale, PoiMaxScale, tNear );

                // 4) fade por distância (config: fim)
                float fadeStart = ConfigManager.PoiDisappearDistance.Value * FadeStartFraction;
                float fadeEnd = ConfigManager.PoiDisappearDistance.Value;
                float fadeDist = 1f - Mathf.InverseLerp( fadeStart, fadeEnd, dist );
                fadeDist = Mathf.Clamp01( fadeDist );

                // 5) fade nas bordas + alpha global (uiAlpha)
                float edgeFade = Mathf.InverseLerp( halfSpan, halfSpan * EdgeFadeInnerFrac, adiff );
                float alpha = fadeDist * edgeFade * uiAlpha;
                if (alpha <= 0.01f)
                    continue;

                // === sprite do minimapa ===
                var sprite = pin.m_icon;
                var tex = sprite.texture;
                if (tex == null)
                    continue;

                // UVs oficiais (tratam trim/packing/rotação) e aspecto
                Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV( sprite ); // (uMin, vMin, uMax, vMax)
                Rect uv = new Rect( outer.x, outer.y, outer.z - outer.x, outer.w - outer.y );
                Rect tr = sprite.textureRect;
                float ar = tr.width / tr.height;

                // 6) pulsar: ping e shout
                string iconName = sprite.name ?? string.Empty;
                bool isPing = iconName.IndexOf( "ping", System.StringComparison.OrdinalIgnoreCase ) >= 0;
                bool isShout = iconName.IndexOf( "shout", System.StringComparison.OrdinalIgnoreCase ) >= 0
                            || iconName.IndexOf( "exclam", System.StringComparison.OrdinalIgnoreCase ) >= 0;

                if (isPing || isShout) {
                    scale *= 1f + PulseAmplitude * Mathf.Sin( Time.time * PulseSpeed );
                }

                // 7) retângulo destino preservando aspecto (proporcional à barra)
                float baseH = (barRect.height * UsableHeightFrac * PoiHeightFactor) * scale;
                float drawH = baseH;
                float drawW = baseH * ar;

                float yOffset = barRect.height * PoiYOffsetFrac; // centraliza visualmente
                float drawX = x - (drawW * 0.5f);
                float drawY = barRect.y + (barRect.height - drawH) * 0.5f + yOffset;

                Rect dst = new Rect( drawX, drawY, drawW, drawH );

                // 8) ícone (SEM sombra)
                GUI.color = new Color( 1f, 1f, 1f, alpha );
                GUI.DrawTextureWithTexCoords( dst, tex, uv );
            }
        }
    }
}
