using System;
using System.Configuration;
using System.Globalization;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public interface IAlutelConfiguration
    {
        bool Habilitado { get; }
        Uri BaseUrl { get; }
        Uri TokenEndpoint { get; }
        string Scope { get; }
        string ClientId { get; }
        string SafetyCardsPath { get; }
        TimeSpan Timeout { get; }
        int MaximoItemsPorRequest { get; }
        TimeSpan MargenRenovacionToken { get; }
        string Entorno { get; }
        void ValidarParaEnvio();
    }

    public sealed class AlutelConfiguration : IAlutelConfiguration
    {
        public AlutelConfiguration(
            bool habilitado,
            Uri baseUrl,
            Uri tokenEndpoint,
            string scope,
            string clientId,
            string safetyCardsPath,
            TimeSpan timeout,
            int maximoItemsPorRequest,
            TimeSpan margenRenovacionToken,
            string entorno)
        {
            Habilitado = habilitado;
            BaseUrl = baseUrl;
            TokenEndpoint = tokenEndpoint;
            Scope = scope;
            ClientId = clientId;
            SafetyCardsPath = safetyCardsPath;
            Timeout = timeout;
            MaximoItemsPorRequest = maximoItemsPorRequest;
            MargenRenovacionToken = margenRenovacionToken;
            Entorno = entorno;
        }

        public bool Habilitado { get; private set; }
        public Uri BaseUrl { get; private set; }
        public Uri TokenEndpoint { get; private set; }
        public string Scope { get; private set; }
        public string ClientId { get; private set; }
        public string SafetyCardsPath { get; private set; }
        public TimeSpan Timeout { get; private set; }
        public int MaximoItemsPorRequest { get; private set; }
        public TimeSpan MargenRenovacionToken { get; private set; }
        public string Entorno { get; private set; }

        public static AlutelConfiguration DesdeAppSettings()
        {
            return new AlutelConfiguration(
                LeerBooleano("Alutel:Habilitado", false),
                LeerUri("Alutel:BaseUrl"),
                LeerUri("Alutel:TokenEndpoint"),
                ConfigurationManager.AppSettings["Alutel:Scope"],
                ConfigurationManager.AppSettings["Alutel:ClientId"],
                ConfigurationManager.AppSettings["Alutel:SafetyCardsPath"] ?? "Cardholder/SafetyCards",
                TimeSpan.FromSeconds(LeerEntero("Alutel:TimeoutSegundos", 30)),
                LeerEntero("Alutel:MaximoItemsPorRequest", 1),
                TimeSpan.FromSeconds(LeerEntero("Alutel:MargenRenovacionTokenSegundos", 60)),
                ConfigurationManager.AppSettings["Alutel:Entorno"] ?? "NoConfigurado");
        }

        public void ValidarParaEnvio()
        {
            if (!Habilitado)
                throw new AlutelConfigurationException("La integración Alutel está deshabilitada.");
            if (BaseUrl == null || !BaseUrl.IsAbsoluteUri || BaseUrl.Scheme != Uri.UriSchemeHttps)
                throw new AlutelConfigurationException("Alutel:BaseUrl debe ser una URL HTTPS absoluta.");
            if (TokenEndpoint == null || !TokenEndpoint.IsAbsoluteUri || TokenEndpoint.Scheme != Uri.UriSchemeHttps)
                throw new AlutelConfigurationException("Alutel:TokenEndpoint debe ser una URL HTTPS absoluta.");
            if (string.IsNullOrWhiteSpace(Scope))
                throw new AlutelConfigurationException("Alutel:Scope es obligatorio.");
            if (string.IsNullOrWhiteSpace(ClientId))
                throw new AlutelConfigurationException("Alutel:ClientId es obligatorio.");
            if (string.IsNullOrWhiteSpace(SafetyCardsPath))
                throw new AlutelConfigurationException("Alutel:SafetyCardsPath es obligatorio.");
            if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromMinutes(2))
                throw new AlutelConfigurationException("Alutel:TimeoutSegundos debe estar entre 1 y 120.");
            if (MaximoItemsPorRequest != 1)
                throw new AlutelConfigurationException("La fase inicial de Alutel admite exactamente un item por request.");
            if (MargenRenovacionToken < TimeSpan.Zero)
                throw new AlutelConfigurationException("El margen de renovación del token no puede ser negativo.");
            if (string.IsNullOrWhiteSpace(Entorno))
                throw new AlutelConfigurationException("Alutel:Entorno es obligatorio.");
        }

        private static Uri LeerUri(string key)
        {
            Uri uri;
            return Uri.TryCreate(ConfigurationManager.AppSettings[key], UriKind.Absolute, out uri) ? uri : null;
        }

        private static bool LeerBooleano(string key, bool valorPredeterminado)
        {
            bool valor;
            return bool.TryParse(ConfigurationManager.AppSettings[key], out valor) ? valor : valorPredeterminado;
        }

        private static int LeerEntero(string key, int valorPredeterminado)
        {
            int valor;
            return int.TryParse(ConfigurationManager.AppSettings[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out valor)
                ? valor
                : valorPredeterminado;
        }
    }

    public sealed class AlutelConfigurationException : InvalidOperationException
    {
        public AlutelConfigurationException(string message) : base(message)
        {
        }
    }
}
