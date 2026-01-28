using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutomacaoApp.Core
{
    /// <summary>
    /// Motor de Visão Computacional de Alto Desempenho.
    /// Especializado em detecção por janela específica para suportar múltiplas instâncias simultâneas.
    /// </summary>
    public class VisionEngine
    {
        private static readonly Random _random = new Random();

        // Importações Win32 para capturar apenas a janela do emulador (Isolamento de Processo)
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        /// <summary>
        /// Captura apenas os pixels da janela do emulador baseado no seu Handle (hWnd).
        /// Essencial para rodar 15+ instâncias sem que uma sobreponha a captura da outra.
        /// </summary>
        public Bitmap CaptureProcessWindow(IntPtr hWnd)
        {
            try
            {
                GetWindowRect(hWnd, out RECT rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                // Evita erro de criação de bitmap se a janela estiver minimizada
                if (width <= 0 || height <= 0) return new Bitmap(1, 1);

                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Copia apenas a área definida pelo retângulo da janela no Windows
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height));
                }
                return bmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VISION ERROR] Falha na captura de janela: {ex.Message}");
                return new Bitmap(1, 1);
            }
        }

        /// <summary>
        /// Localiza um asset visual dentro de um bitmap usando Template Matching.
        /// </summary>
        /// <param name="screen">O print da janela do emulador.</param>
        /// <param name="template">A imagem do botão/elemento procurado.</param>
        /// <param name="threshold">Precisão (0.0 a 1.0). Sugerido: 0.8.</param>
        public System.Drawing.Point? FindElement(Bitmap screen, Bitmap template, double threshold = 0.8)
        {
            try
            {
                // Conversão de Bitmap para Mat (Formato nativo do OpenCV)
                // Usamos 'using' para garantir que a memória da GPU/CPU seja liberada imediatamente
                using var matScreen = screen.ToMat();
                using var matTemplate = template.ToMat();
                using var res = new Mat();

                // Executa a busca matemática de padrões (Correlação Cruzada)
                Cv2.MatchTemplate(matScreen, matTemplate, res, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(res, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                // Verificação de Threshold com Fallback automático de 10% para variações de renderização
                if (maxVal >= threshold || maxVal >= (threshold - 0.1))
                {
                    return PrepareFinalPoint(maxLoc, template.Width, template.Height);
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Calcula o centro do elemento e aplica offset aleatório para evitar detecção de bot.
        /// </summary>
        private System.Drawing.Point PrepareFinalPoint(OpenCvSharp.Point location, int width, int height)
        {
            // Define o centro do elemento encontrado
            int centerX = location.X + (width / 2);
            int centerY = location.Y + (height / 2);

            // Aplica variação humanizada de +-3 pixels para que cliques nunca sejam idênticos
            int offsetX = _random.Next(-3, 4);
            int offsetY = _random.Next(-3, 4);

            return new System.Drawing.Point(centerX + offsetX, centerY + offsetY);
        }

        /// <summary>
        /// Helper estático para randomizar coordenadas brutas.
        /// </summary>
        public static System.Drawing.Point ApplyHumanOffset(System.Drawing.Point point, int range = 3)
        {
            return new System.Drawing.Point(
                point.X + _random.Next(-range, range + 1),
                point.Y + _random.Next(-range, range + 1)
            );
        }
    }
}