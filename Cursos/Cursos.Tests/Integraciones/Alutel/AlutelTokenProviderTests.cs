using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Aplicacion;
using Cursos.Integraciones.Alutel.Infraestructura;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cursos.Tests
{
    [TestClass]
    public class AlutelTokenProviderTests
    {
        [TestMethod]
        public async Task ObtenerTokenAsync_ReutilizaTokenVigente()
        {
            var transport = new QueueTransport(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token-seguro\",\"expires_in\":3600,\"token_type\":\"Bearer\"}")
            });
            var provider = new AlutelTokenProvider(TestConfiguration.Create(), new StaticSecretProvider(), transport, new FixedClock());

            var primero = await provider.ObtenerTokenAsync(CancellationToken.None);
            var segundo = await provider.ObtenerTokenAsync(CancellationToken.None);

            Assert.AreEqual("token-seguro", primero);
            Assert.AreEqual(primero, segundo);
            Assert.HasCount(1, transport.Requests);
            Assert.DoesNotContain("token-seguro", transport.Requests[0].Body);
        }

        [TestMethod]
        public async Task ObtenerTokenAsync_Concurrente_HaceUnSoloRefresh()
        {
            var transport = new QueueTransport(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token-seguro\",\"expires_in\":3600,\"token_type\":\"Bearer\"}")
            });
            var provider = new AlutelTokenProvider(TestConfiguration.Create(), new StaticSecretProvider(), transport, new FixedClock());

            var tokens = await Task.WhenAll(
                provider.ObtenerTokenAsync(CancellationToken.None),
                provider.ObtenerTokenAsync(CancellationToken.None));

            CollectionAssert.AreEqual(new[] { "token-seguro", "token-seguro" }, tokens);
            Assert.HasCount(1, transport.Requests);
        }

        [TestMethod]
        public async Task ObtenerTokenAsync_RespuestaInvalida_ClasificaErrorSinExponerBody()
        {
            var transport = new QueueTransport(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"valor-que-no-debe-salir\"}")
            });
            var provider = new AlutelTokenProvider(TestConfiguration.Create(), new StaticSecretProvider(), transport, new FixedClock());

            var error = await Assert.ThrowsExactlyAsync<AlutelAuthenticationException>(
                () => provider.ObtenerTokenAsync(CancellationToken.None));

            Assert.DoesNotContain("valor-que-no-debe-salir", error.Message);
        }

        private sealed class StaticSecretProvider : IAlutelSecretProvider
        {
            public Task<string> ObtenerClientSecretAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult("secret-no-registrable");
            }
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

            public DateTime Now => new DateTime(2026, 7, 17, 9, 0, 0, DateTimeKind.Local);
        }
    }
}
