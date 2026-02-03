using WindowsInput;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
using AutomacaoApp.Core;
using AutomacaoApp.Enums;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class BonusModuleService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public BonusModuleService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        /// <summary>
        /// Orquestra a detecção e coleta do bônus que aparece no emulador.
        /// </summary>
        public void Execute()
        {
            _bot.Log("--- Monitor de Bônus: Verificando disponibilidade ---");

            // 1. Aguarda a renderização da página de bônus (Trigger visual)
            if (WaitForBonusPage())
            {
                _bot.Log("Página de bônus detectada. Iniciando coleta...");
                
                // 2. Realiza a interação de coleta
                if (CollectBonus())
                {
                    _bot.Log("Bônus coletado com sucesso!");
                }

                // 3. Retorna ao jogo (Fecha o app de bônus ou clica em voltar)
                ExitBonusPage();
            }
            else
            {
                _bot.Log("Página de bônus não apareceu ou ainda está em cooldown.");
            }
        }

        /// <summary>
        /// Monitora a tela até que o asset da página de bônus seja identificado.
        /// </summary>
        private bool WaitForBonusPage()
        {
            int attempts = 0;
            const int MAX_ATTEMPTS = 10;
            string assetName = "menu.pagina_bonus_carregada.png";

            while (attempts < MAX_ATTEMPTS)
            {
                using var screen = CaptureScreen();
                if (DetectElement(screen, assetName)) return true;

                _bot.Log($"Aguardando carregamento do bônus... ({attempts + 1}/10)");
                Thread.Sleep(1500);
                attempts++;
            }
            return false;
        }

        private bool CollectBonus()
        {
            using var screen = CaptureScreen();
            // Tenta clicar no botão principal de coleta da página
            return DetectAndClick(screen, "menu.btn_coletar_bonus.png", "Coletar Bônus");
        }

        private void ExitBonusPage()
        {
            _bot.Log("Saindo da tela de bônus...");
            using var screen = CaptureScreen();
            
            // Tenta fechar pelo 'X' ou pelo botão de voltar da HUD do emulador
            if (!DetectAndClick(screen, "menu.btn_fechar_bonus.png", "Fechar Bônus"))
            {
                _bot.Log("Botão fechar não encontrado. Usando atalho ESC para voltar.");
                _input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.ESCAPE);
            }
            Thread.Sleep(2000);
        }

        // --- MOTOR DE VISÃO AUXILIAR ---

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
            var location = _vision.FindElement(screen, template);

            if (location != null)
            {
                _bot.Log($"Clicando em: {label}");
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