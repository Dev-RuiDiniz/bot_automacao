using System;
using System.Threading;
using AutomacaoApp.Services;
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp
{
    class Program
    {
        // SOLUÇÃO CS7036: Adicionado argumento "MEmu_Bot"
        private static readonly BotInstance _bot = new BotInstance("MEmu_Automator_V1");
        private static readonly VisionEngine _vision = new VisionEngine();

        static void Main(string[] args)
        {
            Console.Title = "Bot Híbrido MEmu - Ativo";
            _bot.Log("Iniciando orquestrador...");

            var vpnService = new VPNService(_bot, _vision);
            var webService = new WebAutomationService(_bot, _vision);
            var bridge = new AppBridgeService(_bot, _vision);
            var gameBonus = new InGameBonusService(_bot, _vision);
            var shutdown = new ShutdownService(_bot, _vision);

            while (true)
            {
                try
                {
                    _bot.Log(">>> Iniciando novo ciclo...");

                    // 1. VPN
                    vpnService.EnsureConnected();
                    
                    // 2. WEB
                    webService.EnsureBrowserOpen();
                    webService.NavigateInAndroid("https://site-de-exemplo.com/bonus");
                    if (webService.WaitForWebBonus())
                    {
                        webService.FillLoginForm("usuario_bot", "senha_segura");
                    }

                    // 3. BRIDGE E JOGO
                    bridge.ReturnToGame();
                    gameBonus.ProcessDailyBonus();
                    
                    // 4. CLEANUP
                    shutdown.CleanCleanup();

                    _bot.Log(">>> Ciclo concluído. Aguardando descanso...");
                    AguardarProximoCiclo();
                }
                catch (CriticalException ex)
                {
                    _bot.Log($"[PARADA CRÍTICA] {ex.Message}");
                    break; 
                }
                catch (LightException ex)
                {
                    _bot.Log($"[RECUPERAÇÃO] {ex.Message}. Reiniciando em 10s...");
                    Thread.Sleep(10000);
                }
                catch (Exception ex)
                {
                    _bot.Log($"[ERRO DESCONHECIDO] {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }

        private static void AguardarProximoCiclo()
        {
            int minutos = new Random().Next(15, 22);
            for (int i = minutos; i > 0; i--)
            {
                Console.Write($"\rPróximo ciclo em: {i} minutos...   ");
                Thread.Sleep(60000);
            }
            Console.WriteLine();
        }
    }
}