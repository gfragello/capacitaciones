using System;
using System.Collections.Generic;
using Cursos.Integraciones.Alutel.Aplicacion;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Models;
using Cursos.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cursos.Tests
{
    [TestClass]
    public class AlutelEligibilityPolicyTests
    {
        private static readonly DateTime Hoy = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Local);

        private readonly AlutelEligibilityPolicy _policy = new AlutelEligibilityPolicy(new FixedClock(Hoy));

        [TestMethod]
        public void Evaluar_RegistroAprobadoVigenteConCursoConfigurado_EsElegible()
        {
            var resultado = _policy.Evaluar(CrearRegistro());

            Assert.IsTrue(resultado.EsElegible);
            Assert.AreEqual(MotivoNoElegibleAlutel.Elegible, resultado.Motivo);
            Assert.AreEqual(string.Empty, resultado.Mensaje);
        }

        [TestMethod]
        [DataRow(EstadosRegistroCapacitacion.Inscripto)]
        [DataRow(EstadosRegistroCapacitacion.NoAprobado)]
        public void Evaluar_RegistroNoAprobado_NoEsElegible(EstadosRegistroCapacitacion estado)
        {
            var registro = CrearRegistro();
            registro.Estado = estado;

            var resultado = _policy.Evaluar(registro);

            Assert.IsFalse(resultado.EsElegible);
            Assert.AreEqual(MotivoNoElegibleAlutel.RegistroNoAprobado, resultado.Motivo);
            Assert.AreNotEqual(string.Empty, resultado.Mensaje);
        }

        [TestMethod]
        public void Evaluar_CursoSinTipoVigencia_NoEsElegible()
        {
            var registro = CrearRegistro();
            registro.Jornada.Curso.TipoVigenciaAlutel = null;

            var resultado = _policy.Evaluar(registro);

            Assert.IsFalse(resultado.EsElegible);
            Assert.AreEqual(MotivoNoElegibleAlutel.CursoNoConfigurado, resultado.Motivo);
        }

        [TestMethod]
        public void Evaluar_SinFechaVencimiento_NoEsElegible()
        {
            var registro = CrearRegistro();
            registro.FechaVencimiento = null;

            var resultado = _policy.Evaluar(registro);

            Assert.IsFalse(resultado.EsElegible);
            Assert.AreEqual(MotivoNoElegibleAlutel.SinFechaVencimiento, resultado.Motivo);
        }

        [TestMethod]
        public void Evaluar_VigenciaVencida_NoEsElegible()
        {
            var registro = CrearRegistro();
            registro.FechaVencimiento = Hoy.AddDays(-1);

            var resultado = _policy.Evaluar(registro);

            Assert.IsFalse(resultado.EsElegible);
            Assert.AreEqual(MotivoNoElegibleAlutel.VigenciaVencida, resultado.Motivo);
        }

        [TestMethod]
        public void Evaluar_DocumentoVacio_NoEsElegible()
        {
            var registro = CrearRegistro();
            registro.Capacitado.Documento = "   ";

            var resultado = _policy.Evaluar(registro);

            Assert.IsFalse(resultado.EsElegible);
            Assert.AreEqual(MotivoNoElegibleAlutel.DocumentoNoDisponible, resultado.Motivo);
        }

        [TestMethod]
        public void Evaluar_RegistroNulo_NoEsElegible()
        {
            var resultado = _policy.Evaluar(null);

            Assert.IsFalse(resultado.EsElegible);
            Assert.AreEqual(MotivoNoElegibleAlutel.RegistroInexistente, resultado.Motivo);
        }

        [TestMethod]
        public void SeleccionarRegistro_GanaLaMayorFechaVencimientoAunqueLaJornadaSeaAnterior()
        {
            var jornadaAntigua = CrearRegistro(id: 10, fechaJornada: new DateTime(2026, 1, 10), vencimiento: new DateTime(2029, 1, 10));
            var jornadaReciente = CrearRegistro(id: 20, fechaJornada: new DateTime(2026, 6, 10), vencimiento: new DateTime(2027, 6, 10));

            var seleccionado = _policy.SeleccionarRegistro(new List<RegistroCapacitacion> { jornadaReciente, jornadaAntigua });

            Assert.AreEqual(10, seleccionado.RegistroCapacitacionID);
        }

        [TestMethod]
        public void SeleccionarRegistro_MismaVigencia_GanaLaJornadaMasReciente()
        {
            var vencimiento = new DateTime(2026, 12, 31);
            var anterior = CrearRegistro(id: 10, fechaJornada: new DateTime(2026, 1, 10), vencimiento: vencimiento);
            var posterior = CrearRegistro(id: 20, fechaJornada: new DateTime(2026, 6, 10), vencimiento: vencimiento);

            var seleccionado = _policy.SeleccionarRegistro(new List<RegistroCapacitacion> { anterior, posterior });

            Assert.AreEqual(20, seleccionado.RegistroCapacitacionID);
        }

        [TestMethod]
        public void SeleccionarRegistro_MismaVigenciaYJornada_GanaElIdentificadorMayor()
        {
            var vencimiento = new DateTime(2026, 12, 31);
            var fechaJornada = new DateTime(2026, 6, 10);
            var menor = CrearRegistro(id: 10, fechaJornada: fechaJornada, vencimiento: vencimiento);
            var mayor = CrearRegistro(id: 11, fechaJornada: fechaJornada, vencimiento: vencimiento);

            var seleccionado = _policy.SeleccionarRegistro(new List<RegistroCapacitacion> { menor, mayor });

            Assert.AreEqual(11, seleccionado.RegistroCapacitacionID);
        }

        [TestMethod]
        public void SeleccionarRegistro_DescartaLosNoElegiblesAunqueTenganMayorVigencia()
        {
            var noAprobado = CrearRegistro(id: 10, vencimiento: new DateTime(2030, 1, 1));
            noAprobado.Estado = EstadosRegistroCapacitacion.NoAprobado;
            var aprobado = CrearRegistro(id: 20, vencimiento: new DateTime(2027, 1, 1));

            var seleccionado = _policy.SeleccionarRegistro(new List<RegistroCapacitacion> { noAprobado, aprobado });

            Assert.AreEqual(20, seleccionado.RegistroCapacitacionID);
        }

        [TestMethod]
        public void SeleccionarRegistro_SinCandidatosElegibles_DevuelveNulo()
        {
            var vencido = CrearRegistro(id: 10, vencimiento: Hoy.AddDays(-1));

            Assert.IsNull(_policy.SeleccionarRegistro(new List<RegistroCapacitacion> { vencido }));
        }

        private static RegistroCapacitacion CrearRegistro(
            int id = 1,
            DateTime? fechaJornada = null,
            DateTime? vencimiento = null,
            TipoVigenciaAlutel tipoVigencia = TipoVigenciaAlutel.TarjetaVerde)
        {
            return new RegistroCapacitacion
            {
                RegistroCapacitacionID = id,
                Estado = EstadosRegistroCapacitacion.Aprobado,
                FechaVencimiento = vencimiento ?? new DateTime(2027, 12, 31),
                Capacitado = new Capacitado { Documento = "4895623" },
                Jornada = new Jornada
                {
                    Fecha = fechaJornada ?? new DateTime(2026, 6, 10),
                    Curso = new Curso { TipoVigenciaAlutel = tipoVigencia }
                }
            };
        }

        private sealed class FixedClock : IClock
        {
            private readonly DateTime _now;

            public FixedClock(DateTime now)
            {
                _now = now;
            }

            public DateTime UtcNow { get { return _now.ToUniversalTime(); } }

            public DateTime Now { get { return _now; } }
        }
    }
}
