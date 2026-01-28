using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
using AutomacaoApp.Core;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class InGameBonusService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public InGameBonusService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        /// <summary>
        /// Executa o fluxo completo de verificação e coleta do bônus interno do jogo.
        /// </summary>
        public void ProcessDailyBonus()
        {
            _bot.Log("--- [IN-GAME] Verificando Bônus Diário ---");

            using var screen = CaptureScreen();

            // 1. Verifica se o botão de bônus está com o asset de "Disponível" (ex: brilhando ou colorido)
            if (DetectElement(screen, "bonus.botao_bonus_disponivel.png"))
            {
                _bot.Log("Bônus disponível detectado! Iniciando coleta...");
                
                if (DetectAndClick(screen, "bonus.botao_bonus_disponivel.png", "Botão Coletar Bônus"))
                {
                    // 2. Aguarda a animação de abertura e confirmação visual
                    if (ValidateCollection())
                    {
                        _bot.Log("Sucesso: Bônus coletado e confirmado visualmente.");
                    }
                    else
                    {
                        _bot.Log("[AVISO] Bônus clicado, mas a confirmação visual não apareceu.");
                    }
                }
            }
            else
            {
                _bot.Log("Bônus ainda não está disponível para coleta.");
            }
        }

        /// <summary>
        /// Aguarda o popup ou ícone de confirmação (bonus_coletado) aparecer na tela.
        /// </summary>
        private bool ValidateCollection()
        {
            int attempts = 0;
            const int MAX_WAIT = 8; // Segundos para o popup de recompensa aparecer
            string assetName = "bonus.bonus_coletado.png";

            while (attempts < MAX_WAIT)
            {
                Thread.Sleep(1000);
                using var screen = CaptureScreen();
                
                if (DetectElement(screen, assetName))
                {
                    // Opcional: Clicar para fechar o popup de bônus se ele bloquear a tela
                    DetectAndClick(screen, "bonus.bonus_coletado.png", "Fechar Confirmação");
                    return true;
                }
                attempts++;
            }
            return false;
        }

        // --- MOTOR DE VISÃO PADRONIZADO ---

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