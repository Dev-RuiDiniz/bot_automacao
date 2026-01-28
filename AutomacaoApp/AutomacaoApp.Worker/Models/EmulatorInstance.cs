using System;

namespace AutomacaoApp.Models
{
    /// <summary>
    /// Representa os dados técnicos de uma instância do MEmu retornados via CLI.
    /// </summary>
    public class EmulatorInstance
    {
        // Alterado para int para suportar lógica matemática de indexação (Resolve CS0029)
        public int Index { get; set; } 
        
        public string Title { get; set; } = string.Empty;
        
        public bool IsRunning { get; set; }
        
        // Campo obrigatório para o monitoramento de processos (Resolve CS0117)
        public int PID { get; set; } 
    }
}