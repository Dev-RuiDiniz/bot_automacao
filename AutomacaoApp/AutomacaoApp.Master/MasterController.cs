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
    public class MasterController
    {
        private readonly MemucService _memuc = new MemucService();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(15, 15);
        private static readonly ConcurrentQueue<int> _workQueue = new ConcurrentQueue<int>();

        public async Task RunIndustrialScaleAsync()
        {
            var inventory = _memuc.GetInventory();
            foreach (var instance in inventory)
            {
                // Converter string para int antes de enfileirar
                if (int.TryParse(instance.Index, out int index))
                {
                    _workQueue.Enqueue(index);
                }
            }

            Console.WriteLine($"=== MASTER: {_workQueue.Count} INSTÂNCIAS NA FILA ===");

            var workerTasks = new List<Task>();
            for (int i = 0; i < 15; i++)
            {
                workerTasks.Add(Task.Run(async () => await WorkerLoopAsync()));
            }

            await Task.WhenAll(workerTasks);
            Console.WriteLine("\n=== PROCESSO INDUSTRIAL CONCLUÍDO! ===");
        }

        private async Task WorkerLoopAsync()
        {
            while (_workQueue.TryDequeue(out int instanceIndex))
            {
                await _semaphore.WaitAsync();
                try
                {
                    // --- FASE 1: GERENCIAMENTO DE CICLO DE VIDA (BOOT) ---
                    Console.WriteLine($"[LIFECYCLE] Ligando Instância {instanceIndex}...");
                    _memuc.StartInstance(instanceIndex);
                    
                    // Aguarda estabilização do Android antes de injetar o bot
                    await Task.Delay(20000); 

                    // --- FASE 2: EXECUÇÃO DO WORKER ---
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
                        var exitTask = process.WaitForExitAsync();
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10)); // Timeout estendido para segurança

                        var completedTask = await Task.WhenAny(exitTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            Console.WriteLine($"[CRITICAL] Timeout no ID {instanceIndex}. Forçando interrupção.");
                            process.Kill(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO] Falha na execução do ID {instanceIndex}: {ex.Message}");
                }
                finally
                {
                    // --- FASE 3: GERENCIAMENTO DE CICLO DE VIDA (SHUTDOWN) ---
                    Console.WriteLine($"[LIFECYCLE] Desligando Instância {instanceIndex} para liberar RAM.");
                    _memuc.StopInstance(instanceIndex);
                    
                    _semaphore.Release();
                    await Task.Delay(2000); // Cool-down para o disco rígido
                }
            }
        }
    }
}