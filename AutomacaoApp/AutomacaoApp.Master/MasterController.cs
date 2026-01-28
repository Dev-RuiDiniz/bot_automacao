using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutomacaoApp.Services;
using AutomacaoApp.Models;

namespace AutomacaoApp.Master
{
    /// <summary>
    /// Orquestrador Central responsável pelo escalonamento industrial.
    /// Gerencia fila, concorrência de hardware e integridade do sistema.
    /// </summary>
    public class MasterController
    {
        private readonly MemucService _memuc = new MemucService();
        private readonly ResourceMonitor _resources = new ResourceMonitor();
        
        // Semáforo limitado a 15 instâncias para evitar estouro de memória RAM
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(15, 15);
        
        // Fila thread-safe para armazenar os IDs das instâncias pendentes
        private static readonly ConcurrentQueue<int> _workQueue = new ConcurrentQueue<int>();

        public async Task RunIndustrialScaleAsync()
        {
            // 1. MAPEAMENTO: Obtém todas as instâncias configuradas no MEmu
            var inventory = _memuc.GetInventory();
            foreach (var instance in inventory)
            {
                if (int.TryParse(instance.Index, out int index))
                {
                    _workQueue.Enqueue(index);
                }
            }

            Console.WriteLine($"=== MASTER: {_workQueue.Count} INSTÂNCIAS NA FILA ===");

            // 2. PARALELISMO: Inicia 15 Workers assíncronos para processar a fila
            var workerTasks = new List<Task>();
            for (int i = 0; i < 15; i++)
            {
                workerTasks.Add(Task.Run(async () => await WorkerLoopAsync()));
            }

            // Aguarda a conclusão de todas as instâncias na fila
            await Task.WhenAll(workerTasks);
            Console.WriteLine("\n=== PROCESSO INDUSTRIAL CONCLUÍDO! ===");
        }

        private async Task WorkerLoopAsync()
        {
            while (_workQueue.TryDequeue(out int instanceIndex))
            {
                // --- TRAVA DE SEGURANÇA: MONITOR DE RECURSOS ---
                // Se a CPU global estiver acima de 90%, aguarda o alívio do hardware
                while (_resources.IsSystemOverloaded(90.0f))
                {
                    Console.WriteLine($"[THROTTLE] CPU Crítica (>90%). Aguardando 10s para estabilizar...");
                    await Task.Delay(10000);
                }

                // Solicita entrada no semáforo (ocupa 1 dos 15 slots)
                await _semaphore.WaitAsync();
                
                try
                {
                    // --- FASE 1: GERENCIAMENTO DE CICLO DE VIDA (BOOT) ---
                    Console.WriteLine($"[LIFECYCLE] Ligando Instância {instanceIndex}...");
                    _memuc.StartInstance(instanceIndex);
                    
                    // Delay necessário para o Android finalizar o boot e serviços de rede
                    await Task.Delay(20000); 

                    // --- FASE 2: EXECUÇÃO DO WORKER (JOB ISOLADO) ---
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "AutomacaoApp.exe",
                        Arguments = instanceIndex.ToString(),
                        CreateNoWindow = false,
                        UseShellExecute = false
                    };

                    using var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        // Monitoramento de Timeout: Se o bot travar por mais de 10 min, é finalizado
                        var exitTask = process.WaitForExitAsync();
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10));

                        var completedTask = await Task.WhenAny(exitTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            Console.WriteLine($"[CRITICAL] Timeout no ID {instanceIndex}. Forçando interrupção do processo.");
                            process.Kill(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO] Falha crítica na execução do ID {instanceIndex}: {ex.Message}");
                }
                finally
                {
                    // --- FASE 3: GERENCIAMENTO DE CICLO DE VIDA (SHUTDOWN) ---
                    // Garante o fechamento da instância para liberar recursos de RAM/VRAM
                    Console.WriteLine($"[LIFECYCLE] Finalizando ID {instanceIndex} e liberando slot.");
                    _memuc.StopInstance(instanceIndex);
                    
                    // Libera o slot no semáforo para a próxima instância da fila
                    _semaphore.Release();
                    
                    // Cool-down de 2 segundos para evitar picos de I/O no disco (SSD/HD)
                    await Task.Delay(2000); 
                }
            }
        }
    }
}