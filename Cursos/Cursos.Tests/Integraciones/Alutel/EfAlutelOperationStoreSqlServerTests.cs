using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Aplicacion;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Integraciones.Alutel.Infraestructura;
using Cursos.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cursos.Tests
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("SqlServerIntegration")]
    public class EfAlutelOperationStoreSqlServerTests
    {
        private const string VariableHabilitacion = "CURSOS_ALUTEL_SQL_TESTS";
        private const string VariableConexion = "CURSOS_ALUTEL_SQL_CONNECTION";
        private const string VariableBaseConfirmada = "CURSOS_ALUTEL_SQL_DATABASE_CONFIRMATION";

        private string _connectionString;
        private string _documentoPrefijo;
        private int _registroCapacitacionId;

        [TestInitialize]
        public async Task InicializarAsync()
        {
            if (!string.Equals(
                    Environment.GetEnvironmentVariable(VariableHabilitacion),
                    "1",
                    StringComparison.Ordinal))
            {
                Assert.Inconclusive(
                    string.Format(
                        "Prueba SQL omitida. Defina {0}=1 para habilitarla.",
                        VariableHabilitacion));
            }

            _connectionString = Environment.GetEnvironmentVariable(VariableConexion);
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Assert.Inconclusive(
                    string.Format("Prueba SQL omitida. Falta definir {0}.", VariableConexion));
            }

            var connectionBuilder = new SqlConnectionStringBuilder(_connectionString);
            var baseConfirmada = Environment.GetEnvironmentVariable(VariableBaseConfirmada);
            if (string.IsNullOrWhiteSpace(connectionBuilder.InitialCatalog) ||
                !string.Equals(
                    connectionBuilder.InitialCatalog,
                    baseConfirmada,
                    StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive(
                    string.Format(
                        "Prueba SQL omitida. {0} debe coincidir exactamente con Initial Catalog.",
                        VariableBaseConfirmada));
            }

            Database.SetInitializer<ApplicationDbContext>(null);
            _documentoPrefijo = "F306-" + Guid.NewGuid().ToString("N");

            using (var context = CrearContexto())
            {
                _registroCapacitacionId = await context.RegistroCapacitacion
                    .AsNoTracking()
                    .OrderBy(r => r.RegistroCapacitacionID)
                    .Select(r => r.RegistroCapacitacionID)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }

            if (_registroCapacitacionId <= 0)
                Assert.Inconclusive("La base indicada no contiene registros de capacitación.");
        }

        [TestCleanup]
        public void Limpiar()
        {
            if (string.IsNullOrWhiteSpace(_connectionString) ||
                string.IsNullOrWhiteSpace(_documentoPrefijo))
            {
                return;
            }

            using (var context = CrearContexto())
            {
                var operaciones = context.OperacionesIntegracionAlutel
                    .Where(o => o.DocumentoSnapshot.StartsWith(_documentoPrefijo))
                    .ToList();

                if (operaciones.Count == 0)
                    return;

                context.OperacionesIntegracionAlutel.RemoveRange(operaciones);
                context.SaveChanges();
            }
        }

        [TestMethod]
        public async Task ReclamarYCompletar_PersisteOperacionIntentoYRowVersion()
        {
            var documento = Documento("COMPLETAR");
            var store = CrearStore();
            var fechaUtc = DateTime.UtcNow;

            var reclamo = await store.ReclamarAsync(
                    _registroCapacitacionId,
                    documento,
                    TipoVigenciaAlutel.TarjetaVerde,
                    fechaUtc.Date.AddYears(1),
                    "F3-06",
                    fechaUtc,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(EstadoReclamoAlutel.Reclamado, reclamo.Estado);
            Assert.IsTrue(reclamo.OperacionId.HasValue);
            Assert.IsTrue(reclamo.IntentoId.HasValue);

            await store.CompletarAsync(
                    reclamo.OperacionId.Value,
                    reclamo.IntentoId.Value,
                    EstadoIntegracionAlutel.Aceptado,
                    new ResultadoInvocacionAlutel(
                        ResultadoTecnicoAlutel.Aceptado,
                        200,
                        "Aceptado en prueba F3-06.",
                        1,
                        0,
                        "F3-06"),
                    fechaUtc.AddSeconds(1),
                    CancellationToken.None)
                .ConfigureAwait(false);

            using (var context = CrearContexto())
            {
                var operacion = await context.OperacionesIntegracionAlutel
                    .AsNoTracking()
                    .SingleAsync(o => o.OperacionIntegracionAlutelID == reclamo.OperacionId.Value)
                    .ConfigureAwait(false);
                var intento = await context.IntentosIntegracionAlutel
                    .AsNoTracking()
                    .SingleAsync(i => i.IntentoIntegracionAlutelID == reclamo.IntentoId.Value)
                    .ConfigureAwait(false);

                Assert.AreEqual(EstadoIntegracionAlutel.Aceptado, operacion.Estado);
                Assert.IsNotNull(operacion.RowVersion);
                Assert.IsGreaterThan(0, operacion.RowVersion.Length);
                Assert.IsTrue(intento.FechaFinUtc.HasValue);
                Assert.AreEqual(200, intento.CodigoHttp);
                Assert.AreEqual(ResultadoTecnicoAlutel.Aceptado, intento.ResultadoTecnico);
                Assert.AreEqual(1, intento.ProcesadosCorrectamente);
                Assert.AreEqual(0, intento.ProcesadosConError);
            }
        }

        [TestMethod]
        public async Task ReclamarAsync_DosContextosConcurrentes_SoloUnoReclama()
        {
            var documento = Documento("RECLAMO");
            var fechaUtc = DateTime.UtcNow;
            var inicio = new ManualResetEventSlim(false);

            var tareas = Enumerable.Range(0, 2)
                .Select(numero => Task.Run(async () =>
                {
                    inicio.Wait();
                    return await CrearStore().ReclamarAsync(
                            _registroCapacitacionId,
                            documento,
                            TipoVigenciaAlutel.TarjetaAzul,
                            fechaUtc.Date.AddYears(1),
                            "F3-06-" + numero,
                            fechaUtc,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }))
                .ToArray();

            inicio.Set();
            var resultados = await Task.WhenAll(tareas).ConfigureAwait(false);

            Assert.AreEqual(
                1,
                resultados.Count(r => r.Estado == EstadoReclamoAlutel.Reclamado));
            Assert.AreEqual(
                1,
                resultados.Count(r => r.Estado == EstadoReclamoAlutel.NoDisponible));

            using (var context = CrearContexto())
            {
                var operaciones = await context.OperacionesIntegracionAlutel
                    .CountAsync(o => o.DocumentoSnapshot == documento)
                    .ConfigureAwait(false);
                var intentos = await context.IntentosIntegracionAlutel
                    .CountAsync(i => i.Operacion.DocumentoSnapshot == documento)
                    .ConfigureAwait(false);

                Assert.AreEqual(1, operaciones);
                Assert.AreEqual(1, intentos);
            }
        }

        [TestMethod]
        public async Task CompletarAsync_DosContextosConcurrentes_SoloUnoCompleta()
        {
            var documento = Documento("FINALIZAR");
            var fechaUtc = DateTime.UtcNow;
            var reclamo = await CrearStore().ReclamarAsync(
                    _registroCapacitacionId,
                    documento,
                    TipoVigenciaAlutel.Refresh,
                    fechaUtc.Date.AddYears(1),
                    "F3-06",
                    fechaUtc,
                    CancellationToken.None)
                .ConfigureAwait(false);
            var inicio = new ManualResetEventSlim(false);

            var tareas = new[]
            {
                EjecutarCapturandoAsync(
                    inicio,
                    () => CrearStore().CompletarAsync(
                        reclamo.OperacionId.Value,
                        reclamo.IntentoId.Value,
                        EstadoIntegracionAlutel.Aceptado,
                        new ResultadoInvocacionAlutel(
                            ResultadoTecnicoAlutel.Aceptado,
                            200,
                            "Aceptado en concurrencia."),
                        fechaUtc.AddSeconds(1),
                        CancellationToken.None)),
                EjecutarCapturandoAsync(
                    inicio,
                    () => CrearStore().CompletarAsync(
                        reclamo.OperacionId.Value,
                        reclamo.IntentoId.Value,
                        EstadoIntegracionAlutel.Fallido,
                        new ResultadoInvocacionAlutel(
                            ResultadoTecnicoAlutel.ErrorDefinitivo,
                            500,
                            "Fallido en concurrencia."),
                        fechaUtc.AddSeconds(1),
                        CancellationToken.None))
            };

            inicio.Set();
            var excepciones = await Task.WhenAll(tareas).ConfigureAwait(false);

            Assert.AreEqual(1, excepciones.Count(e => e == null));
            Assert.AreEqual(1, excepciones.Count(e => e is AlutelPersistenceException));

            using (var context = CrearContexto())
            {
                var operacion = await context.OperacionesIntegracionAlutel
                    .AsNoTracking()
                    .SingleAsync(o => o.OperacionIntegracionAlutelID == reclamo.OperacionId.Value)
                    .ConfigureAwait(false);
                var intento = await context.IntentosIntegracionAlutel
                    .AsNoTracking()
                    .SingleAsync(i => i.IntentoIntegracionAlutelID == reclamo.IntentoId.Value)
                    .ConfigureAwait(false);

                Assert.AreNotEqual(EstadoIntegracionAlutel.EnProceso, operacion.Estado);
                Assert.IsTrue(intento.FechaFinUtc.HasValue);
            }
        }

        [TestMethod]
        public async Task RecuperarEnProcesoAsync_OperacionDePruebaAntigua_LaMarcaIndeterminada()
        {
            var documento = Documento("RECUPERAR");
            var fechaAntigua = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var umbralSeguro = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaRecuperacion = DateTime.UtcNow;
            var store = CrearStore();

            using (var context = CrearContexto())
            {
                var operacionesAjenas = await context.OperacionesIntegracionAlutel
                    .AsNoTracking()
                    .CountAsync(o =>
                        o.Estado == EstadoIntegracionAlutel.EnProceso &&
                        o.FechaActualizacionUtc <= umbralSeguro &&
                        !o.DocumentoSnapshot.StartsWith(_documentoPrefijo))
                    .ConfigureAwait(false);

                if (operacionesAjenas != 0)
                {
                    Assert.Inconclusive(
                        "La recuperación fue omitida porque existen operaciones ajenas anteriores al umbral seguro.");
                }
            }

            var reclamo = await store.ReclamarAsync(
                    _registroCapacitacionId,
                    documento,
                    TipoVigenciaAlutel.TarjetaVerde,
                    fechaRecuperacion.Date.AddYears(1),
                    "F3-06",
                    fechaAntigua,
                    CancellationToken.None)
                .ConfigureAwait(false);

            var recuperadas = await store.RecuperarEnProcesoAsync(
                    umbralSeguro,
                    fechaRecuperacion,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, recuperadas);

            using (var context = CrearContexto())
            {
                var operacion = await context.OperacionesIntegracionAlutel
                    .AsNoTracking()
                    .SingleAsync(o => o.OperacionIntegracionAlutelID == reclamo.OperacionId.Value)
                    .ConfigureAwait(false);
                var intento = await context.IntentosIntegracionAlutel
                    .AsNoTracking()
                    .SingleAsync(i => i.IntentoIntegracionAlutelID == reclamo.IntentoId.Value)
                    .ConfigureAwait(false);

                Assert.AreEqual(EstadoIntegracionAlutel.Indeterminado, operacion.Estado);
                Assert.IsTrue(intento.FechaFinUtc.HasValue);
                Assert.AreEqual(ResultadoTecnicoAlutel.Indeterminado, intento.ResultadoTecnico);
            }
        }

        private ApplicationDbContext CrearContexto()
        {
            return new ApplicationDbContext(_connectionString);
        }

        private EfAlutelOperationStore CrearStore()
        {
            var configuracion = new AlutelConfiguration(
                false,
                null,
                null,
                null,
                null,
                null,
                TimeSpan.FromSeconds(30),
                1,
                TimeSpan.FromSeconds(60),
                "F3-06");

            return new EfAlutelOperationStore(configuracion, CrearContexto);
        }

        private string Documento(string sufijo)
        {
            return _documentoPrefijo + "-" + sufijo;
        }

        private static async Task<Exception> EjecutarCapturandoAsync(
            ManualResetEventSlim inicio,
            Func<Task> accion)
        {
            return await Task.Run(async () =>
            {
                inicio.Wait();
                try
                {
                    await accion().ConfigureAwait(false);
                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }).ConfigureAwait(false);
        }
    }
}
