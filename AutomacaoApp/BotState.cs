namespace AutomacaoApp.Enums
{
    /// <summary>
    /// Define os estados possíveis da máquina de estados do Bot.
    /// </summary>
    public enum BotState
    {
        Opening,        // Inicialização e carregamento do software/jogo
        DailySpin,      // Interação com a roleta diária gratuita
        FriendsModule,  // Gerenciamento ou coleta de recompensas de amigos
        Roulette,       // Apostas ou interações na roleta principal
        NokoBox,        // Abertura de caixas ou baús específicos (NokoBox)
        VPNCheck,       // Verificação de segurança ou troca de IP via VPN
        BonusCollect    // Coleta geral de bônus acumulados
    }
}