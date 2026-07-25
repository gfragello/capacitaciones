using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Aplicacion;
using Newtonsoft.Json;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public interface IAlutelTokenProvider
    {
        Task<string> ObtenerTokenAsync(CancellationToken cancellationToken);
        void Invalidar(string tokenUtilizado);
    }

    public sealed class AlutelTokenProvider : IAlutelTokenProvider
    {
        private readonly IAlutelConfiguration _configuration;
        private readonly IAlutelSecretProvider _secretProvider;
        private readonly IAlutelHttpTransport _transport;
        private readonly IClock _clock;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
        private readonly object _cacheLock = new object();
        private string _accessToken;
        private DateTime _expiresAtUtc;

        public AlutelTokenProvider(IAlutelConfiguration configuration, IAlutelSecretProvider secretProvider, IAlutelHttpTransport transport, IClock clock)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            if (secretProvider == null)
                throw new ArgumentNullException("secretProvider");
            if (transport == null)
                throw new ArgumentNullException("transport");
            if (clock == null)
                throw new ArgumentNullException("clock");

            _configuration = configuration;
            _secretProvider = secretProvider;
            _transport = transport;
            _clock = clock;
        }

        public async Task<string> ObtenerTokenAsync(CancellationToken cancellationToken)
        {
            _configuration.ValidarParaEnvio();
            var token = ObtenerTokenVigente();
            if (token != null)
                return token;

            await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                token = ObtenerTokenVigente();
                if (token != null)
                    return token;

                return await RenovarAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public void Invalidar(string tokenUtilizado)
        {
            lock (_cacheLock)
            {
                if (string.Equals(_accessToken, tokenUtilizado, StringComparison.Ordinal))
                {
                    _accessToken = null;
                    _expiresAtUtc = DateTime.MinValue;
                }
            }
        }

        private string ObtenerTokenVigente()
        {
            lock (_cacheLock)
            {
                return !string.IsNullOrEmpty(_accessToken) &&
                       _clock.UtcNow < _expiresAtUtc.Subtract(_configuration.MargenRenovacionToken)
                    ? _accessToken
                    : null;
            }
        }

        private async Task<string> RenovarAsync(CancellationToken cancellationToken)
        {
            using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutSource.CancelAfter(_configuration.Timeout);
                try
                {
                    var secret = await _secretProvider.ObtenerClientSecretAsync(timeoutSource.Token).ConfigureAwait(false);
                    using (var request = new HttpRequestMessage(HttpMethod.Post, _configuration.TokenEndpoint))
                    {
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "grant_type", "client_credentials" },
                            { "client_id", _configuration.ClientId },
                            { "client_secret", secret },
                            { "scope", _configuration.Scope }
                        });

                        using (var response = await _transport.SendAsync(request, timeoutSource.Token).ConfigureAwait(false))
                        {
                            if (!response.IsSuccessStatusCode)
                                throw new AlutelAuthenticationException("Alutel rechazó la solicitud de autenticación.", (int)response.StatusCode);
                            if (response.Content == null)
                                throw new AlutelAuthenticationException("La respuesta de autenticación de Alutel está vacía.", (int)response.StatusCode);

                            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            OAuthTokenResponse tokenResponse;
                            try
                            {
                                tokenResponse = JsonConvert.DeserializeObject<OAuthTokenResponse>(json);
                            }
                            catch (JsonException)
                            {
                                throw new AlutelAuthenticationException("La respuesta de autenticación de Alutel no es válida.", (int)response.StatusCode);
                            }

                            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken) ||
                                tokenResponse.ExpiresIn <= 0 || !string.Equals(tokenResponse.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase))
                                throw new AlutelAuthenticationException("La respuesta de autenticación de Alutel está incompleta.", (int)response.StatusCode);

                            lock (_cacheLock)
                            {
                                _accessToken = tokenResponse.AccessToken;
                                _expiresAtUtc = _clock.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                                return _accessToken;
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new AlutelAuthenticationException("La autenticación con Alutel agotó el tiempo de espera.", null);
                }
                catch (HttpRequestException)
                {
                    throw new AlutelAuthenticationException("No fue posible autenticar con Alutel.", null);
                }
            }
        }
    }

    public sealed class AlutelAuthenticationException : Exception
    {
        public AlutelAuthenticationException(string message, int? statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        public int? StatusCode { get; private set; }
    }
}
