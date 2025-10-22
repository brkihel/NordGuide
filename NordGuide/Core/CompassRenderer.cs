using System.Reflection;
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
        private const float fadeSpeed = 2.5f;
        private const float headingLerpSpeed = 2.5f;

        // locale tracking
        private bool? _lastLocaleIsPt;
        private float _localeCheckTimer;
        private const float LocaleCheckInterval = 2f; // checar a cada 2s

        private void Start()
        {
            bool usePt = IsPortugueseLocale();

            bgTexture = AssetsManager.Tex( "bg-bussola" );
            LoadCardinalTexturesIfNeeded();
            // N / S não mudam
            texN = AssetsManager.Tex( "PinN" );
            texS = AssetsManager.Tex( "PinS" );
            // E / W dependem do idioma.
            // Tentamos a opção principal; se faltar no pacote, caímos no outro nome.
            texE = AssetsManager.Tex( usePt ? "PinL" : "PinE" ) ?? AssetsManager.Tex( usePt ? "PinE" : "PinL" );
            texW = AssetsManager.Tex( usePt ? "PinO" : "PinW" ) ?? AssetsManager.Tex( usePt ? "PinW" : "PinO" );

            NordGuide.Log?.LogInfo( $"[Compass] Locale PT={usePt}. E= {(texE ? "ok" : "missing")}  W= {(texW ? "ok" : "missing")}" );


            float width = Screen.width * 0.32f;

            // Pegue a proporção direto do texture carregado (agora 1400x150)
            float aspect = bgTexture != null ? (float)bgTexture.width / bgTexture.height : (1400f / 200f);
            float height = width / aspect;

            barRect = new Rect( (Screen.width - width) / 2f, 40f, width, height );

            // Log defensivo
            if (!bgTexture)
                NordGuide.Log?.LogWarning( "[Compass] bgTexture não carregada" );
            if (!texN)
                NordGuide.Log?.LogWarning( "[Compass] texN não carregada" );
            if (!texE)
                NordGuide.Log?.LogWarning( "[Compass] texE não carregada" );
            if (!texS)
                NordGuide.Log?.LogWarning( "[Compass] texS não carregada" );
            if (!texW)
                NordGuide.Log?.LogWarning( "[Compass] texW não carregada" );
        }

        private void Update() {
            MinimapVisibility.Tick();
            _localeCheckTimer += Time.deltaTime;
            if (_localeCheckTimer >= LocaleCheckInterval) {
                _localeCheckTimer = 0f;
                LoadCardinalTexturesIfNeeded();
            }
        }

        private void OnDestroy() {
            // limpa refs para evitar sobreposição ao voltar de menu
            texN = texE = texS = texW = null;
            bgTexture = null;
            _lastLocaleIsPt = null;

            // (se tiver hooks em SceneManager, solte-os aqui também)
        }

        private void OnGUI() {
            if (!ConfigManager.EnableCompass.Value)
                return;
            if (Player.m_localPlayer == null || Camera.main == null)
                return;

            bool uiVisible = InventoryGui.IsVisible() || Minimap.IsOpen() || Menu.IsVisible() || TextInput.IsVisible();
            float targetAlpha = uiVisible ? 0f : 1f;
            uiAlpha = Mathf.Lerp( uiAlpha, targetAlpha, Time.deltaTime * fadeSpeed );
            if (uiAlpha < 0.01f)
                return;

            float targetHeading = Camera.main.transform.eulerAngles.y;
            displayedHeading = Mathf.LerpAngle( displayedHeading, targetHeading, Time.deltaTime * headingLerpSpeed );

            // BAR
            GUI.color = new Color( 1f, 1f, 1f, uiAlpha );
            GUI.DrawTexture( barRect, bgTexture, ScaleMode.StretchToFill, true );

            // CARDINALS
            GUI.color = new Color( 1f, 1f, 1f, uiAlpha );
            UI.CompassCardinals.Draw( texN, texE, texS, texW, barRect, displayedHeading );

            // POIS
            GUI.color = new Color( 1f, 1f, 1f, uiAlpha );
            UI.CompassPOIs.Draw( barRect, displayedHeading, uiAlpha );
        }


        private static void DrawSoftShadowUnderBar( Rect barRect, float uiAlpha ) {
            // === AJUSTE AQUI ===
            const float offsetY = 12f;  // DISTÂNCIA: começa 12 px abaixo da barra
            const float expandX = 20f;  // EXPANSÃO LATERAL: 20 px para cada lado
            const float maxSpread = 40f;  // TAMANHO (altura do blur): 40 px pra baixo
            const float opacity = 0.70f; // escuridão global (0–1)
            const int passes = 12;    // suavidade (12–26 é bom)
            const float falloff = 2.2f;  // curva: maior = miolo mais escuro/borda mais suave
                                         // ====================

            Color prev = GUI.color;

            for (int i = 0; i < passes; i++) {
                // t: 0 perto (miolo), 1 longe (bordas)
                float t = (i + 1f) / passes;
                float a = opacity * Mathf.Pow( 1f - t, falloff );

                // abertura horizontal e vertical progressivas
                float ex = expandX * t;
                float ey = maxSpread * t;

                // retângulo cresce e fica um pouco abaixo da barra
                var r = new Rect(
                    barRect.x - ex,
                    barRect.y + offsetY,
                    barRect.width + 2f * ex,
                    1f + ey               // 1px + spread acumulado
                );

                GUI.color = new Color( 0f, 0f, 0f, a * uiAlpha );
                GUI.DrawTexture( r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false );
            }

            GUI.color = prev;
        }

        private static Rect Inflate( Rect r, float px ) {
            return new Rect( r.x - px, r.y - px, r.width + (px * 2f), r.height + (px * 2f) );
        }

        private static bool IsPortugueseLocale() {
            // 1) preferir a configuração que o Valheim salva
            try {
                string lang = PlayerPrefs.GetString( "language", string.Empty );
                if (!string.IsNullOrEmpty( lang )) {
                    lang = lang.ToLowerInvariant();
                    if (lang.StartsWith( "pt" ) || lang.Contains( "portugu" ))
                        return true;   // "pt", "pt-br", "Portuguese", etc.
                    return false;
                }
            } catch { /* ignore */ }

            // 2) fallback: idioma do sistema do Unity
            return Application.systemLanguage == SystemLanguage.Portuguese;
        }

        private void LoadCardinalTexturesIfNeeded() {
            bool usePt = IsPortugueseLocale();

            // evita recarregar se não mudou
            if (_lastLocaleIsPt.HasValue && _lastLocaleIsPt.Value == usePt)
                return;

            _lastLocaleIsPt = usePt;

            // N / S não mudam
            texN = AssetsManager.Tex( "PinN" );
            texS = AssetsManager.Tex( "PinS" );

            // E / W dependem do idioma; com fallback para o outro nome
            texE = AssetsManager.Tex( usePt ? "PinL" : "PinE" ) ?? AssetsManager.Tex( usePt ? "PinE" : "PinL" );
            texW = AssetsManager.Tex( usePt ? "PinO" : "PinW" ) ?? AssetsManager.Tex( usePt ? "PinW" : "PinO" );

            NordGuide.Log?.LogInfo( $"[Compass] Locale PT={usePt}. E={(texE ? "ok" : "missing")} W={(texW ? "ok" : "missing")}" );
        }
    }
}
