using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    public class MemucService
    {
        private readonly string _exePath = @"C:\Program Files\Microvirt\MEmu\memuc.exe";

        /// <summary>
        /// Obtém a lista de todas as instâncias com PID atualizado para o VisionEngine.
        /// </summary>
        public List<EmulatorInstance> GetInventory()
        {
            var instances = new List<EmulatorInstance>();
            var startInfo = new ProcessStartInfo {
                FileName = _exePath,
                Arguments = "listv2",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return instances;

            string output = process.StandardOutput.ReadToEnd();
            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var data = line.Split(',');
                if (data.Length >= 5)
                {
                    instances.Add(new EmulatorInstance {
                        Index = data[0],
                        Title = data[1],
                        IsRunning = data[3] == "1",
                        PID = int.TryParse(data[4], out int pid) ? pid : 0
                    });
                }
            }
            return instances;
        }

        /// <summary>
        /// Envia um clique diretamente para o Android via ADB (Não usa o mouse do Windows).
        /// </summary>
        public void SendClick(string index, int x, int y)
        {
            ExecuteCommand($"adb -i {index} shell input tap {x} {y}");
        }

        /// <summary>
        /// Envia uma tecla do sistema (Ex: 3 = HOME, 4 = BACK).
        /// </summary>
        public void SendKey(string index, int keyCode)
        {
            ExecuteCommand($"adb -i {index} shell input keyevent {keyCode}");
        }

        public void StartInstance(int index) => ExecuteCommand($"start -i {index}");
        public void StopInstance(int index) => ExecuteCommand($"stop -i {index}");

        private void ExecuteCommand(string args)
        {
            try {
                Process.Start(new ProcessStartInfo {
                    FileName = _exePath,
                    Arguments = args,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                })?.WaitForExit();
            } catch (Exception ex) {
                Console.WriteLine($"[MEMUC ERROR] {ex.Message}");
            }
        }
    }
}