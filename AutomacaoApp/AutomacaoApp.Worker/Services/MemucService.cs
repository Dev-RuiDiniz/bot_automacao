using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutomacaoApp.Worker.Models;

namespace AutomacaoApp.Services
{
    public class MemucService
    {
        // Caminho padrão do executável do MEmu
        private readonly string _exePath = @"C:\Program Files\Microvirt\MEmu\memuc.exe";
        
        // Se precisar do InputSimulator, declare aqui:
        // private readonly IInputSimulator _inputSimulator;

        public MemucService()
        {
            // Se for usar InputSimulator:
            // _inputSimulator = new InputSimulator();
        }

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

            using (var process = Process.Start(startInfo))
            {
                // REMOVA ESTAS LINHAS DAQUI:
                // using WindowsInput;
                // using WindowsInput.Native;
                
                if (process == null) return instances;

                string output = process.StandardOutput.ReadToEnd();
                var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var data = line.Split(',');
                    if (data.Length >= 5)
                    {
                        instances.Add(new EmulatorInstance 
                        {
                            Index = data[0],
                            Title = data[1],
                            IsRunning = data[3] == "1",
                            // O PID é a 5ª coluna no listv2 do memuc
                            PID = int.TryParse(data[4], out int pid) ? pid : 0
                        });
                    }
                }
                return instances;
            }
        }

        public void StartInstance(int index) => ExecuteCommand($"start -i {index}");
        public void StopInstance(int index) => ExecuteCommand($"stop -i {index}");

        private void ExecuteCommand(string args)
        {
            try 
            {
                Process.Start(new ProcessStartInfo 
                {
                    FileName = _exePath,
                    Arguments = args,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                })?.WaitForExit();
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"[MEMUC ERROR] {ex.Message}");
            }
        }
    }
}
