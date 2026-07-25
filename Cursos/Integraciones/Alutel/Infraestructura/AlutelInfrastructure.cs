using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Aplicacion;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow { get { return DateTime.UtcNow; } }

        public DateTime Now { get { return DateTime.Now; } }
    }

    public interface IAlutelSecretProvider
    {
        Task<string> ObtenerClientSecretAsync(CancellationToken cancellationToken);
    }

    public sealed class EnvironmentVariableAlutelSecretProvider : IAlutelSecretProvider
    {
        private readonly string _variableName;

        public EnvironmentVariableAlutelSecretProvider(string variableName = "CURSOS_ALUTEL_CLIENT_SECRET")
        {
            _variableName = variableName;
        }

        public Task<string> ObtenerClientSecretAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var secret = Environment.GetEnvironmentVariable(_variableName);
            if (string.IsNullOrWhiteSpace(secret))
                throw new AlutelConfigurationException("No se configuró el secreto de Alutel en el proveedor seguro.");

            return Task.FromResult(secret);
        }
    }

    public interface IAlutelHttpTransport
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    public sealed class HttpClientAlutelTransport : IAlutelHttpTransport
    {
        private readonly HttpClient _httpClient;

        public HttpClientAlutelTransport(HttpClient httpClient)
        {
            if (httpClient == null)
                throw new ArgumentNullException("httpClient");

            _httpClient = httpClient;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
    }
}
