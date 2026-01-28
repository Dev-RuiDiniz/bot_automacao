using System;

namespace AutomacaoApp.Exceptions
{
    /// <summary>
    /// Erros que permitem a continuidade do bot (ex: elemento não encontrado momentaneamente).
    /// </summary>
    public class LightException : Exception
    {
        public LightException(string message) : base(message) { }
    }

    /// <summary>
    /// Erros que exigem parada imediata ou intervenção humana (ex: VPN, Captcha, IP Ban).
    /// </summary>
    public class CriticalException : Exception
    {
        public CriticalException(string message) : base(message) { }
    }
}