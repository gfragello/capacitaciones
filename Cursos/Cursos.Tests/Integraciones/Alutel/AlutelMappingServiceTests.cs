using System;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Integraciones.Alutel.Infraestructura;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cursos.Tests
{
    [TestClass]
    public class AlutelMappingServiceTests
    {
        private readonly AlutelMappingService _service = new AlutelMappingService();

        [TestMethod]
        [DataRow(TipoVigenciaAlutel.TarjetaVerde, "20240229", "verde")]
        [DataRow(TipoVigenciaAlutel.TarjetaAzul, "20240229", "azul")]
        [DataRow(TipoVigenciaAlutel.Refresh, "20240229", "refresh")]
        public void Mapear_GeneraSoloLaVigenciaConfigurada(TipoVigenciaAlutel tipo, string fechaEsperada, string campo)
        {
            var result = _service.Mapear("4895623", tipo, new DateTime(2024, 2, 29));

            Assert.AreEqual("4895623", result.DocumentNumber);
            Assert.AreEqual(campo == "verde" ? fechaEsperada : null, result.VtoTarjetaVerde);
            Assert.AreEqual(campo == "azul" ? fechaEsperada : null, result.VtoTarjetaAzul);
            Assert.AreEqual(campo == "refresh" ? fechaEsperada : null, result.VtoActualizacionSeguridad);
        }

        [TestMethod]
        public void Mapear_CursoSinConfiguracion_NoGeneraRequest()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _service.Mapear("4895623", null, new DateTime(2027, 12, 31)));
        }

        [TestMethod]
        public void Mapear_FechaNula_NoGeneraRequest()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _service.Mapear("4895623", TipoVigenciaAlutel.TarjetaVerde, null));
        }

        [TestMethod]
        public void Mapear_DocumentoVacio_NoGeneraRequest()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _service.Mapear(" ", TipoVigenciaAlutel.TarjetaVerde, new DateTime(2027, 12, 31)));
        }
    }
}
