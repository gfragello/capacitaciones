using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Aplicacion;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Models;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public sealed class EfAlutelOperationStore : IAlutelOperationStore
    {
        private const string MensajeIntentoIniciado = "Intento iniciado; resultado pendiente.";
        private const string MensajeOperacionRecuperada = "La aplicación se reinició o interrumpió antes de confirmar el resultado. Requiere reconciliación.";

        private readonly Func<ApplicationDbContext> _contextFactory;
        private readonly IAlutelConfiguration _configuration;

        public EfAlutelOperationStore(IAlutelConfiguration configuration)
            : this(configuration, () => new ApplicationDbContext())
        {
        }

        public EfAlutelOperationStore(
            IAlutelConfiguration configuration,
            Func<ApplicationDbContext> contextFactory)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            if (contextFactory == null)
                throw new ArgumentNullException("contextFactory");

            _configuration = configuration;
            _contextFactory = contextFactory;
        }

        public async Task<ReclamoOperacionAlutel> ReclamarAsync(
            int registroCapacitacionId,
            string documento,
            TipoVigenciaAlutel tipoVigencia,
            DateTime fechaVigencia,
            string usuarioOrigen,
            DateTime fechaUtc,
            CancellationToken cancellationToken)
        {
            ValidarEntorno();

            using (var context = _contextFactory())
            using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    await BloquearReclamoAsync(
                            context,
                            documento,
                            tipoVigencia,
                            cancellationToken)
                        .ConfigureAwait(false);

                    // Serializable mantiene bloqueado el rango del documento hasta crear o reclamar la operación.
                    var operacionesMismaVigencia = context.OperacionesIntegracionAlutel
                        .Where(o => o.DocumentoSnapshot == documento && o.TipoVigencia == tipoVigencia);

                    var aceptada = await operacionesMismaVigencia
                        .Where(o => o.Estado == EstadoIntegracionAlutel.Aceptado && o.FechaVigencia >= fechaVigencia)
                        .OrderByDescending(o => o.FechaVigencia)
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (aceptada != null)
                    {
                        transaction.Commit();
                        return ReclamoOperacionAlutel.YaAceptado(aceptada.OperacionIntegracionAlutelID);
                    }

                    var incompatible = await operacionesMismaVigencia
                        .Where(o => o.Estado == EstadoIntegracionAlutel.EnProceso ||
                                    o.Estado == EstadoIntegracionAlutel.Indeterminado)
                        .OrderByDescending(o => o.FechaActualizacionUtc)
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (incompatible != null)
                    {
                        transaction.Commit();
                        return ReclamoOperacionAlutel.NoDisponible(
                            incompatible.OperacionIntegracionAlutelID,
                            incompatible.Estado == EstadoIntegracionAlutel.EnProceso
                                ? "Ya existe un envío en proceso para esta vigencia."
                                : "Existe un envío con resultado indeterminado que debe reconciliarse.");
                    }

                    var operacion = await operacionesMismaVigencia
                        .Where(o => o.RegistroCapacitacionID == registroCapacitacionId &&
                                    o.FechaVigencia == fechaVigencia)
                        .OrderByDescending(o => o.OperacionIntegracionAlutelID)
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);

                    int numeroIntento;
                    if (operacion == null)
                    {
                        operacion = new OperacionIntegracionAlutel
                        {
                            RegistroCapacitacionID = registroCapacitacionId,
                            DocumentoSnapshot = documento,
                            TipoVigencia = tipoVigencia,
                            FechaVigencia = fechaVigencia,
                            Estado = EstadoIntegracionAlutel.EnProceso,
                            FechaCreacionUtc = fechaUtc,
                            FechaActualizacionUtc = fechaUtc,
                            UsuarioOrigen = usuarioOrigen
                        };
                        context.OperacionesIntegracionAlutel.Add(operacion);
                        numeroIntento = 1;
                    }
                    else
                    {
                        operacion.Estado = EstadoIntegracionAlutel.EnProceso;
                        operacion.FechaActualizacionUtc = fechaUtc;
                        operacion.UsuarioOrigen = usuarioOrigen;
                        numeroIntento = (await context.IntentosIntegracionAlutel
                            .Where(i => i.OperacionIntegracionAlutelID == operacion.OperacionIntegracionAlutelID)
                            .Select(i => (int?)i.NumeroIntento)
                            .MaxAsync(cancellationToken)
                            .ConfigureAwait(false) ?? 0) + 1;
                    }

                    var intento = new IntentoIntegracionAlutel
                    {
                        Operacion = operacion,
                        BatchId = Guid.NewGuid(),
                        NumeroIntento = numeroIntento,
                        Entorno = _configuration.Entorno,
                        FechaInicioUtc = fechaUtc,
                        ResultadoTecnico = ResultadoTecnicoAlutel.Indeterminado,
                        MensajeSanitizado = MensajeIntentoIniciado
                    };
                    context.IntentosIntegracionAlutel.Add(intento);

                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    transaction.Commit();
                    return ReclamoOperacionAlutel.Reclamado(
                        operacion.OperacionIntegracionAlutelID,
                        intento.IntentoIntegracionAlutelID);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    throw new AlutelPersistenceException("La operación Alutel fue reclamada por otro proceso.", ex);
                }
                catch (Exception ex)
                {
                    throw CrearExcepcionPersistencia("No fue posible reclamar la operación Alutel.", ex);
                }
            }
        }

        public async Task CompletarAsync(
            long operacionId,
            long intentoId,
            EstadoIntegracionAlutel estado,
            ResultadoInvocacionAlutel resultado,
            DateTime fechaUtc,
            CancellationToken cancellationToken)
        {
            if (resultado == null)
                throw new ArgumentNullException("resultado");

            using (var context = _contextFactory())
            using (var transaction = context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var operacion = await context.OperacionesIntegracionAlutel
                        .SingleOrDefaultAsync(o => o.OperacionIntegracionAlutelID == operacionId, cancellationToken)
                        .ConfigureAwait(false);
                    var intento = await context.IntentosIntegracionAlutel
                        .SingleOrDefaultAsync(i => i.IntentoIntegracionAlutelID == intentoId &&
                                                   i.OperacionIntegracionAlutelID == operacionId,
                                              cancellationToken)
                        .ConfigureAwait(false);

                    if (operacion == null || intento == null)
                        throw new AlutelPersistenceException("La operación o el intento Alutel ya no existe.");
                    if (operacion.Estado != EstadoIntegracionAlutel.EnProceso || intento.FechaFinUtc.HasValue)
                        throw new AlutelPersistenceException("La operación Alutel fue completada por otro proceso.");

                    intento.FechaFinUtc = fechaUtc;
                    intento.CodigoHttp = resultado.CodigoHttp;
                    intento.ResultadoTecnico = resultado.Resultado;
                    intento.MensajeSanitizado = Limitar(resultado.MensajeSanitizado, 1000);
                    intento.ProcesadosCorrectamente = resultado.ProcesadosCorrectamente;
                    intento.ProcesadosConError = resultado.ProcesadosConError;
                    intento.CorrelationIdProveedor = Limitar(resultado.CorrelationIdProveedor, 128);

                    operacion.Estado = estado;
                    operacion.FechaActualizacionUtc = fechaUtc;

                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    transaction.Commit();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    throw new AlutelPersistenceException("La operación Alutel fue completada por otro proceso.", ex);
                }
                catch (Exception ex)
                {
                    throw CrearExcepcionPersistencia("No fue posible guardar el resultado Alutel.", ex);
                }
            }
        }

        public async Task<int> RecuperarEnProcesoAsync(
            DateTime anterioresAUtc,
            DateTime fechaRecuperacionUtc,
            CancellationToken cancellationToken)
        {
            using (var context = _contextFactory())
            using (var transaction = context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var operaciones = await context.OperacionesIntegracionAlutel
                        .Include(o => o.Intentos)
                        .Where(o => o.Estado == EstadoIntegracionAlutel.EnProceso &&
                                    o.FechaActualizacionUtc <= anterioresAUtc)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                    foreach (var operacion in operaciones)
                    {
                        operacion.Estado = EstadoIntegracionAlutel.Indeterminado;
                        operacion.FechaActualizacionUtc = fechaRecuperacionUtc;

                        foreach (var intento in operacion.Intentos.Where(i => !i.FechaFinUtc.HasValue))
                        {
                            intento.FechaFinUtc = fechaRecuperacionUtc;
                            intento.ResultadoTecnico = ResultadoTecnicoAlutel.Indeterminado;
                            intento.MensajeSanitizado = MensajeOperacionRecuperada;
                        }
                    }

                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    transaction.Commit();
                    return operaciones.Count;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    throw new AlutelPersistenceException("Una operación Alutel cambió mientras se intentaba recuperarla.", ex);
                }
                catch (Exception ex)
                {
                    throw CrearExcepcionPersistencia("No fue posible recuperar las operaciones Alutel pendientes.", ex);
                }
            }
        }

        private static async Task BloquearReclamoAsync(
            ApplicationDbContext context,
            string documento,
            TipoVigenciaAlutel tipoVigencia,
            CancellationToken cancellationToken)
        {
            var recurso = string.Format(
                "Cursos:Alutel:Reclamo:{0}:{1}",
                (int)tipoVigencia,
                documento);
            var resultado = await context.Database.SqlQuery<int>(
                    @"DECLARE @Resultado int;
                      EXEC @Resultado = sys.sp_getapplock
                          @Resource = @Recurso,
                          @LockMode = 'Exclusive',
                          @LockOwner = 'Transaction',
                          @LockTimeout = 15000;
                      SELECT @Resultado;",
                    new SqlParameter("@Recurso", recurso))
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);

            if (resultado < 0)
            {
                throw new AlutelPersistenceException(
                    "No fue posible obtener el bloqueo para reclamar la operación Alutel.");
            }
        }

        private void ValidarEntorno()
        {
            if (string.IsNullOrWhiteSpace(_configuration.Entorno) || _configuration.Entorno.Length > 32)
                throw new AlutelPersistenceException("El entorno Alutel no es válido para la auditoría.");
        }

        private static string Limitar(string valor, int longitudMaxima)
        {
            return string.IsNullOrEmpty(valor) || valor.Length <= longitudMaxima
                ? valor
                : valor.Substring(0, longitudMaxima);
        }

        private static AlutelPersistenceException CrearExcepcionPersistencia(string mensaje, Exception exception)
        {
            var persistenceException = exception as AlutelPersistenceException;
            if (persistenceException != null)
                return persistenceException;

            return new AlutelPersistenceException(mensaje, exception);
        }
    }
}
