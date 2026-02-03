using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput; // Necessário para IInputSimulator
using WindowsInput.Native;
using AutomacaoApp.Core;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class AppBridgeService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public AppBridgeService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            // Inicialização padrão do simulador de input
            _input = new InputSimulator();
        }

        public void ReturnToGame()
        {
            _bot.Log("--- Bridge: Retornando ao Jogo Principal ---");

            HandlePotentialCrash();

            _bot.Log("Acessando alternador de aplicativos (ALT+TAB)...");
            // Simula a troca de apps no Android/Windows
            _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.TAB); 
            Thread.Sleep(1500);

            using var screen = CaptureScreen();
            // Lógica para encontrar a miniatura do jogo...
        }

        public void HandlePotentialCrash()
        {
            // Lógica de verificação de crash
        }

        private void ClickAt(int x, int y)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            // Conversão de coordenadas para o sistema absoluto (0-65535)
            _input.Mouse.MoveMouseTo(x * (65535.0 / bounds.Width), y * (65535.0 / bounds.Height));
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