using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;
using System.Windows.Forms; // Necessário para acessar o Screen
using AutomacaoApp.Core;
using AutomacaoApp.Exceptions;
using AutomacaoApp.Models;

namespace AutomacaoApp.Services
{
    /// <summary>
    /// Serviço responsável por gerenciar a inicialização do jogo e validação da tela inicial.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class OpeningService
    {
        private readonly VisionEngine _vision;
        private readonly BotInstance _bot;
        private readonly string _gamePath = @"C:\Caminho\Para\SeuJogo.exe";

        public OpeningService(BotInstance bot, VisionEngine vision)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _vision = vision ?? throw new ArgumentNullException(nameof(vision));
        }

        /// <summary>
        /// Inicia o executável do jogo e aguarda até que a tela home seja identificada via OpenCV.
        /// </summary>
        public void StartGame()
        {
            try
            {
                _bot.Log($"Iniciando processo: {_gamePath}");

                // Verifica se o executável existe antes de tentar abrir
                if (!File.Exists(_gamePath))
                    throw new CriticalException($"Executável não encontrado no caminho especificado: {_gamePath}");

                Process.Start(new ProcessStartInfo(_gamePath) { UseShellExecute = true });

                // Inicia o loop de verificação da tela home
                bool isHomeFound = WaitForHome(60);

                if (!isHomeFound)
                {
                    throw new CriticalException("Timeout: O jogo iniciou, mas a 'tela_home.png' não foi detectada após 60 segundos.");
                }

                _bot.Log("Sucesso: Tela Home detectada. Iniciando módulos de operação.");
            }
            catch (Exception ex) when (!(ex is CriticalException))
            {
                throw new CriticalException($"Falha inesperada ao abrir o jogo: {ex.Message}");
            }
        }

        /// <summary>
        /// Realiza o polling (verificação cíclica) da tela em busca do template da home.
        /// </summary>
        private bool WaitForHome(int timeoutSeconds)
        {
            // Resolve o caminho do asset de forma dinâmica (relativo ao executável do bot)
            string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "tela_home.png");

            if (!File.Exists(assetPath))
                throw new CriticalException($"Asset crucial faltando: {assetPath}");

            using var templateHome = new Bitmap(assetPath);
            
            for (int i = 1; i <= timeoutSeconds; i++)
            {
                _bot.Log($"Aguardando tela home... Tentativa {i}/{timeoutSeconds}");

                using var currentScreen = CaptureScreen(); 
                
                // Tenta localizar o elemento na captura de tela atual
                var element = _vision.FindElement(currentScreen, templateHome);

                if (element != null) 
                {
                    _bot.Log("Elemento Visual 'tela_home' encontrado!");
                    return true;
                }

                Thread.Sleep(1000); // Aguarda 1 segundo entre verificações para não sobrecarregar a CPU
            }

            return false;
        }

        /// <summary>
        /// Captura um print integral da tela do monitor principal.
        /// </summary>
        private Bitmap CaptureScreen()
        {
            // Resolve o erro CS8602 verificando se o monitor principal existe
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
                throw new CriticalException("Erro de Hardware: Monitor principal não detectado para captura de tela.");

            Rectangle bounds = primaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Copia os pixels da tela para o objeto Bitmap
                g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, bounds.Size);
            }

            return bitmap;
        }
    }
}