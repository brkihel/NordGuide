using UnityEngine;

namespace NordGuide.UI {
    internal static class CompassCardinals {
        // Controladores de onde o fade dos POIs acontece na barra
        private const float VisibleSpanDeg = 60f;
        private const float EdgeFadeInnerFrac = 0.20f;
        private const float UsableSpanFrac = 0.82f;

        // Tamanho dos cardeais (fração da ALTURA da barra) e ajuste vertical
        private const float CardinalHeightFactor = 0.60f; // mude aqui p/ aumentar/diminuir
        private const float CardinalYOffsetFrac = 0.00f; // leve ajuste vertical
        private const float UsableHeightFrac = 0.62f;

        public static void Draw( Texture2D texN, Texture2D texE, Texture2D texS, Texture2D texW, Rect barRect, float heading ) {
            DrawCardinal( texN, 0f, barRect, heading );
            DrawCardinal( texE, 90f, barRect, heading );
            DrawCardinal( texS, 180f, barRect, heading );
            DrawCardinal( texW, -90f, barRect, heading );
        }

        private static void DrawCardinal( Texture2D icon, float angle, Rect barRect, float heading ) {
            if (icon == null)
                return;

            float halfSpan = VisibleSpanDeg * 0.5f;

            // ângulo relativo e descarte fora do campo
            float diff = Mathf.DeltaAngle( heading, angle );
            float adiff = Mathf.Abs( diff );
            if (adiff > halfSpan)
                return;

            float midX = barRect.x + barRect.width * 0.5f;
            float usableW = barRect.width * UsableSpanFrac;
            float pxPerDeg = usableW / VisibleSpanDeg;
            float x = midX + diff * pxPerDeg;

            // fade nas bordas
            float alpha = Mathf.InverseLerp( halfSpan, halfSpan * EdgeFadeInnerFrac, adiff );

            // retângulo destino proporcional à ALTURA da barra
            float ar = (float)icon.width / icon.height;
            float h = barRect.height * UsableHeightFrac * CardinalHeightFactor;
            float w = h * ar;
            float yOffset = barRect.height * CardinalYOffsetFrac;

            Rect dst = new Rect(
                x - (w * 0.5f),
                barRect.y + (barRect.height - h) * 0.5f + yOffset,
                w, h
            );

            // desenha o cardinal
            var prev = GUI.color;
            GUI.color = new Color( 1f, 1f, 1f, alpha );
            GUI.DrawTexture( dst, icon, ScaleMode.StretchToFill, true );
            GUI.color = prev;
        }
    }
}
