using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutomacaoApp.Core
{
    public class VisionEngine
    {
        /// <summary>
        /// Busca um elemento na tela com ajuste dinâmico de precisão.
        /// </summary>
        /// <param name="screen">Captura atual da tela.</param>
        /// <param name="template">Imagem do asset a ser buscado.</param>
        /// <param name="initialThreshold">Precisão inicial (Padrão: 0.8 ou 80%).</param>
        /// <returns>Coordenadas do centro do elemento ou null.</returns>
        public System.Drawing.Point? FindElement(Bitmap screen, Bitmap template, double initialThreshold = 0.8)
        {
            // Converte Bitmaps para Mat (formato OpenCV)
            using var matScreen = BitmapConverter.ToMat(screen);
            using var matTemplate = BitmapConverter.ToMat(template);
            using var res = new Mat();

            // Executa o MatchTemplate
            Cv2.MatchTemplate(matScreen, matTemplate, res, TemplateMatchModes.CCoeffNormed);
            
            double minVal, maxVal;
            OpenCvSharp.Point minLoc, maxLoc;
            Cv2.MinMaxLoc(res, out minVal, out maxVal, out minLoc, out maxLoc);

            // 1ª Tentativa: Threshold rigoroso (evita falsos positivos)
            if (maxVal >= initialThreshold)
            {
                return GetCenterPoint(maxLoc, template.Width, template.Height);
            }

            // 2ª Tentativa (Dinâmica): Se falhou, tentamos com 0.60 (60% de confiança)
            double fallbackThreshold = initialThreshold - 0.20;
            if (maxVal >= fallbackThreshold)
            {
                // Log opcional via injeção de dependência se necessário: 
                // "Elemento encontrado com confiança reduzida: " + maxVal
                return GetCenterPoint(maxLoc, template.Width, template.Height);
            }

            return null; // Elemento realmente não está na tela
        }

        private System.Drawing.Point GetCenterPoint(OpenCvSharp.Point location, int width, int height)
        {
            return new System.Drawing.Point(
                location.X + (width / 2),
                location.Y + (height / 2)
            );
        }
    }
}