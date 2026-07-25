using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Integraciones.Alutel.Infraestructura;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cursos.Tests
{
    [TestClass]
    public class AlutelClientTests
    {
        [TestMethod]
        public async Task ActualizarAsync_SerializaSoloLaVigenciaYValidaRespuesta()
        {
            var transport = new QueueTransport(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"successfulProcessed\":1,\"failedProcessed\":0,\"failedDocuments\":[]}")
            });
            var client = new AlutelSafetyCardsClient(TestConfiguration.Create(), new SequenceTokenProvider("token-1"), transport);

            var result = await client.ActualizarAsync(new[]
            {
                new SafetyCardUpdate { DocumentNumber = "4895623", VtoTarjetaVerde = "20271231" }
            }, CancellationToken.None);

            Assert.AreEqual(ResultadoTecnicoAlutel.Aceptado, result.Resultado);
            Assert.AreEqual("[{\"documentNumber\":\"4895623\",\"vtoTarjetaVerde\":\"20271231\"}]", transport.Requests.Single().Body);
            Assert.AreEqual("Bearer", transport.Requests.Single().AuthorizationScheme);
            Assert.AreEqual("token-1", transport.Requests.Single().AuthorizationParameter);
        }

        [TestMethod]
        public async Task ActualizarAsync_RespuestaInconsistente_NuncaAcepta()
        {
            var transport = new QueueTransport(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"successfulProcessed\":1,\"failedProcessed\":1,\"failedDocuments\":[]}")
            });
            var client = new AlutelSafetyCardsClient(TestConfiguration.Create(), new SequenceTokenProvider("token-1"), transport);

            var result = await client.ActualizarAsync(new[]
            {
                new SafetyCardUpdate { DocumentNumber = "4895623", VtoTarjetaAzul = "20280131" }
            }, CancellationToken.None);

            Assert.AreEqual(ResultadoTecnicoAlutel.Indeterminado, result.Resultado);
        }

        [TestMethod]
        public async Task ActualizarAsync_Un401_RenuevaUnaSolaVez()
        {
            var transport = new QueueTransport(
                new HttpResponseMessage(HttpStatusCode.Unauthorized),
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"successfulProcessed\":1,\"failedProcessed\":0,\"failedDocuments\":[]}")
                });
            var tokens = new SequenceTokenProvider("token-vencido", "token-renovado");
            var client = new AlutelSafetyCardsClient(TestConfiguration.Create(), tokens, transport);

            var result = await client.ActualizarAsync(new[]
            {
                new SafetyCardUpdate { DocumentNumber = "4895623", VtoActualizacionSeguridad = "20270115" }
            }, CancellationToken.None);

            Assert.AreEqual(ResultadoTecnicoAlutel.Aceptado, result.Resultado);
            Assert.AreEqual(1, tokens.Invalidations);
            Assert.HasCount(2, transport.Requests);
            Assert.AreEqual("token-renovado", transport.Requests.Last().AuthorizationParameter);
        }

        [TestMethod]
        public async Task ActualizarAsync_Segundo401_NoEntraEnBucle()
        {
            var transport = new QueueTransport(
                new HttpResponseMessage(HttpStatusCode.Unauthorized),
                new HttpResponseMessage(HttpStatusCode.Unauthorized));
            var client = new AlutelSafetyCardsClient(TestConfiguration.Create(), new SequenceTokenProvider("token-1", "token-2"), transport);

            var result = await client.ActualizarAsync(new[]
            {
                new SafetyCardUpdate { DocumentNumber = "4895623", VtoTarjetaVerde = "20271231" }
            }, CancellationToken.None);

            Assert.AreEqual(ResultadoTecnicoAlutel.ErrorDefinitivo, result.Resultado);
            Assert.HasCount(2, transport.Requests);
        }

        [TestMethod]
        public async Task ActualizarAsync_429_EsReintentable()
        {
            var transport = new QueueTransport(new HttpResponseMessage((HttpStatusCode)429));
            var client = new AlutelSafetyCardsClient(TestConfiguration.Create(), new SequenceTokenProvider("token-1"), transport);

            var result = await client.ActualizarAsync(new[]
            {
                new SafetyCardUpdate { DocumentNumber = "4895623", VtoTarjetaVerde = "20271231" }
            }, CancellationToken.None);

            Assert.AreEqual(ResultadoTecnicoAlutel.ErrorReintentable, result.Resultado);
        }

        [TestMethod]
        public async Task ActualizarAsync_5xx_EsIndeterminado()
        {
            var transport = new QueueTransport(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var client = new AlutelSafetyCardsClient(TestConfiguration.Create(), new SequenceTokenProvider("token-1"), transport);

            var result = await client.ActualizarAsync(new[]
            {
                new SafetyCardUpdate { DocumentNumber = "4895623", VtoTarjetaVerde = "20271231" }
            }, CancellationToken.None);

            Assert.AreEqual(ResultadoTecnicoAlutel.Indeterminado, result.Resultado);
        }
    }

    internal sealed class TestConfiguration : IAlutelConfiguration
    {
        public static TestConfiguration Create()
        {
            return new TestConfiguration
            {
                Habilitado = true,
                BaseUrl = new Uri("https://alutel.test/api/"),
                TokenEndpoint = new Uri("https://login.test/token"),
                Scope = "api/.default",
                ClientId = "client-id",
                SafetyCardsPath = "Cardholder/SafetyCards",
                Timeout = TimeSpan.FromSeconds(10),
                MaximoItemsPorRequest = 1,
                MargenRenovacionToken = TimeSpan.FromSeconds(60),
                Entorno = "Tests"
            };
        }

        public bool Habilitado { get; set; }
        public Uri BaseUrl { get; set; }
        public Uri TokenEndpoint { get; set; }
        public string Scope { get; set; }
        public string ClientId { get; set; }
        public string SafetyCardsPath { get; set; }
        public TimeSpan Timeout { get; set; }
        public int MaximoItemsPorRequest { get; set; }
        public TimeSpan MargenRenovacionToken { get; set; }
        public string Entorno { get; set; }

        public void ValidarParaEnvio()
        {
            if (!Habilitado)
                throw new AlutelConfigurationException("Deshabilitado para tests.");
        }
    }

    internal sealed class SequenceTokenProvider : IAlutelTokenProvider
    {
        private readonly Queue<string> _tokens;

        public SequenceTokenProvider(params string[] tokens)
        {
            _tokens = new Queue<string>(tokens);
        }

        public int Invalidations { get; private set; }

        public Task<string> ObtenerTokenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_tokens.Dequeue());
        }

        public void Invalidar(string tokenUtilizado)
        {
            Invalidations++;
        }
    }

    internal sealed class CapturedRequest
    {
        public string Body { get; set; }
        public string AuthorizationScheme { get; set; }
        public string AuthorizationParameter { get; set; }
    }

    internal sealed class QueueTransport : IAlutelHttpTransport
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueTransport(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<CapturedRequest> Requests { get; } = new List<CapturedRequest>();

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            Requests.Add(new CapturedRequest
            {
                Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(),
                AuthorizationScheme = request.Headers.Authorization?.Scheme,
                AuthorizationParameter = request.Headers.Authorization?.Parameter
            });
            return _responses.Dequeue();
        }
    }
}
