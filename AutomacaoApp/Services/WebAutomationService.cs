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
        /// Inicia o Chrome, maximiza e valida a interface gráfica inicial.
        /// </summary>
        public void OpenBrowser(string url = "about:blank")
        {
            _bot.Log($"Abrindo Chrome em: {url}");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "chrome.exe",
                    Arguments = $"{url} --start-maximized --disable-notifications",
                    UseShellExecute = true
                });

                if (WaitForBrowserReady())
                {
                    _bot.Log("Chrome carregado e barra de endereço validada.");
                }
                else
                {
                    throw new LightException("O Chrome abriu, mas a interface (barra de endereço) não foi localizada.");
                }
            }
            catch (Exception ex)
            {
                _bot.Log($"Erro ao iniciar navegador: {ex.Message}");
                throw new CriticalException("Falha crítica ao abrir o Google Chrome.");
            }
        }

        /// <summary>
        /// Navega para uma URL específica limpando a barra de endereços detectada.
        /// </summary>
        public void NavigateToUrl(string url)
        {
            _bot.Log($"Navegando para: {url}");

            using var screen = CaptureScreen();
            var location = FindAssetOnScreen(screen, "chrome.chrome_barra_endereco.png");

            if (location != null)
            {
                // Foco e Sanitização
                ClickAt(location.Value.X, location.Value.Y);
                Thread.Sleep(500);
                
                _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                Thread.Sleep(200);
                _input.Keyboard.KeyPress(VirtualKeyCode.BACK);
                
                // Entrada da URL
                _input.Keyboard.TextEntry(url);
                _input.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                _bot.Log("URL enviada. Aguardando processamento...");
                Thread.Sleep(5000); // Tempo para o servidor começar a responder
            }
            else
            {
                throw new LightException("Falha ao localizar barra de endereço para navegação.");
            }
        }

        /// <summary>
        /// Valida se o site carregou corretamente buscando um elemento visual único (Ex: Logo).
        /// </summary>
        public bool ValidateSite(string assetName)
        {
            _bot.Log($"Validando site via: {assetName}");
            
            int attempts = 0;
            const int MAX_LOAD_WAIT = 15;

            while (attempts < MAX_LOAD_WAIT)
            {
                using var screen = CaptureScreen();
                if (FindAssetOnScreen(screen, assetName) != null)
                {
                    _bot.Log("Site validado com sucesso.");
                    return true;
                }

                Thread.Sleep(1000);
                attempts++;
            }

            return false;
        }

        /// <summary>
        /// Realiza o preenchimento de credenciais em campos detectados visualmente.
        /// </summary>
        public void FillLoginForm(string user, string pass)
        {
            _bot.Log("Preenchendo formulário de login...");

            // Preencher Usuário
            if (DetectAndClickInsideWeb("site.campo_usuario.png", "Campo Usuário"))
            {
                _input.Keyboard.TextEntry(user);
                Thread.Sleep(600);
            }

            // Preencher Senha
            if (DetectAndClickInsideWeb("site.campo_senha.png", "Campo Senha"))
            {
                _input.Keyboard.TextEntry(pass);
                Thread.Sleep(600);
            }

            // Clique no botão de confirmação
            DetectAndClickInsideWeb("site.botao_login.png", "Botão Login");
        }

        // --- MÉTODOS AUXILIARES DE MOTOR ---

        private bool DetectAndClickInsideWeb(string assetName, string label)
        {
            using var screen = CaptureScreen();
            var location = FindAssetOnScreen(screen, assetName);

            if (location != null)
            {
                _bot.Log($"Elemento encontrado: {label}");
                ClickAt(location.Value.X, location.Value.Y);
                Thread.Sleep(400); // Aguarda o campo ganhar foco
                return true;
            }
            _bot.Log($"[AVISO] Elemento não encontrado: {label}");
            return false;
        }

        private Point? FindAssetOnScreen(Bitmap screen, string assetName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return null;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template);
        }

        private bool WaitForBrowserReady()
        {
            int attempts = 0;
            while (attempts < 12)
            {
                using var screen = CaptureScreen();
                if (FindAssetOnScreen(screen, "chrome.chrome_barra_endereco.png") != null) return true;

                Thread.Sleep(1000);
                attempts++;
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