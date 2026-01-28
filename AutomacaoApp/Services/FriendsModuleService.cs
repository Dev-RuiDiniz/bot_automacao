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

            // 1. Navegação e Validação de Entrada
            if (!NavigateToFriendsScreen())
            {
                throw new LightException("Não foi possível acessar a tela de amigos. Tentando novamente no próximo ciclo.");
            }

            _bot.Log("Acesso à tela de amigos confirmado.");

            // 2. Loop de Coleta Contínua
            ProcessFriendsList();

            _bot.Log("Módulo Amigos finalizado.");
            
            // 3. Transição de Estado
            _bot.UpdateStatus(BotState.Roulette);
        }

        private bool NavigateToFriendsScreen()
        {
            using var screen = CaptureScreen();
            string btnPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "amigos.botao_amigos.png");
            string validationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "amigos.tela_validacao.png");

            if (!File.Exists(btnPath)) throw new CriticalException("Asset 'amigos.botao_amigos.png' faltando.");

            using var templateBtn = new Bitmap(btnPath);
            var btnLocation = _vision.FindElement(screen, templateBtn);

            if (btnLocation != null)
            {
                _bot.Log("Clicando no botão de acesso...");
                ClickAt(btnLocation.Value.X, btnLocation.Value.Y);
                
                Thread.Sleep(3000); // Aguarda carregamento da lista
                return ValidateScreen(validationPath);
            }

            return false;
        }

        public void ProcessFriendsList()
        {
            _bot.Log("Iniciando varredura de presentes...");
            bool hasMoreActions = true;
            int maxAttempts = 30; // Segurança contra loops infinitos

            while (hasMoreActions && maxAttempts > 0)
            {
                using var screen = CaptureScreen();

                // 1. Condição de Parada: Lista Vazia
                if (CheckIfListIsEmpty(screen))
                {
                    _bot.Log("Sinal de 'Lista Vazia' detectado.");
                    break;
                }

                // 2. Tenta coletar e depois enviar (Prioridade para entrada de recursos)
                bool collected = DetectAndClick(screen, "amigos.botao_recolher_presente.png", "Coletar");
                bool sent = DetectAndClick(screen, "amigos.botao_enviar_presente.png", "Enviar");

                // Se nada foi encontrado na tela atual
                if (!collected && !sent)
                {
                    _bot.Log("Nenhuma ação pendente na visão atual.");
                    hasMoreActions = false; 
                }

                maxAttempts--;
                Thread.Sleep(800); // Delay para animação de UI
            }
        }

        // Método auxiliar para busca e clique
        private bool DetectAndClick(Bitmap screen, string assetName, string label)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            var location = _vision.FindElement(screen, template);

            if (location != null)
            {
                _bot.Log($"[Ação] {label} encontrado. Clicando...");
                ClickAt(location.Value.X, location.Value.Y);
                return true;
            }
            return false;
        }

        private bool CheckIfListIsEmpty(Bitmap screen)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "amigos.sem_presentes.png");
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        private bool ValidateScreen(string assetPath)
        {
            if (!File.Exists(assetPath)) return true; 

            using var validationScreen = CaptureScreen();
            using var templateValidation = new Bitmap(assetPath);
            return _vision.FindElement(validationScreen, templateValidation) != null;
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