using System.IO;
using System.Drawing;
using Cursos.Models;
using OfficeOpenXml;
using System.Collections.Generic;

namespace Cursos.Helpers
{
    public class ExportarExcelHelper
    {
        private static readonly ExportarExcelHelper _instance = new ExportarExcelHelper();

        private ExportarExcelHelper() { }

        public static ExportarExcelHelper GetInstance()
        {
            return _instance;
        }

        public MemoryStream GenerarJornadaExcelStream(Jornada jornada)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Jornada");

                ws.Cells[1, 1].Value = "Curso";
                ws.Cells[1, 2].Value = jornada.Curso.Descripcion;

                ws.Cells[2, 1].Value = "Instructor";
                ws.Cells[2, 2].Value = jornada.Instructor.NombreCompleto;

                ws.Cells[3, 1].Value = "Lugar";
                ws.Cells[3, 2].Value = jornada.Lugar.NombreLugar;

                ws.Cells[4, 1].Value = "Fecha";
                ws.Cells[4, 2].Value = jornada.Fecha.ToShortDateString();

                ws.Cells[5, 1].Value = "Hora";
                ws.Cells[5, 2].Value = jornada.Hora;

                ws.Cells[1, 1, 5, 1].Style.Font.Bold = true;

                const int rowInicial = 7;
                int i = rowInicial + 1;

                ws.Cells[rowInicial, 1].Value = "Documento";
                ws.Cells[rowInicial, 2].Value = "Nombre";
                ws.Cells[rowInicial, 3].Value = "Empresa";
                ws.Cells[rowInicial, 4].Value = "Datos Adicionales";
                ws.Cells[rowInicial, 5].Value = "Nota";

                ws.Cells[rowInicial, 1, rowInicial, 5].Style.Font.Bold = true;

                var bgColor = Color.White;

                foreach (var registro in jornada.RegistrosCapacitacion)
                {
                    ws.Cells[i, 1].Value = registro.Capacitado.DocumentoCompleto;
                    ws.Cells[i, 2].Value = registro.Capacitado.NombreCompleto;
                    ws.Cells[i, 3].Value = registro.Capacitado.Empresa.NombreFantasia;
                    ws.Cells[i, 4].Value = registro.DocumentacionAdicionalDatos;
                    ws.Cells[i, 5].Value = registro.Nota;

                    if (bgColor != Color.White)
                    {
                        ws.Cells[i, 1, i, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, 1, i, 5].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    ws.Cells[i, 1, i, 5].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;
                    i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }
        }
    }
}