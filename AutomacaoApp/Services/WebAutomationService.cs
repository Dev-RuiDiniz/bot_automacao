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

        /// <summary>
        /// Valida se o Chrome (Android) está ativo. 
        /// Agora inclui verificação preventiva de CAPTCHA antes de qualquer ação.
        /// </summary>
        public void EnsureBrowserOpen()
        {
            _bot.Log("--- Validando Chrome no Emulador ---");
            
            // Prioridade Máxima: Verificar Captcha antes de começar
            CheckForCaptcha();

            using var screen = CaptureScreen();
            
            if (FindAsset(screen, "chrome.android_barra_endereco.png") == null)
            {
                _bot.Log("Chrome oculto. Tentando abrir via ícone no MEmu...");
                if (!DetectAndClick(screen, "chrome.android_icone.png", "Ícone Chrome"))
                {
                    throw new CriticalException("Falha ao localizar o Chrome. O emulador pode estar travado ou na tela errada.");
                }
                Thread.Sleep(5000); // Aguarda renderização do app mobile
            }
        }

        /// <summary>
        /// Navega para a URL. Limpa o campo e verifica Captcha pós-carregamento.
        /// </summary>
        public void NavigateInAndroid(string url)
        {
            _bot.Log($"Navegando para: {url}");

            using var screen = CaptureScreen();
            var bar = FindAsset(screen, "chrome.android_barra_endereco.png");

            if (bar != null)
            {
                // 1. Foco na barra (Teclado Android sobe)
                ClickAt(bar.Value.X, bar.Value.Y);
                Thread.Sleep(1000);

                // 2. Limpeza preventiva
                _input.Keyboard.KeyPress(VirtualKeyCode.BACK);
                Thread.Sleep(200);

                // 3. Digitação e Execução
                _input.Keyboard.TextEntry(url);
                _input.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                _bot.Log("URL enviada. Aguardando carregamento e vigiando CAPTCHAs...");
                
                // 4. Aguarda e verifica se o site carregou um desafio de robô
                Thread.Sleep(6000);
                CheckForCaptcha(); 
            }
        }

        /// <summary>
        /// MONITOR DE CAPTCHA (KILL SWITCH): 
        /// Se detectado, encerra o emulador e interrompe o bot imediatamente.
        /// </summary>
        public void CheckForCaptcha()
        {
            using var screen = CaptureScreen();
            if (FindAsset(screen, "chrome.captcha_detectado.png") != null)
            {
                _bot.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                _bot.Log("[CRÍTICO] CAPTCHA DETECTADO! ACIONANDO KILL SWITCH.");
                _bot.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                ForceKillEmulator(); // Fecha o MEmu instantaneamente
                
                throw new CriticalException("Segurança: Captcha detectado. Bot encerrado para evitar banimento.");
            }
        }

        /// <summary>
        /// Monitor de carregamento para a página de bônus.
        /// </summary>
        public bool WaitForWebBonus()
        {
            _bot.Log("Monitorando renderização do bônus...");
            
            int attempts = 0;
            const int MAX_WAIT = 15;
            string assetName = "chrome.android_bonus_pronto.png";

            while (attempts < MAX_WAIT)
            {
                CheckForCaptcha(); // Verifica captcha em cada loop de carregamento

                using var screen = CaptureScreen();
                if (FindAsset(screen, assetName) != null)
                {
                    _bot.Log("Bônus mobile carregado e pronto!");
                    return true;
                }

                Thread.Sleep(1500);
                attempts++;
            }
            return false;
        }

        // --- MOTOR DE INTERAÇÃO E SEGURANÇA ---

        private void ForceKillEmulator()
        {
            try
            {
                _bot.Log("Finalizando processos do MEmu...");
                foreach (var process in Process.GetProcessesByName("MEmu"))
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                _bot.Log($"Erro ao forçar fechamento: {ex.Message}");
            }
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