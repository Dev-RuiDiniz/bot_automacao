using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
using WindowsInput.Native;
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class WebAutomationService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public WebAutomationService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        /// <summary>
        /// Valida se o navegador Chrome (Android) está aberto dentro do MEmu.
        /// </summary>
        public void EnsureBrowserOpen()
        {
            _bot.Log("--- Validando Chrome no Emulador ---");

            using var screen = CaptureScreen();
            
            // Procura o asset da barra de endereços do Chrome Mobile
            if (FindAsset(screen, "chrome.android_barra_endereco.png") == null)
            {
                _bot.Log("Chrome não detectado na frente. Tentando abrir via ícone...");
                if (!DetectAndClick(screen, "chrome.android_icone.png", "Ícone Chrome"))
                {
                    throw new CriticalException("Não foi possível localizar o Chrome no menu do emulador.");
                }
                Thread.Sleep(4000); // Aguarda abertura do app
            }
        }

        /// <summary>
        /// Navega para a URL usando a interface do Chrome Mobile.
        /// </summary>
        public void NavigateInAndroid(string url)
        {
            _bot.Log($"Navegando para: {url}");

            using var screen = CaptureScreen();
            var bar = FindAsset(screen, "chrome.android_barra_endereco.png");

            if (bar != null)
            {
                // 1. Clica na barra para abrir o teclado do Android
                ClickAt(bar.Value.X, bar.Value.Y);
                Thread.Sleep(800);

                // 2. No Android, clicar na barra geralmente já seleciona tudo. 
                // Usamos BACKSPACE por segurança para limpar.
                _input.Keyboard.KeyPress(VirtualKeyCode.BACK);
                Thread.Sleep(200);

                // 3. Digita a URL e pressiona ENTER
                _input.Keyboard.TextEntry(url);
                _input.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                _bot.Log("Aguardando carregamento da página mobile...");
                Thread.Sleep(6000);
            }
        }

        /// <summary>
        /// Monitor de carregamento para a página de bônus dentro do navegador mobile.
        /// </summary>
        public bool WaitForWebBonus()
        {
            _bot.Log("Monitorando renderização da página de bônus...");
            
            int attempts = 0;
            const int MAX_WAIT = 15;
            // Asset de um elemento único da página web (ex: botão de claim ou logo do site)
            string assetName = "chrome.android_bonus_pronto.png";

            while (attempts < MAX_WAIT)
            {
                using var screen = CaptureScreen();
                if (FindAsset(screen, assetName) != null)
                {
                    _bot.Log("Página de bônus mobile carregada!");
                    return true;
                }

                Thread.Sleep(1500);
                attempts++;
            }
            return false;
        }

        // --- MOTOR DE INTERAÇÃO (OTIMIZADO PARA EMULADOR) ---

        private Point? FindAsset(Bitmap screen, string assetName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return null;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template);
        }

        private bool DetectAndClick(Bitmap screen, string assetName, string label)
        {
            var location = FindAsset(screen, assetName);
            if (location != null)
            {
                _bot.Log($"Ação: {label}");
                ClickAt(location.Value.X, location.Value.Y);
                return true;
            }
            return false;
        }

        private void ClickAt(int x, int y)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            double inputX = x * (65535.0 / bounds.Width);
            double inputY = y * (65535.0 / bounds.Height);
            _input.Mouse.MoveMouseTo(inputX, inputY);
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