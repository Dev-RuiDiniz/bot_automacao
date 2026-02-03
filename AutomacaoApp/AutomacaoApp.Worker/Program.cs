using System;
using System.Linq;
using System.Threading; // Necessário para o Thread.Sleep
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
            // ==========================================================
            // 1. PROCESSAMENTO DE ARGUMENTOS
            // ==========================================================
            // O Worker espera receber o ID (Index) da instância do emulador via linha de comando.
            if (args.Length == 0 || !int.TryParse(args[0], out int instanceIndex))
            {
                Console.WriteLine("[ERRO] ID da instância não fornecido. Use: AutomacaoApp.exe <ID>");
                return;
            }

            // ==========================================================
            // 2. INICIALIZAÇÃO DO CONTEXTO E DADOS TÉCNICOS
            // ==========================================================
            var memuc = new MemucService();
            
            // Busca a lista de emuladores para validar se o ID existe e pegar dados (PID, Título)
            var inventory = memuc.GetInventory();
            
            // Converte instanceIndex (int) para string para comparar com EmulatorInstance.Index
            var currentSpec = inventory.FirstOrDefault(i => i.Index == instanceIndex.ToString());

            if (currentSpec == null)
            {
                Console.WriteLine($"[ERRO] Instância ID {instanceIndex} não encontrada no MEmu.");
                return;
            }

            // Validação de segurança para garantir que temos um título
            if (string.IsNullOrEmpty(currentSpec.Title))
            {
                Console.WriteLine($"[ERRO] Título da instância ID {instanceIndex} é nulo ou vazio.");
                return;
            }

            // --- CORREÇÃO APLICADA AQUI ---
            // O construtor do BotInstance exige (Name, Index, PID).
            // Estamos passando os dados vindos do objeto 'currentSpec' (EmulatorInstance).
            var bot = new BotInstance(currentSpec.Title, currentSpec.Index, currentSpec.PID);
            
            // Inicialização dos serviços de suporte
            var vision = new VisionEngine();
            var metrics = new MetricsService(bot);

            // --- CORREÇÃO APLICADA AQUI ---
            // A propriedade correta no BotInstance é 'Name', e não 'InstanceName'.
            Console.Title = $"BOT WORKER - Instância: {bot.Name} (ID: {instanceIndex})";
            bot.Log($"=== Iniciando Job Industrial para {bot.Name} ===");

            try
            {
                // ==========================================================
                // 3. ORQUESTRAÇÃO DO CICLO ÚNICO (WORKER)
                // ==========================================================
                
                // Injeção de dependência manual (Bot + Vision) para os serviços operacionais
                var vpn = new VPNService(bot, vision);
                var web = new WebAutomationService(bot, vision);
                var bridge = new AppBridgeService(bot, vision);
                var game = new InGameBonusService(bot, vision);
                var shutdown = new ShutdownService(bot, vision);

                // --- FLUXO OPERACIONAL ---

                // Passo 0: Garantir que o emulador está rodando
                if (!currentSpec.IsRunning)
                {
                    bot.Log("Ligando emulador via CLI...");
                    memuc.StartInstance(instanceIndex);
                    
                    // Aguarda o boot do Android (ajustar conforme velocidade da máquina)
                    Thread.Sleep(20000); 
                    
                    // NOTA: Ao iniciar, o PID muda. Em um cenário ideal, deveríamos
                    // atualizar o bot.PID aqui consultando o memuc novamente.
                }

                // Passo 1: Segurança de Rede
                bot.Log("Passo 1: Validando Conexão Segura...");
                vpn.EnsureConnected();

                // Passo 2: Verificação do App (Crash loop / Instalação)
                bot.Log("Passo 2: Verificando integridade do APK...");
                bridge.HandlePotentialCrash();

                // Passo 3: Coleta via Web (Navegador Chrome no Android)
                bot.Log("Passo 3: Coleta Web (Chrome)...");
                web.EnsureBrowserOpen();
                web.NavigateInAndroid("https://site-recompensa.com/bonus");
                
                bool webSucesso = web.WaitForWebBonus();
                if (webSucesso) 
                {
                    web.FillLoginForm("user", "pass");
                }

                // Passo 4: Coleta In-Game (App Nativo)
                bot.Log("Passo 4: Transição e Coleta In-Game...");
                bridge.ReturnToGame();
                game.ProcessDailyBonus();

                // Passo 5: Finalização e Métricas
                bot.Log("Passo 5: Cleanup e Log de Métricas...");
                metrics.RegistrarSucesso(webSucesso);
                
                // No modo industrial, o bot desliga a instância para liberar RAM
                shutdown.CleanCleanup();
                memuc.StopInstance(instanceIndex);

                bot.Log($"=== Job Concluído com Sucesso para ID {instanceIndex} ===");
            }
            catch (CriticalException ex)
            {
                // Erros previstos na lógica de negócio (ex: conta banida, internet caiu)
                bot.Log($"[FALHA CRÍTICA] ID {instanceIndex}: {ex.Message}");
                metrics.RegistrarFalhaCritica();
            }
            catch (Exception ex)
            {
                // Erros imprevistos (bugs, exceções de sistema)
                bot.Log($"[ERRO DESCONHECIDO] ID {instanceIndex}: {ex.Message}");
                // Opcional: Salvar stacktrace em arquivo de log
            }
        }
    }
}