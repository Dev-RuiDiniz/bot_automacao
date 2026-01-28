using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
using AutomacaoApp.Core;
using AutomacaoApp.Enums;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class DailySpinService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public DailySpinService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        public void Execute()
        {
            _bot.Log("Iniciando Módulo: DailySpin");

            using var screen = CaptureScreen();

            // 1. Verificar se o popup da roleta está disponível
            if (DetectAndClick(screen, "popup_roleta_disponivel.png", "Aviso de Roleta"))
            {
                Thread.Sleep(2000); // Aguarda transição de tela
                
                // 2. Tentar encontrar o botão de girar
                using var secondScreen = CaptureScreen();
                if (DetectAndClick(secondScreen, "btn_girar.png", "Botão Girar"))
                {
                    _bot.Log("Giro iniciado! Aguardando 10s pela animação...");
                    Thread.Sleep(10000); // Tempo da animação da roleta
                    
                    // 3. Coletar e fechar
                    using var finalScreen = CaptureScreen();
                    DetectAndClick(finalScreen, "btn_coletar_recompensa.png", "Coleta de Prêmio");
                }
            }
            else
            {
                _bot.Log("Roleta diária não detectada ou já realizada.");
            }

            _bot.UpdateStatus(BotState.FriendsModule); // Segue para o próximo estado
        }

        private bool DetectAndClick(Bitmap screen, string templateName, string desc)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", templateName);
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            var point = _vision.FindElement(screen, template);

            if (point != null)
            {
                _bot.Log($"[DailySpin] {desc} localizado. Clicando...");
                PerformPhysicalClick(point.Value);
                return true;
            }
            return false;
        }

        private void PerformPhysicalClick(Point target)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            double x = target.X * (65535.0 / bounds.Width);
            double y = target.Y * (65535.0 / bounds.Height);

            _input.Mouse.MoveMouseTo(x, y);
            _input.Mouse.LeftButtonClick();
        }

        private Bitmap CaptureScreen()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            return bmp;
        }
    }
}