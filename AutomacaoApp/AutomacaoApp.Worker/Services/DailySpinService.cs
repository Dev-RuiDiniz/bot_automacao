using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Diagnostics;
using AutomacaoApp.Core;
using AutomacaoApp.Enums;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    public class DailySpinService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly MemucService _memuc;

        public DailySpinService(BotInstance bot, VisionEngine vision, MemucService memuc)
        {
            _bot = bot;
            _vision = vision;
            _memuc = memuc;
        }

        public void Execute()
        {
            _bot.Log($"[Módulo DailySpin] Iniciando na Instância {_bot.Index}");

            // 1. Obtém o Handle da janela para captura isolada em background
            IntPtr handle = Process.GetProcessById(_bot.PID).MainWindowHandle;

            // 2. Loop de detecção e interação
            using (var screen = _vision.CaptureProcessWindow(handle))
            {
                if (DetectAndClick(screen, "popup_roleta_disponivel.png", "Aviso de Roleta"))
                {
                    Thread.Sleep(3000); // Aguarda abertura da roleta
                    
                    using var secondScreen = _vision.CaptureProcessWindow(handle);
                    if (DetectAndClick(secondScreen, "btn_girar.png", "Botão Girar"))
                    {
                        _bot.Log("Giro iniciado! Aguardando animação...");
                        Thread.Sleep(10000); 

                        using var finalScreen = _vision.CaptureProcessWindow(handle);
                        DetectAndClick(finalScreen, "btn_coletar_recompensa.png", "Coleta de Prêmio");
                    }
                }
                else
                {
                    _bot.Log("Roleta não disponível no momento.");
                }
            }

            _bot.UpdateStatus(BotState.FriendsModule);
        }

        /// <summary>
        /// Detecta elemento na imagem e envia clique via ADB se encontrado.
        /// </summary>
        private bool DetectAndClick(Bitmap screen, string templateName, string desc)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", templateName);
            if (!File.Exists(path)) 
            {
                _bot.Log($"[ERRO] Asset não encontrado: {templateName}");
                return false;
            }

            using var template = new Bitmap(path);
            var point = _vision.FindElement(screen, template);

            if (point != null)
            {
                _bot.Log($"Elemento {desc} localizado em {point.Value.X},{point.Value.Y}.");
                
                // Envia o clique para o MEmu usando as coordenadas internas da janela
                _memuc.SendClick(_bot.Index, point.Value.X, point.Value.Y);
                return true;
            }
            return false;
        }
    }
}