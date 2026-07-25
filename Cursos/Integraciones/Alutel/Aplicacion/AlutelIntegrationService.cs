using System;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Models;

namespace Cursos.Integraciones.Alutel.Aplicacion
{
    public enum EstadoReclamoAlutel
    {
        Reclamado = 0,
        YaAceptado = 1,
        NoDisponible = 2
    }

    public sealed class ReclamoOperacionAlutel
    {
        private ReclamoOperacionAlutel(EstadoReclamoAlutel estado, long? operacionId, long? intentoId, string mensaje)
        {
            Estado = estado;
            OperacionId = operacionId;
            IntentoId = intentoId;
            Mensaje = mensaje;
        }

        public EstadoReclamoAlutel Estado { get; private set; }
        public long? OperacionId { get; private set; }
        public long? IntentoId { get; private set; }
        public string Mensaje { get; private set; }

        public static ReclamoOperacionAlutel Reclamado(long operacionId, long intentoId)
        {
            return new ReclamoOperacionAlutel(EstadoReclamoAlutel.Reclamado, operacionId, intentoId, string.Empty);
        }

        public static ReclamoOperacionAlutel YaAceptado(long operacionId)
        {
            return new ReclamoOperacionAlutel(
                EstadoReclamoAlutel.YaAceptado,
                operacionId,
                null,
                "La vigencia ya fue aceptada por LENEL.");
        }

        public static ReclamoOperacionAlutel NoDisponible(long? operacionId, string mensaje)
        {
            return new ReclamoOperacionAlutel(EstadoReclamoAlutel.NoDisponible, operacionId, null, mensaje);
        }
    }

    public sealed class ResultadoInvocacionAlutel
    {
        public ResultadoInvocacionAlutel(
            ResultadoTecnicoAlutel resultado,
            int? codigoHttp,
            string mensajeSanitizado,
            int? procesadosCorrectamente = null,
            int? procesadosConError = null,
            string correlationIdProveedor = null)
        {
            Resultado = resultado;
            CodigoHttp = codigoHttp;
            MensajeSanitizado = string.IsNullOrWhiteSpace(mensajeSanitizado)
                ? "Alutel no proporcionó un detalle del resultado."
                : mensajeSanitizado;
            ProcesadosCorrectamente = procesadosCorrectamente;
            ProcesadosConError = procesadosConError;
            CorrelationIdProveedor = correlationIdProveedor;
        }

        public ResultadoTecnicoAlutel Resultado { get; private set; }
        public int? CodigoHttp { get; private set; }
        public string MensajeSanitizado { get; private set; }
        public int? ProcesadosCorrectamente { get; private set; }
        public int? ProcesadosConError { get; private set; }
        public string CorrelationIdProveedor { get; private set; }
    }

    public interface IAlutelVigenciaGateway
    {
        Task<ResultadoInvocacionAlutel> ActualizarAsync(
            string documento,
            TipoVigenciaAlutel tipoVigencia,
            DateTime fechaVigencia,
            CancellationToken cancellationToken);
    }

    public interface IAlutelOperationStore
    {
        Task<ReclamoOperacionAlutel> ReclamarAsync(
            int registroCapacitacionId,
            string documento,
            TipoVigenciaAlutel tipoVigencia,
            DateTime fechaVigencia,
            string usuarioOrigen,
            DateTime fechaUtc,
            CancellationToken cancellationToken);

        Task CompletarAsync(
            long operacionId,
            long intentoId,
            EstadoIntegracionAlutel estado,
            ResultadoInvocacionAlutel resultado,
            DateTime fechaUtc,
            CancellationToken cancellationToken);

        Task<int> RecuperarEnProcesoAsync(
            DateTime anterioresAUtc,
            DateTime fechaRecuperacionUtc,
            CancellationToken cancellationToken);
    }

