using System;
using AutomacaoApp.Enums;

namespace AutomacaoApp.Models
{
    /// <summary>
    /// Representa a sessão ativa de um Bot. 
    /// Une a lógica de estado com as informações técnicas do emulador.
    /// </summary>
    public class BotInstance
    {
        // --- Propriedades de Identificação ---
        public Guid Id { get; private set; }
        public string Name { get; set; }
        
        // --- Propriedades Técnicas (Vindas do EmulatorInstance) ---
        public string Index { get; set; }  // O ID da instância no MEmu (ex: "0", "1")
        public int PID { get; set; }      // O ID do processo no Windows (essencial para o VisionEngine)

        // --- Gestão de Estado ---
        public BotState Status { get; set; }
        public DateTime LastUpdate { get; private set; }

        public BotInstance(string name, string index, int pid)
        {
            Id = Guid.NewGuid();
            Name = name;
            Index = index;
            PID = pid;
            Status = BotState.Opening;
            LastUpdate = DateTime.Now;
            
            Log($"Bot inicializado: MEmu Index {Index} | PID {PID}");
        }

        /// <summary>
        /// Atualiza o estado da máquina de estados.
        /// </summary>
        public void UpdateStatus(BotState newState)
        {
            Log($"Transição: {Status} -> {newState}");
            Status = newState;
            LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Central de Logs da Instância.
        /// </summary>
        public void Log(string message)
        {
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] [Slot: {Index}] [{Name}]: {message}";
            Console.WriteLine(logMessage);
            // Aqui pode-se adicionar o salvamento em ficheiro TXT no futuro
        }
    }
}