using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Aplicacion;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Models;
using Cursos.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cursos.Tests
{
    [TestClass]
    public class AlutelIntegrationServiceTests
    {
        private static readonly DateTime AhoraLocal = new DateTime(2026, 7, 25, 15, 0, 0, DateTimeKind.Local);
        private static readonly DateTime AhoraUtc = AhoraLocal.ToUniversalTime();

        [TestMethod]
        public async Task ProcesarAsync_Aceptado_PersisteEnProcesoAntesDeEnviarYCompleta()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos);
            var gateway = new FakeGateway(
                eventos,
                new ResultadoInvocacionAlutel(ResultadoTecnicoAlutel.Aceptado, 200, "Aceptado.", 1, 0));
            var service = CrearServicio(store, gateway);

            var resultado = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);

            Assert.AreEqual(EstadoIntegracionAlutel.Aceptado, resultado.Estado);
            Assert.IsTrue(resultado.SolicitudEnviada);
            Assert.AreEqual(EstadoIntegracionAlutel.Aceptado, store.Estado);
            CollectionAssert.AreEqual(
                new[] { "reclamar", "enviar", "completar" },
                eventos.ToArray());
        }

        [TestMethod]
        public async Task ProcesarAsync_NoElegible_NoCreaOperacionNiInvocaProveedor()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos);
            var gateway = new FakeGateway(eventos, CrearAceptado());
            var service = CrearServicio(store, gateway);
            var registro = CrearRegistro();
            registro.Estado = EstadosRegistroCapacitacion.NoAprobado;

            var resultado = await service.ProcesarAsync(registro, "administrador", CancellationToken.None);

            Assert.IsNull(resultado.Estado);
            Assert.IsFalse(resultado.SolicitudEnviada);
            Assert.IsEmpty(eventos);
            Assert.AreEqual(0, gateway.Invocaciones);
        }

        [TestMethod]
        public async Task ProcesarAsync_RegistroNoPersistido_NoCreaOperacion()
        {
            var eventos = new ConcurrentQueue<string>();
            var service = CrearServicio(
                new FakeOperationStore(eventos),
                new FakeGateway(eventos, CrearAceptado()));
            var registro = CrearRegistro();
            registro.RegistroCapacitacionID = 0;

            var resultado = await service.ProcesarAsync(registro, "administrador", CancellationToken.None);

            Assert.IsNull(resultado.Estado);
            Assert.IsEmpty(eventos);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ProcesarAsync_DatoExcedeLongitud_NoCreaOperacion(bool documentoLargo)
        {
            var eventos = new ConcurrentQueue<string>();
            var service = CrearServicio(
                new FakeOperationStore(eventos),
                new FakeGateway(eventos, CrearAceptado()));
            var registro = CrearRegistro();
            var usuario = "administrador";
            if (documentoLargo)
                registro.Capacitado.Documento = new string('1', 65);
            else
                usuario = new string('u', 257);

            var resultado = await service.ProcesarAsync(registro, usuario, CancellationToken.None);

            Assert.IsNull(resultado.Estado);
            Assert.IsEmpty(eventos);
        }

        [TestMethod]
        public async Task ProcesarAsync_YaAceptado_NoVuelveAInvocarProveedor()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos)
            {
                TieneOperacion = true,
                Estado = EstadoIntegracionAlutel.Aceptado
            };
            var gateway = new FakeGateway(eventos, CrearAceptado());
            var service = CrearServicio(store, gateway);

            var resultado = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);

            Assert.AreEqual(EstadoIntegracionAlutel.Aceptado, resultado.Estado);
            Assert.IsFalse(resultado.SolicitudEnviada);
            Assert.AreEqual(0, gateway.Invocaciones);
        }

        [TestMethod]
        [DataRow(ResultadoTecnicoAlutel.RechazadoFuncional)]
        [DataRow(ResultadoTecnicoAlutel.ErrorReintentable)]
        [DataRow(ResultadoTecnicoAlutel.ErrorDefinitivo)]
        public async Task ProcesarAsync_ErrorDefinido_MarcaOperacionFallida(ResultadoTecnicoAlutel resultadoTecnico)
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos);
            var gateway = new FakeGateway(
                eventos,
                new ResultadoInvocacionAlutel(resultadoTecnico, 400, "Falló."));
            var service = CrearServicio(store, gateway);

            var resultado = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);

            Assert.AreEqual(EstadoIntegracionAlutel.Fallido, resultado.Estado);
            Assert.AreEqual(EstadoIntegracionAlutel.Fallido, store.Estado);
        }

        [TestMethod]
        public async Task ProcesarAsync_ProveedorLanzaExcepcion_PersisteIndeterminadoSinExponerDetalle()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos);
            var gateway = new FakeGateway(eventos, new InvalidOperationException("dato sensible"));
            var service = CrearServicio(store, gateway);

            var resultado = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);

            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, resultado.Estado);
            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, store.Estado);
            Assert.DoesNotContain("dato sensible", resultado.Mensaje);
            Assert.DoesNotContain("dato sensible", store.UltimoResultado.MensajeSanitizado);
        }

        [TestMethod]
        public async Task ProcesarAsync_ProveedorDevuelveNulo_PersisteIndeterminado()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos);
            var service = CrearServicio(store, new FakeGateway(eventos, (ResultadoInvocacionAlutel)null));

            var resultado = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);

            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, resultado.Estado);
            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, store.Estado);
        }

        [TestMethod]
        public async Task ProcesarAsync_CanceladoTrasReclamo_PersisteIndeterminadoYPropagaCancelacion()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos);
            var service = CrearServicio(store, new FakeGateway(eventos, new OperationCanceledException()));

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None));

            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, store.Estado);
            Assert.AreEqual(ResultadoTecnicoAlutel.Indeterminado, store.UltimoResultado.Resultado);
        }

        [TestMethod]
        public async Task ProcesarAsync_ProveedorRespondioPeroFallaGuardado_DevuelveIndeterminadoYNoReenvia()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos) { FallarAlCompletar = true };
            var gateway = new FakeGateway(eventos, CrearAceptado());
            var service = CrearServicio(store, gateway);

            var primerResultado = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);
            var segundoResultado = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);

            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, primerResultado.Estado);
            Assert.IsTrue(primerResultado.SolicitudEnviada);
            Assert.IsNull(segundoResultado.Estado);
            Assert.IsFalse(segundoResultado.SolicitudEnviada);
            Assert.AreEqual(1, gateway.Invocaciones);
            Assert.AreEqual(EstadoIntegracionAlutel.EnProceso, store.Estado);

            store.FechaActualizacionUtc = AhoraUtc.AddHours(-2);
            var recuperadas = await service.RecuperarEnProcesoAsync(TimeSpan.FromHours(1), CancellationToken.None);
            Assert.AreEqual(1, recuperadas);
            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, store.Estado);
        }

        [TestMethod]
        public async Task RecuperarEnProcesoAsync_ConvierteOperacionAbandonadaEnIndeterminada()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos)
            {
                TieneOperacion = true,
                Estado = EstadoIntegracionAlutel.EnProceso,
                FechaActualizacionUtc = AhoraUtc.AddHours(-2)
            };
            var service = CrearServicio(store, new FakeGateway(eventos, CrearAceptado()));

            var recuperadas = await service.RecuperarEnProcesoAsync(TimeSpan.FromHours(1), CancellationToken.None);

            Assert.AreEqual(1, recuperadas);
            Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, store.Estado);
        }

        [TestMethod]
        [DataRow(EstadoIntegracionAlutel.Aceptado)]
        [DataRow(EstadoIntegracionAlutel.Fallido)]
        [DataRow(EstadoIntegracionAlutel.Indeterminado)]
        public async Task RecuperarEnProcesoAsync_NoModificaEstadosFinales(EstadoIntegracionAlutel estado)
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos)
            {
                TieneOperacion = true,
                Estado = estado,
                FechaActualizacionUtc = AhoraUtc.AddDays(-1)
            };
            var service = CrearServicio(store, new FakeGateway(eventos, CrearAceptado()));

            var recuperadas = await service.RecuperarEnProcesoAsync(TimeSpan.FromHours(1), CancellationToken.None);

            Assert.AreEqual(0, recuperadas);
            Assert.AreEqual(estado, store.Estado);
        }

        [TestMethod]
        public async Task ProcesarAsync_DosInvocacionesConcurrentes_SoloUnaLlegaAlProveedor()
        {
            var eventos = new ConcurrentQueue<string>();
            var store = new FakeOperationStore(eventos);
            var gateway = new PausedGateway(eventos, CrearAceptado());
            var service = CrearServicio(store, gateway);

            var primero = service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);
            await gateway.Iniciado.Task;

            var segundo = await service.ProcesarAsync(CrearRegistro(), "administrador", CancellationToken.None);
            gateway.Continuar.SetResult(true);
            var primerResultado = await primero;

            Assert.IsTrue(primerResultado.SolicitudEnviada);
            Assert.IsFalse(segundo.SolicitudEnviada);
            Assert.AreEqual(1, gateway.Invocaciones);
        }

        private static AlutelIntegrationService CrearServicio(
            IAlutelOperationStore store,
            IAlutelVigenciaGateway gateway)
        {
            var clock = new OrchestrationClock(AhoraLocal);
            return new AlutelIntegrationService(
                new AlutelEligibilityPolicy(clock),
                gateway,
                store,
                clock);
        }

        private static ResultadoInvocacionAlutel CrearAceptado()
        {
            return new ResultadoInvocacionAlutel(ResultadoTecnicoAlutel.Aceptado, 200, "Aceptado.", 1, 0);
        }

        private static RegistroCapacitacion CrearRegistro()
        {
            return new RegistroCapacitacion
            {
                RegistroCapacitacionID = 15,
                Estado = EstadosRegistroCapacitacion.Aprobado,
                FechaVencimiento = new DateTime(2027, 12, 31),
                Capacitado = new Capacitado { Documento = "4895623" },
                Jornada = new Jornada
                {
                    Fecha = new DateTime(2026, 7, 20),
                    Curso = new Curso { TipoVigenciaAlutel = TipoVigenciaAlutel.TarjetaVerde }
                }
            };
        }

        private sealed class OrchestrationClock : IClock
        {
            private readonly DateTime _now;

            public OrchestrationClock(DateTime now)
            {
                _now = now;
            }

            public DateTime UtcNow { get { return _now.ToUniversalTime(); } }
            public DateTime Now { get { return _now; } }
        }

        private sealed class FakeOperationStore : IAlutelOperationStore
        {
            private readonly object _sync = new object();
            private readonly ConcurrentQueue<string> _eventos;

            public FakeOperationStore(ConcurrentQueue<string> eventos)
            {
                _eventos = eventos;
            }

            public bool TieneOperacion { get; set; }
            public bool FallarAlCompletar { get; set; }
            public EstadoIntegracionAlutel Estado { get; set; }
            public DateTime FechaActualizacionUtc { get; set; }
            public ResultadoInvocacionAlutel UltimoResultado { get; private set; }

            public Task<ReclamoOperacionAlutel> ReclamarAsync(
                int registroCapacitacionId,
                string documento,
                TipoVigenciaAlutel tipoVigencia,
                DateTime fechaVigencia,
                string usuarioOrigen,
                DateTime fechaUtc,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _eventos.Enqueue("reclamar");
                lock (_sync)
                {
                    if (TieneOperacion && Estado == EstadoIntegracionAlutel.Aceptado)
                        return Task.FromResult(ReclamoOperacionAlutel.YaAceptado(10));
                    if (TieneOperacion && (Estado == EstadoIntegracionAlutel.EnProceso || Estado == EstadoIntegracionAlutel.Indeterminado))
                        return Task.FromResult(ReclamoOperacionAlutel.NoDisponible(10, "No disponible."));

                    TieneOperacion = true;
                    Estado = EstadoIntegracionAlutel.EnProceso;
                    FechaActualizacionUtc = fechaUtc;
                    return Task.FromResult(ReclamoOperacionAlutel.Reclamado(10, 20));
                }
            }

            public Task CompletarAsync(
                long operacionId,
                long intentoId,
                EstadoIntegracionAlutel estado,
                ResultadoInvocacionAlutel resultado,
                DateTime fechaUtc,
                CancellationToken cancellationToken)
            {
                _eventos.Enqueue("completar");
                if (FallarAlCompletar)
                    throw new AlutelPersistenceException("Fallo simulado.");

                lock (_sync)
                {
                    Estado = estado;
                    FechaActualizacionUtc = fechaUtc;
                    UltimoResultado = resultado;
                }
                return Task.CompletedTask;
            }

            public Task<int> RecuperarEnProcesoAsync(
                DateTime anterioresAUtc,
                DateTime fechaRecuperacionUtc,
                CancellationToken cancellationToken)
            {
                lock (_sync)
                {
                    if (!TieneOperacion || Estado != EstadoIntegracionAlutel.EnProceso ||
                        FechaActualizacionUtc > anterioresAUtc)
                        return Task.FromResult(0);

                    Estado = EstadoIntegracionAlutel.Indeterminado;
                    FechaActualizacionUtc = fechaRecuperacionUtc;
                    return Task.FromResult(1);
                }
            }
        }

        private class FakeGateway : IAlutelVigenciaGateway
        {
            private readonly ConcurrentQueue<string> _eventos;
            private readonly ResultadoInvocacionAlutel _resultado;
            private readonly Exception _exception;
            private int _invocaciones;

            public FakeGateway(ConcurrentQueue<string> eventos, ResultadoInvocacionAlutel resultado)
            {
                _eventos = eventos;
                _resultado = resultado;
            }

            public FakeGateway(ConcurrentQueue<string> eventos, Exception exception)
            {
                _eventos = eventos;
                _exception = exception;
            }

            public int Invocaciones { get { return _invocaciones; } }

            public virtual Task<ResultadoInvocacionAlutel> ActualizarAsync(
                string documento,
                TipoVigenciaAlutel tipoVigencia,
                DateTime fechaVigencia,
                CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _invocaciones);
                _eventos.Enqueue("enviar");
                if (_exception != null)
                    throw _exception;

                return Task.FromResult(_resultado);
            }
        }

        private sealed class PausedGateway : FakeGateway
        {
            public PausedGateway(ConcurrentQueue<string> eventos, ResultadoInvocacionAlutel resultado)
                : base(eventos, resultado)
            {
            }

            public TaskCompletionSource<bool> Iniciado { get; } = new TaskCompletionSource<bool>();
            public TaskCompletionSource<bool> Continuar { get; } = new TaskCompletionSource<bool>();

            public override async Task<ResultadoInvocacionAlutel> ActualizarAsync(
                string documento,
                TipoVigenciaAlutel tipoVigencia,
                DateTime fechaVigencia,
                CancellationToken cancellationToken)
            {
                var resultado = base.ActualizarAsync(documento, tipoVigencia, fechaVigencia, cancellationToken);
                Iniciado.SetResult(true);
                await Continuar.Task;
                return await resultado;
            }
        }
    }
}