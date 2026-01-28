using System;

namespace AutomacaoApp.Models
{
    /// <summary>
    /// Modelo para persistência de dados no monitoramento.json
    /// </summary>
    public class BotStats
    {
        public string? InstanciaID { get; set; }
        public int Sucessos { get; set; }
        public int FalhasCriticas { get; set; }
        public int BonusColetados { get; set; }
        public DateTime UltimaAtualizacao { get; set; }
    }
}