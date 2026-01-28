using System;
using System.Linq;
using AutomacaoApp.Services;
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. PROCESSAMENTO DE ARGUMENTOS
            // Espera o ID da instância como primeiro argumento (ex: AutomacaoApp.exe 5)
            if (args.Length == 0 || !int.TryParse(args[0], out int instanceIndex))
            {
                Console.WriteLine("[ERRO] ID da instância não fornecido. Use: AutomacaoApp.exe <ID>");
                return;
            }

            // 2. INICIALIZAÇÃO DO CONTEXTO
            var memuc = new MemucService();
            var inventory = memuc.GetInventory();
            var currentSpec = inventory.FirstOrDefault(i => i.Index == instanceIndex.ToString());

            if (currentSpec == null)
            {
                Console.WriteLine($"[ERRO] Instância ID {instanceIndex} não encontrada no MEmu.");
                return;
            }

            // Configuração dinâmica do Bot baseada na instância escolhida
            if (string.IsNullOrEmpty(currentSpec.Title))
            {
                Console.WriteLine($"[ERRO] Título da instância ID {instanceIndex} é nulo ou vazio.");
                return;
            }

            var bot = new BotInstance(currentSpec.Title);
            var vision = new VisionEngine();
            var metrics = new MetricsService(bot);

            Console.Title = $"BOT WORKER - Instância: {bot.InstanceName} (ID: {instanceIndex})";
            bot.Log($"=== Iniciando Job Industrial para {bot.InstanceName} ===");

            try
            {
                // 3. ORQUESTRAÇÃO DO CICLO ÚNICO
                var vpn = new VPNService(bot, vision);
                var web = new WebAutomationService(bot, vision);
                var bridge = new AppBridgeService(bot, vision);
                var game = new InGameBonusService(bot, vision);
                var shutdown = new ShutdownService(bot, vision);

                // --- FLUXO OPERACIONAL ---
                
                // Garante que a instância está ligada antes de começar
                if (!currentSpec.IsRunning)
                {
                    bot.Log("Ligando emulador via CLI...");
                    memuc.StartInstance(instanceIndex);
                    System.Threading.Thread.Sleep(20000); // Aguarda boot do Android
                }

                bot.Log("Passo 1: Validando Conexão Segura...");
                vpn.EnsureConnected();

                bot.Log("Passo 2: Verificando integridade do APK...");
                bridge.HandlePotentialCrash();

                bot.Log("Passo 3: Coleta Web (Chrome)...");
                web.EnsureBrowserOpen();
                web.NavigateInAndroid("https://site-recompensa.com/bonus");
                bool webSucesso = web.WaitForWebBonus();
                if (webSucesso) web.FillLoginForm("user", "pass");

                bot.Log("Passo 4: Transição e Coleta In-Game...");
                bridge.ReturnToGame();
                game.ProcessDailyBonus();

                bot.Log("Passo 5: Cleanup e Log de Métricas...");
                metrics.RegistrarSucesso(webSucesso);
                
                // No modo industrial, o bot desliga a instância para poupar RAM para o próximo worker
                shutdown.CleanCleanup();
                memuc.StopInstance(instanceIndex);

                bot.Log($"=== Job Concluído com Sucesso para ID {instanceIndex} ===");
            }
            catch (CriticalException ex)
            {
                bot.Log($"[FALHA CRÍTICA] ID {instanceIndex}: {ex.Message}");
                metrics.RegistrarFalhaCritica();
            }
            catch (Exception ex)
            {
                bot.Log($"[ERRO DESCONHECIDO] ID {instanceIndex}: {ex.Message}");
            }
        }
    }
}