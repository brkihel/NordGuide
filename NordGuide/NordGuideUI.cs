using UnityEngine;

namespace NordGuide
{
    public class NordGuideUI : MonoBehaviour
    {
        private Rect barRect;
        private GUIStyle textStyle;
        private bool isReady = false; // garante que só desenha quando o jogo estiver pronto

        private void Start()
        {
            // Define posição e tamanho da barra
            float width = Screen.width * 0.6f;
            float height = 30f;
            float x = (Screen.width - width) / 2;
            float y = 15f;

            barRect = new Rect(x, y, width, height);

            // Espera o mundo carregar antes de exibir
            InvokeRepeating(nameof(CheckWorldReady), 1f, 1f);
        }

        private void CheckWorldReady()
        {
            // Só desenha se o player e o Hud existirem
            if (Player.m_localPlayer != null && Hud.instance != null)
            {
                isReady = true;
                CancelInvoke(nameof(CheckWorldReady));
            }
        }

        private void OnGUI()
        {
            // Se o mundo ainda não estiver pronto, não desenha nada
            if (!isReady)
                return;

            // Garante que o estilo foi criado dentro do contexto gráfico
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    normal = { textColor = Color.white }
                };
            }

            // Fundo da barra translúcido
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.Box(barRect, GUIContent.none);

            // Obtém direção da câmera
            string direction = "?";
            if (Player.m_localPlayer != null && Camera.main != null)
            {
                float yRot = Camera.main.transform.eulerAngles.y;
                direction = GetDirection(yRot);
            }

            // Texto central dinâmico
            GUI.color = Color.white;
            GUI.Label(barRect, $"NORDGUIDE — FACING: {direction}", textStyle);
        }

        private string GetDirection(float yRotation)
        {
            // Normaliza o ângulo (0 a 360)
            yRotation %= 360f;
            if (yRotation < 0) yRotation += 360f;

            // Define as direções com base em faixas de ângulo
            if (yRotation >= 337.5f || yRotation < 22.5f)
                return "N";
            if (yRotation >= 22.5f && yRotation < 67.5f)
                return "NE";
            if (yRotation >= 67.5f && yRotation < 112.5f)
                return "E";
            if (yRotation >= 112.5f && yRotation < 157.5f)
                return "SE";
            if (yRotation >= 157.5f && yRotation < 202.5f)
                return "S";
            if (yRotation >= 202.5f && yRotation < 247.5f)
                return "SW";
            if (yRotation >= 247.5f && yRotation < 292.5f)
                return "W";
            if (yRotation >= 292.5f && yRotation < 337.5f)
                return "NW";

            return "?";
        }


    }
}
