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
    public class ShutdownService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public ShutdownService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        /// <summary>
        /// Realiza o fechamento de todos os aplicativos e retorna à Home do Android.
        /// </summary>
        public void CleanCleanup()
        {
            _bot.Log("--- [FINALIZAÇÃO] Iniciando Limpeza de Memória ---");

            // 1. Abre o menu de Apps Recentes
            _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.TAB); 
            Thread.Sleep(1500);

            // 2. Tenta localizar e clicar no botão "Fechar Tudo" ou "Limpar Tudo" do MEmu
            // Isso encerra o Jogo e o Chrome de uma só vez.
            using (var screen = CaptureScreen())
            {
                if (!DetectAndClick(screen, "home.btn_limpar_tudo.png", "Limpar Apps Recentes"))
                {
                    _bot.Log("Botão 'Limpar Tudo' não visto. Forçando fechamento via Home.");
                }
            }

            // 3. Garante o retorno à Tela Home do Android
            ReturnToHome();

            _bot.Log("Instância limpa e pronta para standby.");
        }

        /// <summary>
        /// Pressiona o botão Home e valida visualmente se chegamos à tela inicial.
        /// </summary>
        private void ReturnToHome()
        {
            int attempts = 0;
            while (attempts < 3)
            {
                _bot.Log("Pressionando botão HOME...");
                // Atalho HOME padrão no MEmu é Alt+1 ou comando via HUD
                _input.Keyboard.KeyPress(VirtualKeyCode.ESCAPE); 
                Thread.Sleep(2000);

                using var screen = CaptureScreen();
                if (DetectElement(screen, "home.tela_home.png"))
                {
                    _bot.Log("Home detectada com sucesso.");
                    return;
                }
                attempts++;
            }
        }

        // --- MOTOR DE VISÃO E APOIO ---

        private bool DetectElement(Bitmap screen, string assetName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return false;
            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        private bool DetectAndClick(Bitmap screen, string assetName, string label)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            var loc = _vision.FindElement(screen, template);
            if (loc != null)
            {
                _bot.Log($"Executando: {label}");
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