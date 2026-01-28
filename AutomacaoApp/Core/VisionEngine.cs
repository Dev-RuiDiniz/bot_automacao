using System;
using System.Drawing;
using System.Runtime.Versioning; // Necessário para o atributo de plataforma
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutomacaoApp.Core
{
    // Informamos ao compilador que esta classe foi feita especificamente para Windows
    [SupportedOSPlatform("windows")]
    public class VisionEngine
    {
        private readonly double _threshold;

        public VisionEngine(double threshold = 0.8)
        {
            _threshold = threshold;
        }

        public System.Drawing.Point? FindElement(Bitmap screenSource, Bitmap template)
        {
            // Agora o aviso CA1416 desaparece pois o método está "protegido" pelo atributo da classe
            using var matSource = BitmapConverter.ToMat(screenSource);
            using var matTemplate = BitmapConverter.ToMat(template);
            using var result = new Mat();

            Cv2.MatchTemplate(matSource, matTemplate, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= _threshold)
            {
                return new System.Drawing.Point(
                    maxLoc.X + (matTemplate.Cols / 2), 
                    maxLoc.Y + (matTemplate.Rows / 2)
                );
            }

            return null;
        }
    }
}