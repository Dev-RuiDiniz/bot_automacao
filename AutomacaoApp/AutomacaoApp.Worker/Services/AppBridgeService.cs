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

            // 1. Verifica se o app crashou ANTES de tentar alternar
            HandlePotentialCrash();

            // 2. Abre a lista de Apps Recentes do Android
            _bot.Log("Acessando alternador de aplicativos...");
            _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.TAB); 
            Thread.Sleep(1500);

            using var screen = CaptureScreen();

            // 3. Busca a miniatura do Jogo para retomar o foco
            var gameThumb = FindAsset(screen, "bridge.miniatura_jogo.png");
            if (gameThumb != null)
            {
                _bot.Log("Miniatura do jogo localizada. Retomando foco...");
                ClickAt(gameThumb.Value.X, gameThumb.Value.Y);
            }
            else
            {
                _bot.Log("Miniatura não encontrada. Forçando retorno via Home Screen...");
                ReturnToHomeAndOpen();
            }

            // 4. Validação de Retorno
            ValidateGameActive();
        }

        /// <summary>
        /// Detecta se o jogo parou de funcionar e realiza a reinicialização completa (Cold Boot).
        /// </summary>
        public void HandlePotentialCrash()
        {
            using var screen = CaptureScreen();
            
            // Verifica assets de erro: popup do Android ("App parou") ou tela totalmente preta
            bool isCrashed = FindAsset(screen, "erros.app_crash.png") != null;
            bool isBlackScreen = CheckForBlackScreen(screen);

            if (isCrashed || isBlackScreen)
            {
                _bot.Log("[CRÍTICO] Falha no APK detectada (Crash ou Tela Preta).");
                
                // Procedimento de limpeza: Fecha o app nos recentes
                ForceCloseCurrentApp();
                
                // Reinicia do zero
                ReturnToHomeAndOpen();
            }
        }

        private void ReturnToHomeAndOpen()
        {
            _bot.Log("Executando Cold Boot: Voltando à Home...");
            _input.Keyboard.KeyPress(VirtualKeyCode.ESCAPE); 
            Thread.Sleep(2000);
            
            using var homeScreen = CaptureScreen();
            if (DetectAndClick(homeScreen, "game.icone_principal.png", "Ícone do Jogo"))
            {
                _bot.Log("Jogo reaberto. Aguardando carregamento inicial (Splash Screen)...");
                Thread.Sleep(15000); // Tempo de espera estendido para o boot frio
            }
        }

        private void ForceCloseCurrentApp()
        {
            _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.TAB);
            Thread.Sleep(1000);
            
            using var screen = CaptureScreen();
            // Tenta clicar no 'X' da miniatura do jogo se ele estiver visível
            DetectAndClick(screen, "bridge.btn_fechar_miniatura.png", "Encerrando processo travado");
            Thread.Sleep(1000);
        }

        private bool CheckForBlackScreen(Bitmap screen)
        {
            // Lógica simples de amostragem: verifica se o centro da tela é totalmente preto
            Color centerPixel = screen.GetPixel(screen.Width / 2, screen.Height / 2);
            return centerPixel.R == 0 && centerPixel.G == 0 && centerPixel.B == 0;
        }

        private void ValidateGameActive()
        {
            int attempts = 0;
            while (attempts < 5)
            {
                using var screen = CaptureScreen();
                if (FindAsset(screen, "game.hud_principal.png") != null)
                {
                    _bot.Log("Foco do Jogo reestabelecido e HUD validada.");
                    return;
                }
                Thread.Sleep(2500);
                attempts++;
            }
            _bot.Log("[AVISO] HUD do jogo não detectada. O app pode estar em tela de loading.");
        }

        // --- MÉTODOS DE APOIO TÉCNICO ---

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
                _bot.Log($"Ação: {label}");
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
            using (Graphics g = Graphics.FromImage(bmp)) 
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            return bmp;
        }
    }
}