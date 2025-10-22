using UnityEngine;

namespace NordGuide.Core
{
    public class CompassRenderer : MonoBehaviour
    {
        private Texture2D bgTexture;
        private Texture2D texN, texE, texS, texW;

        private Rect barRect;
        private float uiAlpha;
        private float displayedHeading;

        private const float visibleSpanDeg = 90f;
        private const float fadeSpeed = 2.5f;
        private const float headingLerpSpeed = 2.5f;

        private void Start()
        {
            bgTexture = AssetsManager.LoadTexture("bg-bussola_g.png");
            texN = AssetsManager.LoadTexture("PinN.png");
            texE = AssetsManager.LoadTexture("PinE.png");
            texS = AssetsManager.LoadTexture("PinS.png");
            texW = AssetsManager.LoadTexture("PinW.png");

            float width = Screen.width * 0.20f;
            float height = width / (1200f / 90f);
            barRect = new Rect((Screen.width - width) / 2f, 40f, width, height);
        }

        private void Update() {
            MinimapVisibility.Tick();
        }

        private void OnGUI()
        {
            if (!ConfigManager.EnableCompass.Value) return;
            if (Player.m_localPlayer == null || Camera.main == null) return;

            bool uiVisible = InventoryGui.IsVisible() || Minimap.IsOpen() || Menu.IsVisible() || TextInput.IsVisible();
            float targetAlpha = uiVisible ? 0f : 1f;
            uiAlpha = Mathf.Lerp(uiAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            if (uiAlpha < 0.01f) return;

            GUI.color = new Color(1f, 1f, 1f, uiAlpha);
            float targetHeading = Camera.main.transform.eulerAngles.y;
            displayedHeading = Mathf.LerpAngle(displayedHeading, targetHeading, Time.deltaTime * headingLerpSpeed);

            // Fundo
            GUI.DrawTexture(barRect, bgTexture, ScaleMode.StretchToFill, true);

            // Cardeais
            UI.CompassCardinals.Draw(texN, texE, texS, texW, barRect, displayedHeading);

            // POIs
            UI.CompassPOIs.Draw(barRect, displayedHeading, uiAlpha);
        }
    }
}