    public sealed class AlutelPersistenceException : Exception
    {
        public AlutelPersistenceException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    public sealed class ResultadoOrquestacionAlutel
    {
        private ResultadoOrquestacionAlutel(
            EstadoIntegracionAlutel? estado,
            long? operacionId,
            bool solicitudEnviada,
            string mensaje)
        {
            Estado = estado;
            OperacionId = operacionId;
            SolicitudEnviada = solicitudEnviada;
            Mensaje = mensaje;
        }

        public EstadoIntegracionAlutel? Estado { get; private set; }
        public long? OperacionId { get; private set; }
        public bool SolicitudEnviada { get; private set; }
        public string Mensaje { get; private set; }

        public static ResultadoOrquestacionAlutel NoElegible(string mensaje)
        {
            return new ResultadoOrquestacionAlutel(null, null, false, mensaje);
        }

        public static ResultadoOrquestacionAlutel NoDisponible(long? operacionId, string mensaje)
        {
            return new ResultadoOrquestacionAlutel(null, operacionId, false, mensaje);
        }

        public static ResultadoOrquestacionAlutel Completado(
            EstadoIntegracionAlutel estado,
            long operacionId,
            bool solicitudEnviada,
            string mensaje)
        {
            return new ResultadoOrquestacionAlutel(estado, operacionId, solicitudEnviada, mensaje);
        }
    }

    public interface IAlutelIntegrationService
    {
        Task<ResultadoOrquestacionAlutel> ProcesarAsync(
            RegistroCapacitacion registro,
            string usuarioOrigen,
            CancellationToken cancellationToken);

        Task<int> RecuperarEnProcesoAsync(TimeSpan antiguedadMinima, CancellationToken cancellationToken);
    }

    public sealed class AlutelIntegrationService : IAlutelIntegrationService
    {
        private readonly IAlutelEligibilityPolicy _eligibilityPolicy;
        private readonly IAlutelVigenciaGateway _gateway;
        private readonly IAlutelOperationStore _operationStore;
        private readonly IClock _clock;

        public AlutelIntegrationService(
            IAlutelEligibilityPolicy eligibilityPolicy,
            IAlutelVigenciaGateway gateway,
            IAlutelOperationStore operationStore,
            IClock clock)
        {
            if (eligibilityPolicy == null)
                throw new ArgumentNullException("eligibilityPolicy");
            if (gateway == null)
                throw new ArgumentNullException("gateway");
            if (operationStore == null)
                throw new ArgumentNullException("operationStore");
            if (clock == null)
                throw new ArgumentNullException("clock");

            _eligibilityPolicy = eligibilityPolicy;
            _gateway = gateway;
            _operationStore = operationStore;
            _clock = clock;
        }

