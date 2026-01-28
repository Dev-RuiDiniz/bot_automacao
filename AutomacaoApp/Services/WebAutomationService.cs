using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
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
        /// Inicia o Chrome e aguarda a renderização da interface.
        /// </summary>
        public void OpenBrowser(string url = "about:blank")
        {
            _bot.Log($"Abrindo Chrome em: {url}");

            try
            {
                // Inicia o processo do navegador
                Process.Start(new ProcessStartInfo
                {
                    FileName = "chrome.exe",
                    Arguments = url + " --start-maximized",
                    UseShellExecute = true
                });

                // Validação de Carregamento
                if (WaitForBrowserReady())
                {
                    _bot.Log("Chrome carregado e barra de endereço validada.");
                }
                else
                {
                    throw new LightException("O Chrome abriu, mas a barra de endereço não foi localizada.");
                }
            }
            catch (Exception ex)
            {
                _bot.Log($"Erro ao iniciar navegador: {ex.Message}");
                throw new CriticalException("Falha ao abrir o Google Chrome. Verifique se está instalado.");
            }
        }

        /// <summary>
        /// Loop de verificação visual para garantir que a GUI do Chrome está pronta.
        /// </summary>
        private bool WaitForBrowserReady()
        {
            int attempts = 0;
            const int MAX_WAIT_SECONDS = 10;
            string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "chrome.chrome_barra_endereco.png");

            if (!File.Exists(assetPath)) 
                throw new CriticalException("Asset da barra de endereços do Chrome não encontrado.");

            using var template = new Bitmap(assetPath);

            while (attempts < MAX_WAIT_SECONDS)
            {
                using var screen = CaptureScreen();
                var location = _vision.FindElement(screen, template);

                if (location != null) return true;

                _bot.Log("Aguardando interface do Chrome...");
                Thread.Sleep(1000);
                attempts++;
            }

            return false;
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