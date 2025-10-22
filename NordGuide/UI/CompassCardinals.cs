using UnityEngine;

namespace NordGuide.UI
{
    internal static class CompassCardinals
    {
        public static void Draw(Texture2D texN, Texture2D texE, Texture2D texS, Texture2D texW, Rect barRect, float heading)
        {
            DrawCardinal(texN, 0f, barRect, heading);
            DrawCardinal(texE, 90f, barRect, heading);
            DrawCardinal(texS, 180f, barRect, heading);
            DrawCardinal(texW, -90f, barRect, heading);
        }

        private static void DrawCardinal(Texture2D icon, float angle, Rect barRect, float heading)
        {
            if (icon == null) return;

            float diff = Mathf.DeltaAngle(heading, angle);
            if (Mathf.Abs(diff) > 45f) return;

            float t = diff / 45f;
            float x = barRect.x + (barRect.width / 2f) + t * (barRect.width / 2.2f);
            float size = Mathf.Lerp(48f, 42f, Mathf.Abs(t));
            float alpha = Mathf.InverseLerp(45f, 20f, Mathf.Abs(diff));

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(x - size / 2f, barRect.y + (barRect.height - size) / 2f, size, size), icon);
        }
    }
}