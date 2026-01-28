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
    /// Gerencia o ciclo de vida, troca de contexto entre apps e monitoramento de métricas.
    /// </summary>
    class Program
    {
        // Instanciação dos motores base com identificador único da instância
        private static readonly BotInstance _bot = new BotInstance("MEmu_Automator_V1");
        private static readonly VisionEngine _vision = new VisionEngine();

        static void Main(string[] args)
        {
            // Configuração visual do console
            Console.Title = $"Iniciado: {_bot.InstanceName} | Bot Híbrido Ativo";
            _bot.Log("=== Sistema Orquestrador Iniciado ===");

            // Inicialização dos Serviços (Dependency Injection manual)
            var vpnService = new VPNService(_bot, _vision);
            var webService = new WebAutomationService(_bot, _vision);
            var bridge = new AppBridgeService(_bot, _vision);
            var gameBonus = new InGameBonusService(_bot, _vision);
            var shutdown = new ShutdownService(_bot, _vision);
            var metrics = new MetricsService(_bot); // Novo: Dash de Monitoramento JSON

            // Loop Infinito de Operação
            while (true)
            {
                try
                {
                    _bot.Log(">>> Iniciando novo ciclo de trabalho...");

                    // 1. CAMADA DE SEGURANÇA (VPN)
                    // Verifica se o túnel está ativo antes de qualquer exposição de tráfego
                    vpnService.EnsureConnected();
                    
                    // 2. CAMADA WEB (Chrome Android)
                    // Acessa bônus externos e valida presença de CAPTCHA
                    webService.EnsureBrowserOpen();
                    webService.NavigateInAndroid("https://site-recompensa.com/bonus");
                    
                    bool bonusWebColetado = false;
                    if (webService.WaitForWebBonus())
                    {
                        webService.FillLoginForm("usuario_exemplo", "senha_segura");
                        bonusWebColetado = true;
                    }

                    // 3. CAMADA DE TRANSIÇÃO (Bridge)
                    // Alterna o foco do Chrome para o APK do Jogo via 'Apps Recentes'
                    bridge.ReturnToGame();

                    // 4. CAMADA IN-GAME (Tarefa Nativa)
                    // Executa a coleta interna e validações visuais de sucesso
                    gameBonus.ProcessDailyBonus();
                    
                    // 5. FINALIZAÇÃO E LIMPEZA
                    // Fecha apps para liberar RAM e volta para a tela home do MEmu
                    shutdown.CleanCleanup();

                    // Atualiza métricas no dashboard JSON
                    metrics.RegistrarSucesso(foiBonus: bonusWebColetado);

                    _bot.Log(">>> Ciclo concluído com sucesso. Entrando em repouso.");
                    AguardarProximoCiclo();
                }
                catch (CriticalException ex)
                {
                    // Erros que representam risco à conta (Captcha, VPN Down)
                    _bot.Log($"[FATAL] {ex.Message}");
                    metrics.RegistrarFalhaCritica();
                    _bot.Log("Execução interrompida por segurança. Verifique o emulador.");
                    break; // Sai do loop infinito
                }
                catch (LightException ex)
                {
                    // Erros de interface (Lag, Asset não encontrado)
                    _bot.Log($"[RECUPERAÇÃO] {ex.Message}. Reiniciando fluxo em 10s...");
                    Thread.Sleep(10000);
                    // O loop continuará, tentando novamente do zero
                }
                catch (Exception ex)
                {
                    // Erros imprevistos de sistema
                    _bot.Log($"[SISTEMA] Erro não tratado: {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// Implementa o tempo de espera "humano" com variação aleatória.
        /// Evita padrões de tempo fixo que facilitam a detecção por servidores.
        /// </summary>
        private static void AguardarProximoCiclo()
        {
            Random rnd = new Random();
            int minutos = rnd.Next(15, 22); // Entre 15 e 21 minutos
            
            _bot.Log($"Standby ativo: Próxima execução em aproximadamente {minutos} minutos.");

            for (int i = minutos; i > 0; i--)
            {
                Console.Write($"\rTempo restante: {i:D2} min | Status: Aguardando...   ");
                Thread.Sleep(60000); // Espera 1 minuto
            }
            Console.WriteLine("\nTempo de espera concluído!");
        }
    }
}