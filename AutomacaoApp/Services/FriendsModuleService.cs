using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using WindowsInput;
using AutomacaoApp.Core;
using AutomacaoApp.Enums;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    [SupportedOSPlatform("windows")]
    public class FriendsModuleService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public FriendsModuleService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        public void Execute()
        {
            _bot.Log("Iniciando Módulo: Amigos");

            // 1. Tentar acessar a aba de amigos a partir da Home
            if (!NavigateToFriendsScreen())
            {
                throw new LightException("Não foi possível acessar a tela de amigos. Tentando novamente no próximo ciclo.");
            }

            // 2. Aqui entrará a lógica de coletar/enviar presentes (Módulo 3)
            _bot.Log("Acesso à tela de amigos confirmado. Pronto para interações.");

            // Após finalizar, atualiza o status para o próximo módulo
            _bot.UpdateStatus(BotState.Roulette);
        }

        private bool NavigateToFriendsScreen()
        {
            using var screen = CaptureScreen();
            string btnPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "amigos.botao_amigos.png");
            string validationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "amigos.tela_validacao.png");

            if (!File.Exists(btnPath)) throw new CriticalException("Asset do botão amigos não encontrado.");

            using var templateBtn = new Bitmap(btnPath);
            var btnLocation = _vision.FindElement(screen, templateBtn);

            if (btnLocation != null)
            {
                _bot.Log("Clicando no botão Amigos...");
                ClickAt(btnLocation.Value.X, btnLocation.Value.Y);
                
                // Aguarda transição de tela
                Thread.Sleep(3000);

                // Validação: Verifica se realmente entrou na tela de amigos
                return ValidateScreen(validationPath);
            }

            return false;
        }

        private bool ValidateScreen(string assetPath)
        {
            if (!File.Exists(assetPath)) return true; // Se não houver imagem de validação, assume sucesso

            using var validationScreen = CaptureScreen();
            using var templateValidation = new Bitmap(assetPath);
            
            var found = _vision.FindElement(validationScreen, templateValidation);
            return found != null;
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