using WindowsInput;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput; // Se instalou o Plus, este using deve brilhar agora
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class RecoveryService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input; 

        public RecoveryService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            // No Plus, a instância é criada assim:
            _input = new InputSimulator();
        }

        public void CheckAndHandleErrors()
        {
            _bot.Log("Verificando obstruções...");
            using var currentScreen = CaptureScreen();
            DetectAndClick(currentScreen, "popup_erro_conexao.png", "Erro de Conexão");
        }

        private bool DetectAndClick(Bitmap screen, string templateName, string description)
        {
            string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", templateName);
            if (!File.Exists(assetPath)) return false;

            using var template = new Bitmap(assetPath);
            var location = _vision.FindElement(screen, template);

            if (location != null)
            {
                _bot.Log($"[Recuperação] {description} detectado em {location.Value.X}, {location.Value.Y}");
                
                // Conversão de coordenadas para 0-65535
                var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
                double inputX = location.Value.X * (65535.0 / bounds.Width);
                double inputY = location.Value.Y * (65535.0 / bounds.Height);

                _input.Mouse.MoveMouseTo(inputX, inputY);
                _input.Mouse.LeftButtonClick();
                return true;
            }
            return false;
        }

        private Bitmap CaptureScreen()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            return bitmap;
        }
    }
}