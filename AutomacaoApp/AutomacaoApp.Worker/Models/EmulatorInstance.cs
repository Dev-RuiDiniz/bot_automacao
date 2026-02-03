namespace AutomacaoApp.Worker.Models
{
    public class EmulatorInstance
    {
        public string Index { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public int PID { get; set; }
    }
}
