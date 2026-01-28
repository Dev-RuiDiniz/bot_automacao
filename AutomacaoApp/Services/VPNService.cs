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
    public class VPNService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly IInputSimulator _input;

        public VPNService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot;
            _vision = vision;
            _input = new InputSimulator();
        }

        public void EnsureConnected()
        {
            _bot.Log("--- Verificando Status VPN (NekoBox) ---");

            using var screen = CaptureScreen();

            // 1. Verifica se já está conectado (procurando um asset de "status_conectado")
            if (IsConnected(screen))
            {
                _bot.Log("VPN NekoBox já está ativa. Prosseguindo...");
                return;
            }

            // 2. Se não estiver conectado, tenta clicar no botão de conexão
            _bot.Log("VPN desconectada. Tentando ativar...");
            if (DetectAndClick(screen, "nekobox.btn_conectar.png", "Botão Conectar VPN"))
            {
                // Aguarda o túnel ser estabelecido
                Thread.Sleep(5000);
                
                using var checkScreen = CaptureScreen();
                if (IsConnected(checkScreen))
                {
                    _bot.Log("VPN conectada com sucesso!");
                }
                else
                {
                    _bot.Log("[AVISO] Tentativa de conexão falhou ou demora na resposta.");
                }
            }
        }

        private bool IsConnected(Bitmap screen)
        {
            // Procura por um indicador visual de que a VPN está ON (ex: ícone verde ou botão 'Desconectar')
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "nekobox.status_conectado.png");
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            return _vision.FindElement(screen, template) != null;
        }

        // --- MÉTODOS DE MOTOR PADRONIZADOS ---
        private bool DetectAndClick(Bitmap screen, string assetName, string label)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", assetName);
            if (!File.Exists(path)) return false;

            using var template = new Bitmap(path);
            var location = _vision.FindElement(screen, template);

            if (location != null)
            {
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