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
        
        // Controle de Ciclos: Repete a entrada na tela de amigos X vezes
        private int _cycleCount = 0;
        private const int MAX_CYCLES = 3;

        public FriendsModuleService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        /// <summary>
        /// Ponto de entrada do módulo. Gerencia a navegação e a repetição de ciclos.
        /// </summary>
        public void Execute()
        {
            _bot.Log($"--- Módulo Amigos: Ciclo {_cycleCount + 1}/{MAX_CYCLES} ---");

            // 1. Navegação: Tenta entrar na tela de amigos
            if (NavigateToFriendsScreen())
            {
                // 2. Processamento: Coleta e envia presentes na lista atual
                ProcessFriendsList();

                _cycleCount++;

                // 3. Orquestração: Verifica se deve repetir ou seguir para o próximo módulo
                if (_cycleCount < MAX_CYCLES)
                {
                    _bot.Log($"Ciclo {_cycleCount} finalizado. Reiniciando para garantir limpeza...");
                    CloseFriendsScreen();
                    
                    // Mantém o estado atual para que o loop principal o execute novamente
                    _bot.UpdateStatus(BotState.FriendsModule);
                }
                else
                {
                    _bot.Log("Todos os 3 ciclos de amigos concluídos.");
                    _cycleCount = 0; // Reseta para a próxima execução global do bot
                    _bot.UpdateStatus(BotState.Roulette);
                }
            }
            else
            {
                _bot.Log("Não foi possível navegar para a tela de amigos. Abortando ciclo.");
                _bot.UpdateStatus(BotState.Roulette); // Pula para evitar travamento
            }
        }

        /// <summary>
        /// Realiza o processamento contínuo da lista visível.
        /// </summary>
        private void ProcessFriendsList()
        {
            bool hasMoreActions = true;
            int maxAttempts = 30;      // Limite de frames analisados
            int interactions = 0;     // Contador de cliques reais
            int maxInteractions = 25; // Circuit Breaker: evita cliques infinitos por erro visual

            while (hasMoreActions && maxAttempts > 0)
            {
                if (interactions >= maxInteractions)
                {
                    _bot.Log("[Segurança] Limite de interações atingido. Possível falso positivo visual.");
                    break;
                }

                using var screen = CaptureScreen();

                // Verifica se a lista está vazia através de um marcador visual
                if (CheckIfListIsEmpty(screen))
                {
                    _bot.Log("Lista de presentes vazia.");
                    break;
                }

                // Prioridade 1: Recolher presente
                bool collected = DetectAndClick(screen, "amigos.botao_recolher_presente.png", "Coletar");
                if (collected) interactions++;

                // Prioridade 2: Enviar presente (só tenta se não coletou para manter o foco da UI)
                if (!collected)
                {
                    bool sent = DetectAndClick(screen, "amigos.botao_enviar_presente.png", "Enviar");
                    if (sent) interactions++;

                    if (!sent)
                    {
                        _bot.Log("Nenhuma ação detectada na tela atual.");
                        hasMoreActions = false; 
                    }
                }

                maxAttempts--;
                Thread.Sleep(850); // Delay para a animação do botão sumindo
            }
        }

        /// <summary>
        /// Navega da Home para a Tela de Amigos e valida a transição.
        /// </summary>
        private bool NavigateToFriendsScreen()
        {
            using var screen = CaptureScreen();
            if (DetectAndClick(screen, "amigos.botao_amigos.png", "Menu Amigos"))
            {
                Thread.Sleep(3000); // Tempo de carregamento da interface
                return ValidateScreen("amigos.tela_validacao.png");
            }
            return false;
        }

        private void CloseFriendsScreen()
        {
            using var screen = CaptureScreen();
            DetectAndClick(screen, "btn_fechar_generic.png", "Fechar Janela");
            Thread.Sleep(1500);
        }

        // --- MÉTODOS AUXILIARES DE MOTOR ---

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

        private bool CheckIfListIsEmpty(Bitmap screen)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "amigos.sem_presentes.png");
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        private bool ValidateScreen(string assetName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return true; // Se não houver asset, assume que entrou

            using var screen = CaptureScreen();
            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        private void ClickAt(int x, int y)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            // Conversão para o sistema de coordenadas absoluto do Windows (0-65535)
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