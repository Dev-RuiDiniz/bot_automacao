using WindowsInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutomacaoApp.Services
{
    public class EmulatorManager
    {
        private readonly string _memucPath = @"C:\Program Files\Microvirt\MEmu\memuc.exe";

        /// <summary>
        /// Mapeia todas as instâncias disponíveis no sistema.
        /// </summary>
        public List<EmulatorInstance> GetAllInstances()
        {
            var list = new List<EmulatorInstance>();
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _memucPath,
                    Arguments = "listv2",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parsing da saída CSV do memuc
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    list.Add(new EmulatorInstance {
                        Index = parts[0],
                        Title = parts[1],
                        IsRunning = parts[3] == "1"
                    });
                }
            }

            return list;
        }
    }

    public class EmulatorInstance
    {
        public string? Index { get; set; }
        public string? Title { get; set; }
        public bool IsRunning { get; set; }
    }
}
