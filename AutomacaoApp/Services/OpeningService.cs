using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Runtime.Versioning;
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class OpeningService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly string _gamePath = @"C:\Caminho\Para\SeuJogo.exe";

        public OpeningService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
        }

        public void StartGame()
        {
            _bot.Log("Iniciando processo do jogo...");
            
            // 1. Inicia o processo
            Process.Start(_gamePath);

            // 2. Aguarda a Tela Home (Polling)
            // Tentaremos localizar a home por até 60 segundos
            bool isHomeFound = WaitForHome(60);

            if (!isHomeFound)
            {
                throw new CriticalException("O jogo abriu, mas a Tela Home não foi detectada (Timeout).");
            }

            _bot.Log("Tela Home detectada! Bot pronto para operar.");
        }

        private bool WaitForHome(int timeoutSeconds)
        {
            using var templateHome = new Bitmap("assets/tela_home.png");
            
            for (int i = 0; i < timeoutSeconds; i++)
            {
                _bot.Log($"Aguardando carregamento... ({i}s)");

                // Captura a tela atual (Necessita do ScreenCapture que podemos fazer a seguir)
                using var currentScreen = CaptureScreen(); 
                
                var element = _vision.FindElement(currentScreen, templateHome);

                if (element != null) return true;

                Thread.Sleep(1000); // Espera 1 segundo antes da próxima tentativa
            }

            return false;
        }

        private Bitmap CaptureScreen()
        {
            // Método simples de captura de tela cheia
            Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            return bitmap;
        }
    }
}