using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutomacaoApp.Services;
using AutomacaoApp.Models; // Usaremos apenas este para o objeto de dados

namespace AutomacaoApp.Master
{
    public class MasterController
    {
        private readonly MemucService _memuc = new MemucService();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(15, 15);

        public async Task RunIndustrialScaleAsync()
        {
            Console.WriteLine("=== MASTER ORCHESTRATOR: CORREÇÃO DE NAMESPACE ATIVA ===");
            
            // Aqui garantimos que a lista retornada seja do tipo Services.EmulatorInstance
            List<AutomacaoApp.Services.EmulatorInstance> inventory = _memuc.GetInventory();
            var tasks = new List<Task>();

            foreach (var instance in inventory)
            {
                tasks.Add(ProcessInstanceAsync(instance));
            }

            await Task.WhenAll(tasks);
        }

        private async Task ProcessInstanceAsync(AutomacaoApp.Services.EmulatorInstance instance)
        {
            await _semaphore.WaitAsync();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "AutomacaoApp.exe",
                    Arguments = instance.Index?.ToString(),
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    // WaitForExitAsync requer .NET 5+ ou superior
                    await process.WaitForExitAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}