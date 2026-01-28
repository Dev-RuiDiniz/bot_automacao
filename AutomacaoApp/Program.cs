using System;
using System.Threading;
using AutomacaoApp.Services;
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp
{
    /// <summary>
    /// Orquestrador Principal do Bot Híbrido (MEmu).
    /// Centraliza a lógica de decisão, tratamento de erros e métricas.
    /// </summary>
    class Program
    {
        // Instanciação dos motores base (Core)
        private static readonly BotInstance _bot = new BotInstance("MEmu_Automator_V1");
        private static readonly VisionEngine _vision = new VisionEngine();

        static void Main(string[] args)
        {
            // Configuração de Título com a propriedade InstanceName do BotInstance
            Console.Title = $"Iniciado: {_bot.InstanceName} | Bot Híbrido Ativo";
            _bot.Log("=== Sistema Orquestrador Iniciado ===");

            // Inicialização dos Serviços (Dependency Injection manual)
            var vpnService = new VPNService(_bot, _vision);
            var webService = new WebAutomationService(_bot, _vision);
            var bridge = new AppBridgeService(_bot, _vision);
            var gameBonus = new InGameBonusService(_bot, _vision);
            var shutdown = new ShutdownService(_bot, _vision);
            var metrics = new MetricsService(_bot); 

            // Loop Infinito de Operação
            while (true)
            {
                try
                {
                    _bot.Log(">>> Iniciando novo ciclo de trabalho...");

                    // 1. CAMADA DE SEGURANÇA (VPN)
                    // Garante que o IP está protegido antes de abrir o browser ou jogo
                    vpnService.EnsureConnected();

                    // 2. CAMADA DE SAÚDE (Crash Check)
                    // Verifica se o jogo crashou ou está em tela preta antes de começar
                    bridge.HandlePotentialCrash();
                    
                    // 3. CAMADA WEB (Chrome Android)
                    // Realiza a coleta do bônus diário externo
                    webService.EnsureBrowserOpen();
                    webService.NavigateInAndroid("https://site-recompensa.com/bonus");
                    
                    bool bonusWebColetado = false;
                    if (webService.WaitForWebBonus())
                    {
                        webService.FillLoginForm("usuario_bot", "senha_segura");
                        bonusWebColetado = true;
                    }

                    // 4. CAMADA DE TRANSIÇÃO (Bridge)
                    // Alterna o foco do navegador para o aplicativo do jogo
                    bridge.ReturnToGame();

                    // 5. CAMADA IN-GAME (Tarefa Nativa)
                    // Coleta recompensas internas e valida bônus coletado
                    gameBonus.ProcessDailyBonus();
                    
                    // 6. FINALIZAÇÃO E LIMPEZA
                    // Encerra processos para evitar lentidão no MEmu e volta à Home
                    shutdown.CleanCleanup();

                    // 7. DASHBOARD (Persistência JSON)
                    // Registra o sucesso e atualiza o monitoramento.json
                    metrics.RegistrarSucesso(foiBonus: bonusWebColetado);

                    _bot.Log(">>> Ciclo concluído com sucesso. Entrando em modo repouso.");
                    AguardarProximoCiclo();
                }
                catch (CriticalException ex)
                {
                    // Falhas de segurança (Captcha/VPN offline) -> Para o Bot imediatamente
                    _bot.Log($"[FATAL] {ex.Message}");
                    metrics.RegistrarFalhaCritica();
                    _bot.Log("Encerrando por segurança. Resolva o problema manualmente no emulador.");
                    break; 
                }
                catch (LightException ex)
                {
                    // Falhas de interface (Lag/Asset não encontrado) -> Tenta recuperar no próximo ciclo
                    _bot.Log($"[RECUPERAÇÃO] {ex.Message}. Tentando reinicializar em 10s...");
                    Thread.Sleep(10000);
                }
                catch (Exception ex)
                {
                    // Erros imprevistos de código ou sistema operacional
                    _bot.Log($"[SISTEMA] Erro crítico não tratado: {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// Gera uma pausa aleatória entre ciclos para mimetizar comportamento humano.
        /// </summary>
        private static void AguardarProximoCiclo()
        {
            Random rnd = new Random();
            int minutos = rnd.Next(15, 23); // Varia entre 15 e 22 minutos
            
            _bot.Log($"Standby: Próxima verificação em {minutos} minutos.");

            for (int i = minutos; i > 0; i--)
            {
                Console.Write($"\r[COUNTDOWN] Retomando em: {i:D2} min | Status: Idle...   ");
                Thread.Sleep(60000); // 1 minuto
            }
            Console.WriteLine("\n[AVISO] Reiniciando ciclo operacional...");
        }
    }
}