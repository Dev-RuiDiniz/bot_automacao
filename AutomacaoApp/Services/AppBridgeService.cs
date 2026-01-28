using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
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
            _input = new InputSimulator();
        }

        /// <summary>
        /// Realiza a transição do Chrome de volta para o Jogo.
        /// </summary>
        public void ReturnToGame()
        {
            _bot.Log("--- Bridge: Retornando ao Jogo Principal ---");

            // 1. Abre a lista de Apps Recentes do Android (Botão quadrado do MEmu)
            // Geralmente mapeado para uma coordenada fixa na barra lateral ou atalho
            _bot.Log("Acessando alternador de aplicativos...");
            _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.TAB); // Atalho comum para 'Recentes'
            Thread.Sleep(1500);

            using var screen = CaptureScreen();

            // 2. Busca a miniatura do Jogo para retomar o foco
            var gameThumb = FindAsset(screen, "bridge.miniatura_jogo.png");
            if (gameThumb != null)
            {
                _bot.Log("Miniatura do jogo localizada. Retomando foco...");
                ClickAt(gameThumb.Value.X, gameThumb.Value.Y);
            }
            else
            {
                // Fallback: Se não achar nos recentes, volta para a Home e clica no ícone principal
                _bot.Log("Miniatura não encontrada. Forçando retorno via Home Screen...");
                _input.Keyboard.KeyPress(VirtualKeyCode.ESCAPE); // Simula botão 'Home' ou 'Back'
                Thread.Sleep(1000);
                
                using var homeScreen = CaptureScreen();
                DetectAndClick(homeScreen, "game.icone_principal.png", "Ícone do Jogo");
            }

            // 3. Validação de Retorno
            ValidateGameActive();
        }

        private void ValidateGameActive()
        {
            int attempts = 0;
            while (attempts < 5)
            {
                using var screen = CaptureScreen();
                if (FindAsset(screen, "game.hud_principal.png") != null)
                {
                    _bot.Log("Foco do Jogo reestabelecido com sucesso.");
                    return;
                }
                Thread.Sleep(2000);
                attempts++;
            }
            _bot.Log("[AVISO] Interface do jogo não detectada após transição.");
        }

        // --- MÉTODOS DE APOIO ---

        private Point? FindAsset(Bitmap screen, string assetName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return null;
            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template);
        }

        private bool DetectAndClick(Bitmap screen, string assetName, string label)
        {
            var loc = FindAsset(screen, assetName);
            if (loc != null)
            {
                ClickAt(loc.Value.X, loc.Value.Y);
                return true;
            }
            return false;
        }

        private void ClickAt(int x, int y)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            _input.Mouse.MoveMouseTo(x * (65535.0 / bounds.Width), y * (65535.0 / bounds.Height));
            _input.Mouse.LeftButtonClick();
        }

        private Bitmap CaptureScreen()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            return bmp;
        }
    }
}