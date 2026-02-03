using WindowsInput;
using System;
using System.IO;
using Newtonsoft.Json;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    public class MetricsService
    {
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monitoramento.json");
        private readonly BotInstance _bot;

        public MetricsService(BotInstance bot)
        {
            _bot = bot;
            InitializeFile();
        }

        private void InitializeFile()
        {
            if (!File.Exists(_filePath))
            {
                var initialStats = new BotStats { 
                    InstanciaID = "MEmu_Automator_V1", 
                    Sucessos = 0, 
                    FalhasCriticas = 0, 
                    BonusColetados = 0 
                };
                Save(initialStats);
            }
        }

        public BotStats Load()
        {
            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<BotStats>(json);
        }

        public void Save(BotStats stats)
        {
            stats.UltimaAtualizacao = DateTime.Now;
            string json = JsonConvert.SerializeObject(stats, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void RegistrarSucesso(bool foiBonus = false)
        {
            var stats = Load();
            stats.Sucessos++;
            if (foiBonus) stats.BonusColetados++;
            Save(stats);
            _bot.Log($"[DASHBOARD] Sucessos: {stats.Sucessos} | Bônus: {stats.BonusColetados}");
        }

        public void RegistrarFalhaCritica()
        {
            var stats = Load();
            stats.FalhasCriticas++;
            Save(stats);
        }
    }

    public class BotStats
    {
        public string InstanciaID { get; set; }
        public int Sucessos { get; set; }
        public int FalhasCriticas { get; set; }
        public int BonusColetados { get; set; }
        public DateTime UltimaAtualizacao { get; set; }
    }
}