using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
using WindowsInput.Native; // Necessário para VirtualKeyCode
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
        /// Inicia o Chrome, maximiza e valida a interface.
        /// </summary>
        public void OpenBrowser(string url = "about:blank")
        {
            _bot.Log($"Abrindo Chrome em: {url}");

            try
            {
                // Inicia o processo do Chrome com flags para facilitar a automação
                Process.Start(new ProcessStartInfo
                {
                    FileName = "chrome.exe",
                    Arguments = url + " --start-maximized --disable-notifications",
                    UseShellExecute = true
                });

                // Validação visual de carregamento da GUI
                if (WaitForBrowserReady())
                {
                    _bot.Log("Chrome carregado e barra de endereço validada.");
                }
                else
                {
                    throw new LightException("O Chrome abriu, mas a barra de endereço não foi localizada visualmente.");
                }
            }
            catch (Exception ex)
            {
                _bot.Log($"Erro ao iniciar navegador: {ex.Message}");
                throw new CriticalException("Falha crítica ao abrir o Google Chrome.");
            }
        }

        /// <summary>
        /// Navega para uma URL clicando na barra de endereços detectada.
        /// </summary>
        public void NavigateToUrl(string url)
        {
            _bot.Log($"Navegando para: {url}");

            string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "chrome.chrome_barra_endereco.png");
            
            using var screen = CaptureScreen();
            using var template = new Bitmap(assetPath);

            // 1. Localiza a barra de endereços como âncora de clique
            var location = _vision.FindElement(screen, template);

            if (location != null)
            {
                // 2. Foco e Sanitização: Clica, limpa o campo (Ctrl+A -> Backspace)
                ClickAt(location.Value.X, location.Value.Y);
                Thread.Sleep(500);
                
                _bot.Log("Limpando barra de endereços...");
                _input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                Thread.Sleep(200);
                _input.Keyboard.KeyPress(VirtualKeyCode.BACK);
                
                // 3. Entrada da URL e Execução (Enter)
                _bot.Log("Digitando URL...");
                _input.Keyboard.TextEntry(url);
                _input.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                // Aguarda tempo de resposta do servidor
                Thread.Sleep(5000); 
            }
            else
            {
                throw new LightException("Falha ao localizar barra de endereço para navegação.");
            }
        }

        /// <summary>
        /// Verifica periodicamente se a barra de endereços apareceu na tela.
        /// </summary>
        private bool WaitForBrowserReady()
        {
            int attempts = 0;
            const int MAX_WAIT_SECONDS = 12;
            string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "chrome.chrome_barra_endereco.png");

            if (!File.Exists(assetPath)) 
                throw new CriticalException("Asset 'chrome.chrome_barra_endereco.png' não encontrado.");

            using var template = new Bitmap(assetPath);

            while (attempts < MAX_WAIT_SECONDS)
            {
                using var screen = CaptureScreen();
                var location = _vision.FindElement(screen, template);

                if (location != null) return true;

                _bot.Log($"Aguardando Chrome ({attempts + 1}s)...");
                Thread.Sleep(1000);
                attempts++;
            }

            return false;
        }

        // --- MÉTODOS AUXILIARES DE MOTOR ---

        private void ClickAt(int x, int y)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            
            // Conversão para coordenadas absolutas do Windows Input (0-65535)
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