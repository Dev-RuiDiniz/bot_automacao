using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutomacaoApp.Core
{
    /// <summary>
    /// Motor de Visão Computacional baseado em OpenCV.
    /// Responsável pela detecção inteligente de elementos e cálculo de coordenadas humanizadas.
    /// </summary>
    public class VisionEngine
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Busca um elemento na tela com ajuste dinâmico de precisão e retorno centralizado.
        /// </summary>
        /// <param name="screen">Captura atual da tela (Desktop/Emulador).</param>
        /// <param name="template">Imagem do asset (botão/ícone) a ser localizado.</param>
        /// <param name="initialThreshold">Precisão desejada (Padrão 80%).</param>
        /// <returns>Coordenadas centrais com offset aleatório ou null se não encontrado.</returns>
        public System.Drawing.Point? FindElement(Bitmap screen, Bitmap template, double initialThreshold = 0.8)
        {
            try
            {
                // Conversão para formato Mat do OpenCV para processamento de alto desempenho
                using var matScreen = BitmapConverter.ToMat(screen);
                using var matTemplate = BitmapConverter.ToMat(template);
                using var res = new Mat();

                // Executa a correlação cruzada normalizada
                Cv2.MatchTemplate(matScreen, matTemplate, res, TemplateMatchModes.CCoeffNormed);
                
                Cv2.MinMaxLoc(res, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                // 1ª Tentativa: Threshold rigoroso para evitar cliques em elementos errados
                if (maxVal >= initialThreshold)
                {
                    return PrepareFinalPoint(maxLoc, template.Width, template.Height);
                }

                // 2ª Tentativa (Dinâmica): Fallback de -20% para lidar com variações de brilho/anti-aliasing
                double fallbackThreshold = initialThreshold - 0.20;
                if (maxVal >= fallbackThreshold)
                {
                    // Nota técnica: MaxVal indica quão similar é a imagem (ex: 0.65 = 65% de similaridade)
                    return PrepareFinalPoint(maxLoc, template.Width, template.Height);
                }

                return null; // Elemento não detectado na varredura
            }
            catch (Exception)
            {
                // Em caso de erro de memória ou formato, retorna null para o orquestrador tratar
                return null;
            }
        }

        /// <summary>
        /// Calcula o centro do elemento e aplica um desvio aleatório (Antropomorfismo).
        /// </summary>
        private System.Drawing.Point PrepareFinalPoint(OpenCvSharp.Point location, int width, int height)
        {
            // Calcula o centro exato do asset
            int centerX = location.X + (width / 2);
            int centerY = location.Y + (height / 2);

            // Aplica Randomização de Cliques (Offset de +-3 pixels)
            // Impede que o bot clique repetidamente no mesmo pixel exato
            int offsetX = _random.Next(-3, 4); 
            int offsetY = _random.Next(-3, 4);

            return new System.Drawing.Point(centerX + offsetX, centerY + offsetY);
        }

        /// <summary>
        /// Método estático para aplicar offset a qualquer ponto se necessário fora do motor principal.
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