        public async Task<ResultadoOrquestacionAlutel> ProcesarAsync(
            RegistroCapacitacion registro,
            string usuarioOrigen,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var elegibilidad = _eligibilityPolicy.Evaluar(registro);
            if (!elegibilidad.EsElegible)
                return ResultadoOrquestacionAlutel.NoElegible(elegibilidad.Mensaje);
            if (registro.RegistroCapacitacionID <= 0)
                return ResultadoOrquestacionAlutel.NoElegible("El registro de capacitación todavía no fue persistido.");
            if (string.IsNullOrWhiteSpace(usuarioOrigen))
                return ResultadoOrquestacionAlutel.NoElegible("No se pudo identificar al usuario que inició el envío.");
            if (usuarioOrigen.Length > 256)
                return ResultadoOrquestacionAlutel.NoElegible("La identificación del usuario excede el máximo permitido.");

            var documento = registro.Capacitado.Documento;
            if (documento.Length > 64)
                return ResultadoOrquestacionAlutel.NoElegible("El documento excede el máximo permitido para la auditoría.");

            var tipoVigencia = registro.Jornada.Curso.TipoVigenciaAlutel.Value;
            var fechaVigencia = registro.FechaVencimiento.Value;
            ReclamoOperacionAlutel reclamo;
            try
            {
                reclamo = await _operationStore.ReclamarAsync(
                    registro.RegistroCapacitacionID,
                    documento,
                    tipoVigencia,
                    fechaVigencia,
                    usuarioOrigen,
                    _clock.UtcNow,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (AlutelPersistenceException)
            {
                return ResultadoOrquestacionAlutel.NoDisponible(
                    null,
                    "No fue posible reservar la operación para el envío.");
            }

            if (reclamo.Estado == EstadoReclamoAlutel.YaAceptado)
                return ResultadoOrquestacionAlutel.Completado(
                    EstadoIntegracionAlutel.Aceptado,
                    reclamo.OperacionId.Value,
                    false,
                    reclamo.Mensaje);
            if (reclamo.Estado != EstadoReclamoAlutel.Reclamado)
                return ResultadoOrquestacionAlutel.NoDisponible(reclamo.OperacionId, reclamo.Mensaje);

            ResultadoInvocacionAlutel resultadoInvocacion;
            try
            {
                resultadoInvocacion = await _gateway.ActualizarAsync(
                    documento,
                    tipoVigencia,
                    fechaVigencia,
                    cancellationToken).ConfigureAwait(false);
                if (resultadoInvocacion == null)
                    resultadoInvocacion = CrearResultadoIndeterminado("Alutel no devolvió un resultado interpretable.");
            }
            catch (OperationCanceledException)
            {
                resultadoInvocacion = CrearResultadoIndeterminado("El envío fue cancelado y no se pudo confirmar su resultado.");
                await CompletarSinPropagarAsync(reclamo, resultadoInvocacion).ConfigureAwait(false);
                throw;
            }
            catch (Exception)
            {
                resultadoInvocacion = CrearResultadoIndeterminado("Ocurrió un error inesperado y no se pudo confirmar el resultado del envío.");
            }

            var estado = MapearEstado(resultadoInvocacion.Resultado);
            try
            {
                // Una respuesta del proveedor debe auditarse aunque la solicitud original haya sido cancelada.
                await _operationStore.CompletarAsync(
                    reclamo.OperacionId.Value,
                    reclamo.IntentoId.Value,
                    estado,
                    resultadoInvocacion,
                    _clock.UtcNow,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (AlutelPersistenceException)
            {
                return ResultadoOrquestacionAlutel.Completado(
                    EstadoIntegracionAlutel.Indeterminado,
                    reclamo.OperacionId.Value,
                    true,
                    "Alutel respondió, pero no fue posible guardar el resultado local. La operación requiere reconciliación.");
            }

            return ResultadoOrquestacionAlutel.Completado(
                estado,
                reclamo.OperacionId.Value,
                true,
                resultadoInvocacion.MensajeSanitizado);
        }

        public Task<int> RecuperarEnProcesoAsync(TimeSpan antiguedadMinima, CancellationToken cancellationToken)
        {
            if (antiguedadMinima <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("antiguedadMinima", "La antigüedad mínima debe ser mayor que cero.");

            var ahora = _clock.UtcNow;
            return _operationStore.RecuperarEnProcesoAsync(
                ahora.Subtract(antiguedadMinima),
                ahora,
                cancellationToken);
        }

        private async Task CompletarSinPropagarAsync(
            ReclamoOperacionAlutel reclamo,
            ResultadoInvocacionAlutel resultado)
        {
            try
            {
                await _operationStore.CompletarAsync(
                    reclamo.OperacionId.Value,
                    reclamo.IntentoId.Value,
                    EstadoIntegracionAlutel.Indeterminado,
                    resultado,
                    _clock.UtcNow,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (AlutelPersistenceException)
            {
            }
        }

        private static ResultadoInvocacionAlutel CrearResultadoIndeterminado(string mensaje)
        {
            return new ResultadoInvocacionAlutel(ResultadoTecnicoAlutel.Indeterminado, null, mensaje);
        }

        private static EstadoIntegracionAlutel MapearEstado(ResultadoTecnicoAlutel resultado)
        {
            switch (resultado)
            {
                case ResultadoTecnicoAlutel.Aceptado:
                    return EstadoIntegracionAlutel.Aceptado;
                case ResultadoTecnicoAlutel.RechazadoFuncional:
                case ResultadoTecnicoAlutel.ErrorReintentable:
                case ResultadoTecnicoAlutel.ErrorDefinitivo:
                    return EstadoIntegracionAlutel.Fallido;
                case ResultadoTecnicoAlutel.Indeterminado:
                    return EstadoIntegracionAlutel.Indeterminado;
                default:
                    return EstadoIntegracionAlutel.Indeterminado;
            }
        }
    }
}