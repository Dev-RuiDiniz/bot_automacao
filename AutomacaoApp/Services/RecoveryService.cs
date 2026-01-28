using System;
using System.Drawing;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput; // InputSimulator
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class RecoveryService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly InputSimulator _input;

        public RecoveryService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        /// <summary>
        /// Verifica se há pop-ups de erro de conexão ou anúncios bloqueando a tela.
        /// </summary>
        public void CheckAndHandleErrors()
        {
            _bot.Log("Verificando obstruções na tela (Erros/Popups)...");

            using var currentScreen = CaptureScreen();

            // 1. Verificar Erro de Conexão (Crítico)
            if (DetectAndClick(currentScreen, "popup_erro_conexao.png", "Erro de Conexão"))
            {
                _bot.Log("Aguardando 10 segundos para estabilização da rede...");
                Thread.Sleep(10000);
                return;
            }

            // 2. Verificar Popups Genéricos / Botão Fechar (Leve)
            DetectAndClick(currentScreen, "btn_fechar_popup.png", "Popup de Anúncio/Informativo");
        }

        private bool DetectAndClick(Bitmap screen, string templateName, string description)
        {
            string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", templateName);
            
            if (!File.Exists(assetPath)) return false;

            using var template = new Bitmap(assetPath);
            var location = _vision.FindElement(screen, template);

            if (location != null)
            {
                _bot.Log($"[Recuperação] {description} detectado! Tentando fechar...");
                
                // Move o mouse e clica
                // Nota: O InputSimulator usa coordenadas absolutas (0-65535) ou pixels
                // Aqui vamos usar o clique simples nas coordenadas do pixel
                ClickAt(location.Value.X, location.Value.Y);
                
                Thread.Sleep(2000); // Aguarda o fechamento da animação
                return true;
            }

            return false;
        }

        private void ClickAt(int x, int y)
        {
            // O mouse deve se mover até a posição antes de clicar
            // Convertendo coordenadas de pixel para o formato do InputSimulator
            double screenWidth = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Width;
            double screenHeight = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Height;

            double inputX = x * (65535.0 / screenWidth);
            double inputY = y * (65535.0 / screenHeight);

            _input.Mouse.MoveMouseTo(inputX, inputY);
            _input.Mouse.LeftButtonClick();
        }

        private Bitmap CaptureScreen()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            return bitmap;
        }
    }
}