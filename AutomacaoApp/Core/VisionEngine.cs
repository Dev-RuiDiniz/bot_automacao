using System;
using System.Drawing; // Para o Point e Bitmap do Windows
using OpenCvSharp;
using OpenCvSharp.Extensions; // Agora vai funcionar após o comando dotnet add

namespace AutomacaoApp.Core
{
    public class VisionEngine
    {
        private readonly double _threshold;

        public VisionEngine(double threshold = 0.8)
        {
            _threshold = threshold;
        }

        // Especificamos System.Drawing.Point para evitar ambiguidade com OpenCvSharp.Point
        public System.Drawing.Point? FindElement(Bitmap screenSource, Bitmap template)
        {
            // O ToMat() vive dentro de OpenCvSharp.Extensions
            using var matSource = BitmapConverter.ToMat(screenSource);
            using var matTemplate = BitmapConverter.ToMat(template);
            using var result = new Mat();

            Cv2.MatchTemplate(matSource, matTemplate, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= _threshold)
            {
                // Retorna o centro do objeto encontrado como um ponto do sistema
                return new System.Drawing.Point(
                    maxLoc.X + (matTemplate.Cols / 2), 
                    maxLoc.Y + (matTemplate.Rows / 2)
                );
            }

            return null;
        }
    }
}