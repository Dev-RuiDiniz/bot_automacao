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
    public class VPNService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        // Configurações de Resiliência
        private const int MAX_CONNECTION_ATTEMPTS = 3;
        private const int HANDSHAKE_TIMEOUT_MS = 8000;

        public VPNService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        /// <summary>
        /// Garante que a VPN NekoBox esteja conectada. 
        /// Lança CriticalException se houver falha fatal ou timeout.
        /// </summary>
        public void EnsureConnected()
        {
            _bot.Log("--- [SEGURANÇA] Validando Conexão VPN NekoBox ---");

            int attempts = 0;
            bool secure = false;

            while (attempts < MAX_CONNECTION_ATTEMPTS)
            {
                using var screen = CaptureScreen();

                // 1. Verificação de Erro Fatal (Kill Switch Visual)
                // Se o APK do NekoBox mostrar um erro de sistema, paramos tudo.
                if (DetectElement(screen, "vpn.erro_vpn.png"))
                {
                    _bot.Log("[CRÍTICO] Erro fatal detectado na interface do NekoBox.");
                    throw new CriticalException("VPN NekoBox reportou erro de driver ou sistema.");
                }

                // 2. Verifica se o status já é "Conectado"
                if (IsConnected(screen))
                {
                    _bot.Log("Conexão VPN confirmada e segura.");
                    secure = true;
                    break;
                }

                // 3. Tentativa de acionamento
                _bot.Log($"VPN desconectada. Tentativa de ativação {attempts + 1}/{MAX_CONNECTION_ATTEMPTS}...");
                
                if (DetectAndClick(screen, "nekobox.btn_conectar.png", "Botão Conectar"))
                {
                    // Aguarda o handshake do servidor VPN
                    Thread.Sleep(HANDSHAKE_TIMEOUT_MS);
                }
                else
                {
                    _bot.Log("[AVISO] Botão de conexão não localizado na tela.");
                }

                attempts++;
            }

            // 4. Validação Final (Se após as tentativas não conectar, encerra o bot)
            if (!secure)
            {
                _bot.Log("[FALLBACK] Falha persistente na conexão VPN.");
                throw new CriticalException("Não foi possível estabelecer conexão segura após múltiplas tentativas.");
            }
        }

        /// <summary>
        /// Verifica se o asset de status conectado está visível.
        /// </summary>
        private bool IsConnected(Bitmap screen)
        {
            string path = GetAssetPath("nekobox.status_conectado.png");
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        /// <summary>
        /// Apenas detecta um elemento sem clicar (útil para checar erros).
        /// </summary>
        private bool DetectElement(Bitmap screen, string assetName)
        {
            string path = GetAssetPath(assetName);
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        // --- MÉTODOS DE MOTOR (CORE) ---

        private bool DetectAndClick(Bitmap screen, string assetName, string label)
        {
            string path = GetAssetPath(assetName);
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            var location = _vision.FindElement(screen, template);

            if (location != null)
            {
                _bot.Log($"[VPN] Clicando em: {label}");
                ClickAt(location.Value.X, location.Value.Y);
                return true;
            }
            return false;
        }

        private void ClickAt(int x, int y)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            
            // Mapeamento para coordenadas absolutas do Windows (0 a 65535)
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

        private string GetAssetPath(string assetName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
        }
    }
}