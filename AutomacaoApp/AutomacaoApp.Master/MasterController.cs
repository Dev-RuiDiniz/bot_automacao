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
        
        // Semáforo: Controla quantas instâncias rodam ao mesmo tempo (Slots de hardware)
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(15, 15);
        
        // Fila Concorrente: Armazena os IDs das 1000 instâncias de forma segura
        private static readonly ConcurrentQueue<int> _workQueue = new ConcurrentQueue<int>();

        public async Task RunIndustrialScaleAsync()
        {
            // 1. Carrega o inventário e popula a fila
            var inventory = _memuc.GetInventory();
            foreach (var instance in inventory)
            {
                if (int.TryParse(instance.Index, out int index))
                {
                    _workQueue.Enqueue(index);
                }
                else
                {
                    Console.WriteLine($"[ERRO] Não foi possível converter o Index '{instance.Index}' para int.");
                }
            }

            Console.WriteLine($"=== FILA INICIADA: {_workQueue.Count} TAREFAS AGUARDANDO ===");

            // 2. Cria os "Processadores" que ficarão consumindo a fila
            // Rodamos 15 workers simultâneos (limitados pelo semáforo)
            var workerTasks = new List<Task>();
            for (int i = 0; i < 15; i++)
            {
                workerTasks.Add(Task.Run(async () => await WorkerLoopAsync()));
            }

            // Aguarda o esvaziamento da fila
            await Task.WhenAll(workerTasks);
            
            Console.WriteLine("\n=== TODAS AS 1000 INSTÂNCIAS FORAM PROCESSADAS! ===");
        }

        private async Task WorkerLoopAsync()
        {
            // O loop continua enquanto houver itens na fila
            while (_workQueue.TryDequeue(out int instanceIndex))
            {
                await _semaphore.WaitAsync();
                try
                {
                    Console.WriteLine($"[EXECUTANDO] Slot ocupado pelo ID: {instanceIndex}");
                    
                    // Dispara o Worker (Job de Ciclo Único)
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "AutomacaoApp.exe",
                        Arguments = instanceIndex.ToString(),
                        CreateNoWindow = false
                    };

                    using var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        // Aguarda a conclusão ou define um timeout (8 min)
                        var exitTask = process.WaitForExitAsync();
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(8));

                        var completedTask = await Task.WhenAny(exitTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            Console.WriteLine($"[TIMEOUT] ID {instanceIndex} travou. Matando processo...");
                            process.Kill(true);
                        }
                    }
                }
                finally
                {
                    // Libera o semáforo para o próximo da fila
                    _semaphore.Release();
                    Console.WriteLine($"[SLOT LIBERADO] ID {instanceIndex} finalizou.");
                }
            }
        }
    }
}