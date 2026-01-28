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
    public class RouletteModuleService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public RouletteModuleService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        public void Execute()
        {
            _bot.Log("--- Iniciando Módulo: Roleta Principal ---");

            // 1. Entrada: Clica no ícone da roleta na HUD
            if (!NavigateToRoulette())
            {
                _bot.Log("Falha ao entrar na Roleta. Verificando se já está nela...");
                if (!ValidateInRoulette()) return; 
            }

            // 2. Execução de 2 Giros
            for (int i = 1; i <= 2; i++)
            {
                _bot.Log($"Executando Giro {i}/2...");
                if (!PerformSpin())
                {
                    _bot.Log("Interrompendo giros por falta de recursos ou erro.");
                    break;
                }
            }

            // 3. Saída: Retorna para a Home
            ExitRoulette();

            _bot.Log("Módulo Roleta finalizado.");
            _bot.UpdateStatus(BotState.NokoBox); // Próximo módulo na sequência
        }

        private bool PerformSpin()
        {
            using var screen = CaptureScreen();
            
            // Verifica se há o botão de girar (ou se há energia)
            if (!DetectAndClick(screen, "roleta.botao_girar.png", "Botão Girar"))
            {
                _bot.Log("Botão de girar não encontrado (Possível falta de energia).");
                return false;
            }

            // Aguarda o início da animação
            Thread.Sleep(1500);

            // Loop de Espera: Enquanto o asset 'roleta_em_execucao' estiver na tela, o bot aguarda
            int waitTimeout = 15; // Máximo de 15 segundos por giro
            while (waitTimeout > 0)
            {
                using var currentScreen = CaptureScreen();
                if (!IsSpinning(currentScreen))
                {
                    _bot.Log("Giro finalizado.");
                    break;
                }
                
                _bot.Log("Roleta em execução... aguardando.");
                Thread.Sleep(1000);
                waitTimeout--;
            }

            // Pequena pausa para processar possíveis popups de prêmios
            Thread.Sleep(2000);
            HandlePostSpinPopups();

            return true;
        }

        private bool IsSpinning(Bitmap screen)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "roleta.roleta_em_execucao.png");
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        private void HandlePostSpinPopups()
        {
            using var screen = CaptureScreen();
            // Tenta fechar prêmios ou avisos de "Level Up" que travam a tela
            DetectAndClick(screen, "btn_coletar_generic.png", "Coleta de prêmio");
        }

        private bool NavigateToRoulette()
        {
            using var screen = CaptureScreen();
            return DetectAndClick(screen, "roleta.acesso_hud.png", "Acesso Roleta");
        }

        private bool ValidateInRoulette()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "roleta.validacao_tela.png");
            using var screen = CaptureScreen();
            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        private void ExitRoulette()
        {
            _bot.Log("Saindo da roleta...");
            using var screen = CaptureScreen();
            DetectAndClick(screen, "btn_voltar_home.png", "Botão Voltar");
            Thread.Sleep(2000);
        }

        // --- MÉTODOS DE MOTOR REUTILIZÁVEIS ---

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