using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    public class MemucService
    {
        // Certifique-se de que este caminho aponta para o seu diretório de instalação do MEmu
        private readonly string _exePath = @"C:\Program Files\Microvirt\MEmu\memuc.exe";

        /// <summary>
        /// Mapeia o inventário completo de instâncias (até 1000+).
        /// </summary>
        public List<EmulatorInstance> GetInventory()
        {
            var instances = new List<EmulatorInstance>();
            
            var startInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                Arguments = "listv2",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Proteção contra falha ao iniciar o processo (Resolve CS8602)
            using var process = Process.Start(startInfo);
            if (process == null) return instances;

            using var reader = process.StandardOutput;
            string output = reader.ReadToEnd();

            // O memuc listv2 entrega: index,title,handle,is_running,pid,process_id
            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var data = line.Split(',');
                if (data.Length >= 5)
                {
                    // Parsing explícito: converte string do terminal para int do modelo
                    instances.Add(new EmulatorInstance
                    {
                        Index = data[0],
                        Title = data[1],
                        IsRunning = data[3] == "1",
                        PID = int.TryParse(data[4], out int pid) ? pid : 0
                    });
                }
            }
            return instances;
        }

        public void StartInstance(int index) => ExecuteCommand($"start -i {index}");
        public void StopInstance(int index) => ExecuteCommand($"stop -i {index}");

        private void ExecuteCommand(string args)
        {
            try 
            {
                Process.Start(new ProcessStartInfo {
                    FileName = _exePath,
                    Arguments = args,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO CLI] Falha ao comandar memuc: {ex.Message}");
            }
        }
    }
}