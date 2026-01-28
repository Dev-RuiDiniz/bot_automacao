using System;
using AutomacaoApp.Enums;

namespace AutomacaoApp.Models
{
    public class BotInstance
    {
        // Propriedades da Instância
        public Guid Id { get; private set; }
        public string Name { get; set; }
        public BotState Status { get; set; }
        public DateTime LastUpdate { get; private set; }

        public BotInstance(string name)
        {
            Id = Guid.NewGuid(); // Gera um identificador único universal
            Name = name;
            Status = BotState.Opening;
            LastUpdate = DateTime.Now;
            
            Log("Instância inicializada com sucesso.");
        }

        /// <summary>
        /// Atualiza o estado do bot e registra a mudança no log.
        /// </summary>
        public void UpdateStatus(BotState newState)
        {
            Log($"Mudança de estado: {Status} -> {newState}");
            Status = newState;
            LastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Registra logs prefixados com o ID e Nome da instância para facilitar o debug.
        /// </summary>
        public void Log(string message)
        {
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] [ID: {Id.ToString().Substring(0, 8)}] [{Name}]: {message}";
            
            // Aqui você poderia salvar em um arquivo .txt usando Newtonsoft.Json ou File.AppendText
            Console.WriteLine(logMessage);
        }
    }
}