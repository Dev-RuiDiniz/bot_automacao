using System.Diagnostics;

namespace AutomacaoApp.Services
{
    public class ResourceMonitor
    {
        private readonly PerformanceCounter _cpuCounter;

        public ResourceMonitor()
        {
            // Inicializa o contador de CPU global
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        public float GetCpuUsage()
        {
            // A primeira chamada sempre retorna 0, então fazemos um pequeno aquecimento
            _cpuCounter.NextValue();
            System.Threading.Thread.Sleep(100); 
            return _cpuCounter.NextValue();
        }

        public bool IsSystemOverloaded(float threshold = 90.0f)
        {
            return GetCpuUsage() > threshold;
        }
    }
}