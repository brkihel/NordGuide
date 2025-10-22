using BepInEx;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

namespace NordGuide
{
    public class CompassRenderer : MonoBehaviour
    {
        private Camera mainCamera;

        private Texture2D bgTexture;
        private Texture2D texN, texS, texE, texW;

        private Rect barRect;
        private bool initialized = false;

        private float iconSize = 52f;
        private float barAspectRatio = 1200f / 90f;
        private float fadeEdge = 0.60f;

        // 🔸 Controle do fade global
        private float uiAlpha = 0f;
        private float fadeSpeed = 2.5f;
        private bool worldEntered = false;

        // --- Configuração de visualização ---
        private float displayedHeading;               // heading suavizado exibido
        private const float visibleSpanDeg = 90f;     // campo de visão da barra (±45°)
        private const float headingLerpSpeed = 2.5f;  // suavidade do heading

        private void Start()
        {
            InvokeRepeating(nameof(TryInitialize), 1f, 1f);
        }

        private void TryInitialize()
        {
            if (Player.m_localPlayer == null)
                return;

            if (bgTexture == null)
            {
                Debug.Log("[NordGuide] Tentando carregar texturas...");
                bgTexture = LoadTextureFromFile("bg-bussola_g.png");
                texN = LoadTextureFromFile("PinN_g.png");
                texS = LoadTextureFromFile("PinS_g.png");
                texE = LoadTextureFromFile("PinE_g.png");
                texW = LoadTextureFromFile("PinW_g.png");
            }

            if (bgTexture != null)
            {
                float targetWidth = Screen.width * 0.20f;
                float targetHeight = targetWidth / barAspectRatio;
                float x = (Screen.width - targetWidth) / 2f;
                float y = 40f;

                barRect = new Rect(x, y, targetWidth, targetHeight);

                initialized = true;
                CancelInvoke(nameof(TryInitialize));

                Debug.Log("[NordGuide] CompassRenderer inicializado!");
            }
        }

