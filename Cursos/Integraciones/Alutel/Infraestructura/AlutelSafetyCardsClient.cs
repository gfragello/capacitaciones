using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Dominio;
using Newtonsoft.Json;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public interface IAlutelSafetyCardsClient
    {
        Task<AlutelClientResult> ActualizarAsync(IReadOnlyCollection<SafetyCardUpdate> items, CancellationToken cancellationToken);
    }

    public sealed class AlutelSafetyCardsClient : IAlutelSafetyCardsClient
    {
        private readonly IAlutelConfiguration _configuration;
        private readonly IAlutelTokenProvider _tokenProvider;
        private readonly IAlutelHttpTransport _transport;

        public AlutelSafetyCardsClient(IAlutelConfiguration configuration, IAlutelTokenProvider tokenProvider, IAlutelHttpTransport transport)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            if (tokenProvider == null)
                throw new ArgumentNullException("tokenProvider");
            if (transport == null)
                throw new ArgumentNullException("transport");

            _configuration = configuration;
            _tokenProvider = tokenProvider;
            _transport = transport;
        }

        public async Task<AlutelClientResult> ActualizarAsync(IReadOnlyCollection<SafetyCardUpdate> items, CancellationToken cancellationToken)
        {
            _configuration.ValidarParaEnvio();
            ValidarItems(items);

            string token;
            try
            {
                token = await _tokenProvider.ObtenerTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (AlutelAuthenticationException ex)
            {
                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.ErrorDefinitivo, ex.StatusCode, ex.Message);
            }

            var result = await EnviarUnaVezAsync(items, token, cancellationToken).ConfigureAwait(false);
            if (result.CodigoHttp != (int)HttpStatusCode.Unauthorized)
                return result;

            _tokenProvider.Invalidar(token);
            string tokenRenovado;
            try
            {
                tokenRenovado = await _tokenProvider.ObtenerTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (AlutelAuthenticationException ex)
            {
                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.ErrorDefinitivo, ex.StatusCode, ex.Message);
            }

            return await EnviarUnaVezAsync(items, tokenRenovado, cancellationToken).ConfigureAwait(false);
        }

        private async Task<AlutelClientResult> EnviarUnaVezAsync(IReadOnlyCollection<SafetyCardUpdate> items, string token, CancellationToken cancellationToken)
        {
            using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutSource.CancelAfter(_configuration.Timeout);
                try
                {
                    var endpoint = new Uri(_configuration.BaseUrl, _configuration.SafetyCardsPath);
                    using (var request = new HttpRequestMessage(HttpMethod.Put, endpoint))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var json = JsonConvert.SerializeObject(items, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                        using (var response = await _transport.SendAsync(request, timeoutSource.Token).ConfigureAwait(false))
                        {
                            if (response.StatusCode == HttpStatusCode.Unauthorized)
                                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.ErrorDefinitivo, (int)response.StatusCode, "Token rechazado por Alutel.");
                            if ((int)response.StatusCode == 429)
                                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.ErrorReintentable, (int)response.StatusCode, "Alutel limitó temporalmente la cantidad de solicitudes.");
                            if ((int)response.StatusCode >= 500)
                                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.Indeterminado, (int)response.StatusCode, "No fue posible confirmar si Alutel procesó la solicitud.");
                            if (!response.IsSuccessStatusCode)
                                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.ErrorDefinitivo, (int)response.StatusCode, "Alutel rechazó la solicitud.");
                            if (response.StatusCode != HttpStatusCode.OK)
                                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.Indeterminado, (int)response.StatusCode, "Alutel devolvió un código de éxito no contemplado.");
                            if (response.Content == null)
                                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.Indeterminado, (int)response.StatusCode, "Alutel devolvió una respuesta vacía.");

                            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            return InterpretarRespuesta(body, items, (int)response.StatusCode);
                        }
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    return AlutelClientResult.Crear(ResultadoTecnicoAlutel.Indeterminado, null, "La solicitud a Alutel agotó el tiempo de espera.");
                }
                catch (HttpRequestException)
                {
                    return AlutelClientResult.Crear(ResultadoTecnicoAlutel.Indeterminado, null, "No fue posible confirmar el resultado de la solicitud a Alutel.");
                }
            }
        }

        private static AlutelClientResult InterpretarRespuesta(string body, IReadOnlyCollection<SafetyCardUpdate> items, int statusCode)
        {
            SafetyCardsResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<SafetyCardsResponse>(body);
            }
            catch (JsonException)
            {
                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.Indeterminado, statusCode, "Alutel devolvió una respuesta JSON inválida.");
            }

            var failedDocuments = response == null ? null : response.FailedDocuments;
            var documentos = new HashSet<string>(items.Select(i => i.DocumentNumber), StringComparer.Ordinal);
            var fallidosUnicos = failedDocuments == null
                ? null
                : new HashSet<string>(failedDocuments.Where(d => d != null), StringComparer.Ordinal);

            var consistente = response != null &&
                              response.SuccessfulProcessed >= 0 &&
                              response.FailedProcessed >= 0 &&
                              response.SuccessfulProcessed + response.FailedProcessed == items.Count &&
                              failedDocuments != null &&
                              failedDocuments.All(d => !string.IsNullOrWhiteSpace(d) && documentos.Contains(d)) &&
                              fallidosUnicos.Count == failedDocuments.Count &&
                              failedDocuments.Count == response.FailedProcessed;

            if (!consistente)
                return AlutelClientResult.Crear(ResultadoTecnicoAlutel.Indeterminado, statusCode, "Los contadores o documentos devueltos por Alutel son inconsistentes.");

            return response.FailedProcessed == 0
                ? AlutelClientResult.Crear(ResultadoTecnicoAlutel.Aceptado, statusCode, "Alutel procesó la vigencia.", response.SuccessfulProcessed, response.FailedProcessed)
                : AlutelClientResult.Crear(ResultadoTecnicoAlutel.RechazadoFuncional, statusCode, "Alutel rechazó el documento.", response.SuccessfulProcessed, response.FailedProcessed);
        }

        private void ValidarItems(IReadOnlyCollection<SafetyCardUpdate> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Se requiere al menos un item.", "items");
            if (items.Count > _configuration.MaximoItemsPorRequest)
                throw new ArgumentException("La cantidad de items excede el máximo configurado.", "items");

            foreach (var item in items)
                SafetyCardUpdateValidator.Validar(item);

            if (items.Select(i => i.DocumentNumber).Distinct(StringComparer.Ordinal).Count() != items.Count)
                throw new ArgumentException("No se permiten documentos duplicados en una solicitud.", "items");
        }
    }
}
