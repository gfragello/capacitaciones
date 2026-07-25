using System;
using System.Globalization;
using Cursos.Integraciones.Alutel.Dominio;

namespace Cursos.Integraciones.Alutel.Infraestructura
{
    public interface IAlutelMappingService
    {
        SafetyCardUpdate Mapear(string documento, TipoVigenciaAlutel? tipoVigencia, DateTime? fechaVencimiento);
    }

    public sealed class AlutelMappingService : IAlutelMappingService
    {
        public SafetyCardUpdate Mapear(string documento, TipoVigenciaAlutel? tipoVigencia, DateTime? fechaVencimiento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                throw new ArgumentException("El documento es obligatorio.", "documento");
            if (!tipoVigencia.HasValue)
                throw new ArgumentException("El curso no tiene un tipo de vigencia Alutel configurado.", "tipoVigencia");
            if (!fechaVencimiento.HasValue)
                throw new ArgumentException("El registro no tiene fecha de vencimiento.", "fechaVencimiento");

            var fecha = fechaVencimiento.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var item = new SafetyCardUpdate { DocumentNumber = documento };

            switch (tipoVigencia.Value)
            {
                case TipoVigenciaAlutel.TarjetaVerde:
                    item.VtoTarjetaVerde = fecha;
                    break;
                case TipoVigenciaAlutel.TarjetaAzul:
                    item.VtoTarjetaAzul = fecha;
                    break;
                case TipoVigenciaAlutel.Refresh:
                    item.VtoActualizacionSeguridad = fecha;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("tipoVigencia", "El tipo de vigencia Alutel no es válido.");
            }

            return item;
        }
    }

    internal static class SafetyCardUpdateValidator
    {
        public static void Validar(SafetyCardUpdate item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            if (string.IsNullOrWhiteSpace(item.DocumentNumber))
                throw new ArgumentException("Cada item debe incluir un documento.", "item");

            var cantidadVigencias = 0;
            cantidadVigencias += string.IsNullOrEmpty(item.VtoTarjetaVerde) ? 0 : 1;
            cantidadVigencias += string.IsNullOrEmpty(item.VtoTarjetaAzul) ? 0 : 1;
            cantidadVigencias += string.IsNullOrEmpty(item.VtoActualizacionSeguridad) ? 0 : 1;
            if (cantidadVigencias != 1)
                throw new ArgumentException("Cada item debe incluir exactamente una vigencia.", "item");

            ValidarFecha(item.VtoTarjetaVerde);
            ValidarFecha(item.VtoTarjetaAzul);
            ValidarFecha(item.VtoActualizacionSeguridad);
        }

        private static void ValidarFecha(string fecha)
        {
            DateTime valor;
            if (!string.IsNullOrEmpty(fecha) &&
                !DateTime.TryParseExact(fecha, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
                throw new ArgumentException("Las vigencias deben usar el formato yyyyMMdd.", "item");
        }
    }
}