        private void OnGUI()
        {
            if (!initialized || Player.m_localPlayer == null)
                return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (bgTexture == null)
                return;

            // Esconde se alguma UI importante estiver aberta
            bool uiVisible = IsAnyGameUIVisible();

            // Primeiro spawn no mundo → liga fade in
            if (!worldEntered && !uiVisible)
                worldEntered = true;

            // Fade global
            float targetAlpha = (!uiVisible && worldEntered) ? 1f : 0f;
            uiAlpha = Mathf.Lerp(uiAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            if (uiAlpha <= 0.01f)
                return;

            GUI.depth = -1000;
            Color prev = GUI.color;
            GUI.color = new Color(prev.r, prev.g, prev.b, uiAlpha);

            // --- Heading suavizado ---
            float targetHeading = mainCamera.transform.eulerAngles.y;
            displayedHeading = Mathf.LerpAngle(displayedHeading, targetHeading, Time.deltaTime * headingLerpSpeed);

            // 🔹 Desenha sombra leve da barra
            Rect shadowRect = new Rect(barRect.x + 3f, barRect.y + 3f, barRect.width, barRect.height);
            GUI.color = new Color(0, 0, 0, uiAlpha * 0.45f);
            GUI.DrawTexture(shadowRect, bgTexture, ScaleMode.StretchToFill, true);
            GUI.color = prev;

            // 🔹 Barra principal
            GUI.DrawTexture(barRect, bgTexture, ScaleMode.StretchToFill, true);

            float midX = barRect.x + barRect.width / 2f;
            float pxPerDeg = barRect.width / visibleSpanDeg;

            // 🔹 Pontos cardeais
            DrawCardinalIcon(texN, 0f, midX, pxPerDeg);
            DrawCardinalIcon(texE, 90f, midX, pxPerDeg);
            DrawCardinalIcon(texS, 180f, midX, pxPerDeg);
            DrawCardinalIcon(texW, -90f, midX, pxPerDeg);

            // ---------- POIs VANILLA (usando provider resiliente) ----------
            if (PinsProvider.TryGetPins(out List<Minimap.PinData> pins))
            {
                Vector3 playerPos = Player.m_localPlayer.transform.position;

                const float maxRange = 500f;   // distância máxima para mostrar
                const float minScale = 0.5f;   // escala quando está longe
                const float maxScale = 1.3f;   // escala quando está bem perto
                float halfSpan = visibleSpanDeg * 0.5f;

                foreach (var pin in pins)
                {
                    if (pin == null || pin.m_icon == null)
                        continue;

                    // distância
                    Vector3 dir = pin.m_pos - playerPos;
                    float dist = dir.magnitude;
                    if (dist > maxRange)
                        continue;

                    // ângulo do pin relativo à câmera
                    float pinYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    float diff = Mathf.DeltaAngle(displayedHeading, pinYaw);
                    if (Mathf.Abs(diff) > halfSpan)
                        continue;

                    // posição X na barra
                    float x = midX + diff * pxPerDeg;

                    // escala e opacidade por distância
                    float t = Mathf.InverseLerp(maxRange, 0f, dist);      // 0 = longe, 1 = perto
                    float size = iconSize * Mathf.Lerp(minScale, maxScale, t);
                    float alpha = Mathf.Lerp(0.3f, 1f, t);

                    var prevCol = GUI.color;
                    GUI.color = new Color(prevCol.r, prevCol.g, prevCol.b, prevCol.a * alpha);

                    var tex = pin.m_icon.texture;
                    if (tex != null)
                    {
                        Rect r = new Rect(
                            x - size / 2f,
                            barRect.y + (barRect.height - size) / 2f,
                            size, size
                        );
                        GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
                    }

                    GUI.color = prevCol;
                }
            }
            // ----------------------------------------------------------------

            GUI.color = prev;
        }

        private void DrawCardinalIcon(Texture2D icon, float cardinalDeg, float midX, float pxPerDeg)
        {
            if (icon == null)
                return;

            float diff = Mathf.DeltaAngle(displayedHeading, cardinalDeg);
            float visibleRange = 45f;
            if (Mathf.Abs(diff) > visibleRange)
                return;

            float normalized = diff / visibleRange;
            float x = midX + normalized * (barRect.width / 2.2f);

            float distNorm = Mathf.Abs(diff / visibleRange);
            float sizeScale = Mathf.Lerp(1.15f, 0.90f, distNorm);
            float size = iconSize * sizeScale;

            float y = barRect.y + (barRect.height - size) / 2f;
            Rect rect = new Rect(x - size / 2f, y, size, size);

            float fadeStart = visibleRange * 0.45f;
            float alpha = 1f;
            if (Mathf.Abs(diff) > fadeStart)
                alpha = Mathf.InverseLerp(visibleRange, fadeStart, Mathf.Abs(diff));

            Color prev = GUI.color;
            GUI.color = new Color(prev.r, prev.g, prev.b, prev.a * alpha);
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = prev;
        }

        private bool IsAnyGameUIVisible()
        {
            if (InventoryGui.IsVisible()) return true;
            if (Minimap.IsOpen()) return true;
            if (Menu.IsVisible() || TextInput.IsVisible()) return true;
            return false;
        }

        // ==== Carregamento de PNG SEM referenciar ImageConversionModule ====
        private Texture2D LoadTextureFromFile(string fileName)
        {
            try
            {
                string dirPath = Path.Combine(Paths.PluginPath, "NordGuide", "Assets").Replace("\\", "/");
                string fullPath = Path.Combine(dirPath, fileName);

                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"[NordGuide] Arquivo de textura não encontrado: {fullPath}");
                    return null;
                }

                byte[] data = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                var imageConvType = Type.GetType("UnityEngine.ImageConversion, UnityEngine");
                var loadImageMethod = imageConvType?.GetMethod(
                    "LoadImage",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                    null
                );

                if (loadImageMethod == null)
                {
                    Debug.LogError("[NordGuide] UnityEngine.ImageConversion.LoadImage não encontrado via reflection.");
                    return null;
                }

                bool ok = (bool)loadImageMethod.Invoke(null, new object[] { tex, data, false });
                if (!ok)
                {
                    Debug.LogWarning($"[NordGuide] Falha ao decodificar {fileName}.");
                    return null;
                }

                Debug.Log($"[NordGuide] Textura {fileName} carregada ({data.Length} bytes).");
                return tex;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NordGuide] Falha ao carregar textura {fileName}: {ex.Message}");
                return null;
            }
        }
    }
}
