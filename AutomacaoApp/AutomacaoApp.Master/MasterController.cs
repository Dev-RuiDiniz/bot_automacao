using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AutomacaoApp.Services;
using AutomacaoApp.Models;

namespace AutomacaoApp.Master
{
    public class MasterController
    {
        private readonly MemucService _memuc = new MemucService();
        private readonly int _maxConcurrent = 5; // Limite de bots rodando ao mesmo tempo
        private readonly string _botExePath = "AutomacaoApp.exe"; // Caminho do Worker

        public void RunIndustrialScale()
        {
            Console.WriteLine("=== MASTER CONTROLLER: INICIANDO ESCALONAMENTO ===");
            
            // 1. Mapeia todas as instâncias existentes
            var totalInventory = _memuc.GetInventory();
            Console.WriteLine($"Total de instâncias mapeadas: {totalInventory.Count}");

            // 2. Filtra quem precisa rodar (Ex: Você pode filtrar por nome ou status no JSON)
            var queue = new Queue<EmulatorInstance>(totalInventory);
            var activeProcesses = new List<Process>();

            while (queue.Count > 0 || activeProcesses.Count > 0)
            {
                // Limpa processos que já terminaram
                activeProcesses.RemoveAll(p => p.HasExited);

                // 3. Gerenciamento de Carga (Slot Management)
                while (activeProcesses.Count < _maxConcurrent && queue.Count > 0)
                {
                    var nextInstance = queue.Dequeue();
                    Console.WriteLine($"[MASTER] Disparando Worker para ID {nextInstance.Index} ({nextInstance.Title})");

                    // Dispara o Worker passando o Index como argumento
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = _botExePath,
                        Arguments = nextInstance.Index.ToString(),
                        UseShellExecute = true, // Abre em uma nova janela para monitoramento visual
                        CreateNoWindow = false
                    });

                    if (process != null) activeProcesses.Add(process);
                    
                    // Delay de segurança para não sobrecarregar o barramento de disco no boot
                    Thread.Sleep(5000); 
                }

                // Monitoramento no console
                Console.Write($"\r[STATUS] Ativos: {activeProcesses.Count} | Restantes na fila: {queue.Count}      ");
                Thread.Sleep(2000);
            }

            Console.WriteLine("\n=== TODAS AS INSTÂNCIAS FORAM PROCESSADAS ===");
        }
    }
}