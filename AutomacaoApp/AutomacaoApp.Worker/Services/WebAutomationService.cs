using System;
using System.Diagnostics;
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

        public void EnsureBrowserOpen()
        {
            _bot.Log("--- Validando Chrome no Emulador ---");
            CheckForCaptcha(); // Proteção inicial

            using var screen = CaptureScreen();
            if (FindAsset(screen, "chrome.android_barra_endereco.png") == null)
            {
                if (!DetectAndClick(screen, "chrome.android_icone.png", "Ícone Chrome"))
                {
                    throw new CriticalException("Chrome não encontrado no MEmu.");
                }
                Thread.Sleep(5000);
            }
        }

        public void NavigateInAndroid(string url)
        {
            _bot.Log($"Navegando para: {url}");
            CheckForCaptcha();

            using var screen = CaptureScreen();
            var bar = FindAsset(screen, "chrome.android_barra_endereco.png");
            if (bar != null)
            {
                ClickAt(bar.Value.X, bar.Value.Y);
                Thread.Sleep(1000);
                _input.Keyboard.KeyPress(VirtualKeyCode.BACK);
                _input.Keyboard.TextEntry(url);
                _input.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                Thread.Sleep(6000);
                CheckForCaptcha();
            }
        }

        // SOLUÇÃO DO ERRO CS1061: Método de Login implementado aqui
        public void FillLoginForm(string user, string pass)
        {
            _bot.Log("Preenchendo credenciais no site...");
            CheckForCaptcha();

            if (DetectAndClickInsideWeb("site.campo_usuario.png", "Campo Usuário"))
            {
                _input.Keyboard.TextEntry(user);
                Thread.Sleep(600);
            }

            if (DetectAndClickInsideWeb("site.campo_senha.png", "Campo Senha"))
            {
                _input.Keyboard.TextEntry(pass);
                Thread.Sleep(600);
            }

            DetectAndClickInsideWeb("site.botao_login.png", "Botão Login");
        }

        public void CheckForCaptcha()
        {
            using var screen = CaptureScreen();
            if (FindAsset(screen, "chrome.captcha_detectado.png") != null)
            {
                _bot.Log("[ALERTA] CAPTCHA DETECTADO! Encerrando MEmu...");
                ForceKillEmulator();
                throw new CriticalException("Captcha detectado. Operação interrompida.");
            }
        }

        public bool WaitForWebBonus()
        {
            int attempts = 0;
            while (attempts < 15)
            {
                CheckForCaptcha();
                using var screen = CaptureScreen();
                if (FindAsset(screen, "chrome.android_bonus_pronto.png") != null) return true;
                Thread.Sleep(1500);
                attempts++;
            }
            return false;
        }

        // MÉTODOS AUXILIARES
        private bool DetectAndClickInsideWeb(string assetName, string label)
        {
            using var screen = CaptureScreen();
            return DetectAndClick(screen, assetName, label);
        }

        private void ForceKillEmulator()
        {
            foreach (var process in Process.GetProcessesByName("MEmu")) process.Kill();
        }

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
                _bot.Log($"Clicando em: {label}");
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
            var b = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            Bitmap bmp = new Bitmap(b.Width, b.Height);
            using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(Point.Empty, Point.Empty, b.Size);
            return bmp;
        }
    }
}
