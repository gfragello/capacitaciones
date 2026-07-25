using System;
using System.Collections.Generic;
using Cursos.Integraciones.Alutel.Dominio;
using Newtonsoft.Json;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public sealed class SafetyCardUpdate
    {
        [JsonProperty("documentNumber")]
        public string DocumentNumber { get; set; }

        [JsonProperty("vtoTarjetaVerde", NullValueHandling = NullValueHandling.Ignore)]
        public string VtoTarjetaVerde { get; set; }

        [JsonProperty("vtoTarjetaAzul", NullValueHandling = NullValueHandling.Ignore)]
        public string VtoTarjetaAzul { get; set; }

        [JsonProperty("vtoActualizacionSeguridad", NullValueHandling = NullValueHandling.Ignore)]
        public string VtoActualizacionSeguridad { get; set; }
    }

    internal sealed class SafetyCardsResponse
    {
        [JsonProperty("successfulProcessed")]
        public int SuccessfulProcessed { get; set; }

        [JsonProperty("failedProcessed")]
        public int FailedProcessed { get; set; }

        [JsonProperty("failedDocuments")]
        public List<string> FailedDocuments { get; set; }
    }

    internal sealed class OAuthTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }

    public sealed class AlutelClientResult
    {
        private AlutelClientResult(ResultadoTecnicoAlutel resultado, int? codigoHttp, string mensaje, int? exitosos, int? fallidos)
        {
            Resultado = resultado;
            CodigoHttp = codigoHttp;
            MensajeSanitizado = mensaje;
            ProcesadosCorrectamente = exitosos;
            ProcesadosConError = fallidos;
        }

        public ResultadoTecnicoAlutel Resultado { get; private set; }
        public int? CodigoHttp { get; private set; }
        public string MensajeSanitizado { get; private set; }
        public int? ProcesadosCorrectamente { get; private set; }
        public int? ProcesadosConError { get; private set; }

        public static AlutelClientResult Crear(ResultadoTecnicoAlutel resultado, int? codigoHttp, string mensaje, int? exitosos = null, int? fallidos = null)
        {
            return new AlutelClientResult(resultado, codigoHttp, mensaje, exitosos, fallidos);
        }
    }
}
