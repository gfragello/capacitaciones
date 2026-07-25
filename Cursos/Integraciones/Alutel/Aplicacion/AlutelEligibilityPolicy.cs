using System;
using System.Collections.Generic;
using System.Linq;
using Cursos.Integraciones.Alutel.Dominio;
using Cursos.Models;
using Cursos.Models.Enums;

namespace Cursos.Integraciones.Alutel.Aplicacion
{
    public enum MotivoNoElegibleAlutel
    {
        Elegible = 0,
        RegistroInexistente = 1,
        CursoNoConfigurado = 2,
        RegistroNoAprobado = 3,
        SinFechaVencimiento = 4,
        VigenciaVencida = 5,
        DocumentoNoDisponible = 6
    }

    public sealed class ResultadoElegibilidadAlutel
    {
        private static readonly ResultadoElegibilidadAlutel _elegible =
            new ResultadoElegibilidadAlutel(MotivoNoElegibleAlutel.Elegible, string.Empty);

        private ResultadoElegibilidadAlutel(MotivoNoElegibleAlutel motivo, string mensaje)
        {
            Motivo = motivo;
            Mensaje = mensaje;
        }

        public MotivoNoElegibleAlutel Motivo { get; private set; }

        /// <summary>
        /// Motivo presentable al operador. Vacío cuando el registro es elegible.
        /// </summary>
        public string Mensaje { get; private set; }

        public bool EsElegible { get { return Motivo == MotivoNoElegibleAlutel.Elegible; } }

        public static ResultadoElegibilidadAlutel Elegible()
        {
            return _elegible;
        }

        public static ResultadoElegibilidadAlutel NoElegible(MotivoNoElegibleAlutel motivo, string mensaje)
        {
            return new ResultadoElegibilidadAlutel(motivo, mensaje);
        }
    }

    public interface IAlutelEligibilityPolicy
    {
        ResultadoElegibilidadAlutel Evaluar(RegistroCapacitacion registro);

        RegistroCapacitacion SeleccionarRegistro(IEnumerable<RegistroCapacitacion> registros);
    }

    /// <summary>
    /// Reglas de elegibilidad y selección confirmadas en el Gate 1.
    /// No accede a la base de datos ni a HttpContext.
    /// </summary>
    public sealed class AlutelEligibilityPolicy : IAlutelEligibilityPolicy
    {
        private readonly IClock _clock;

        public AlutelEligibilityPolicy(IClock clock)
        {
            if (clock == null)
                throw new ArgumentNullException("clock");

            _clock = clock;
        }

        public ResultadoElegibilidadAlutel Evaluar(RegistroCapacitacion registro)
        {
            if (registro == null)
                return ResultadoElegibilidadAlutel.NoElegible(
                    MotivoNoElegibleAlutel.RegistroInexistente,
                    "El registro de capacitación no existe.");

            if (!ObtenerTipoVigencia(registro).HasValue)
                return ResultadoElegibilidadAlutel.NoElegible(
                    MotivoNoElegibleAlutel.CursoNoConfigurado,
                    "El curso no está configurado para actualizar tarjetas en LENEL.");

            if (registro.Estado != EstadosRegistroCapacitacion.Aprobado)
                return ResultadoElegibilidadAlutel.NoElegible(
                    MotivoNoElegibleAlutel.RegistroNoAprobado,
                    "Solo se envían registros aprobados.");

            if (!registro.FechaVencimiento.HasValue)
                return ResultadoElegibilidadAlutel.NoElegible(
                    MotivoNoElegibleAlutel.SinFechaVencimiento,
                    "El registro no tiene fecha de vencimiento.");

            if (registro.FechaVencimiento.Value <= _clock.Now)
                return ResultadoElegibilidadAlutel.NoElegible(
                    MotivoNoElegibleAlutel.VigenciaVencida,
                    "La vigencia del registro ya venció.");

            if (registro.Capacitado == null || string.IsNullOrWhiteSpace(registro.Capacitado.Documento))
                return ResultadoElegibilidadAlutel.NoElegible(
                    MotivoNoElegibleAlutel.DocumentoNoDisponible,
                    "El capacitado no tiene documento registrado.");

            return ResultadoElegibilidadAlutel.Elegible();
        }

        /// <summary>
        /// Selecciona el registro que debe informarse cuando existen varias capacitaciones
        /// del mismo curso para la misma persona: gana la de mayor vigencia y, ante empate,
        /// la jornada más reciente y luego el identificador mayor.
        /// </summary>
        public RegistroCapacitacion SeleccionarRegistro(IEnumerable<RegistroCapacitacion> registros)
        {
            if (registros == null)
                return null;

            return registros
                .Where(r => Evaluar(r).EsElegible)
                .OrderByDescending(r => r.FechaVencimiento.Value)
                .ThenByDescending(r => r.Jornada != null ? r.Jornada.Fecha : DateTime.MinValue)
                .ThenByDescending(r => r.RegistroCapacitacionID)
                .FirstOrDefault();
        }

        private static TipoVigenciaAlutel? ObtenerTipoVigencia(RegistroCapacitacion registro)
        {
            if (registro.Jornada == null || registro.Jornada.Curso == null)
                return null;

            return registro.Jornada.Curso.TipoVigenciaAlutel;
        }
    }
}
