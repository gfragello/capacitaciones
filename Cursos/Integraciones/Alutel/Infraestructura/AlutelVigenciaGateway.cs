using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cursos.Integraciones.Alutel.Aplicacion;
using Cursos.Integraciones.Alutel.Dominio;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public sealed class AlutelVigenciaGateway : IAlutelVigenciaGateway
    {
        private readonly IAlutelMappingService _mappingService;
        private readonly IAlutelSafetyCardsClient _client;

        public AlutelVigenciaGateway(IAlutelMappingService mappingService, IAlutelSafetyCardsClient client)
        {
            if (mappingService == null)
                throw new ArgumentNullException("mappingService");
            if (client == null)
                throw new ArgumentNullException("client");

            _mappingService = mappingService;
            _client = client;
        }

        public async Task<ResultadoInvocacionAlutel> ActualizarAsync(
            string documento,
            TipoVigenciaAlutel tipoVigencia,
            DateTime fechaVigencia,
            CancellationToken cancellationToken)
        {
            try
            {
                var item = _mappingService.Mapear(documento, tipoVigencia, fechaVigencia);
                var resultado = await _client.ActualizarAsync(
                    new List<SafetyCardUpdate> { item },
                    cancellationToken).ConfigureAwait(false);

                return new ResultadoInvocacionAlutel(
                    resultado.Resultado,
                    resultado.CodigoHttp,
                    resultado.MensajeSanitizado,
                    resultado.ProcesadosCorrectamente,
                    resultado.ProcesadosConError);
            }
            catch (AlutelConfigurationException)
            {
                return new ResultadoInvocacionAlutel(
                    ResultadoTecnicoAlutel.ErrorDefinitivo,
                    null,
                    "La configuración de Alutel está incompleta o deshabilitada.");
            }
        }
    }
}