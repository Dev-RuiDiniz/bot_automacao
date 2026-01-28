using System;

namespace AutomacaoApp.Models
{
    /// <summary>
    /// Representa os dados técnicos crus retornados pelo MEmu CLI.
    /// </summary>
    public class EmulatorInstance
    {
        public string Index { get; set; } = string.Empty; // Mantemos string para o comando memuc
        public string Title { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public int PID { get; set; } 
    }
}