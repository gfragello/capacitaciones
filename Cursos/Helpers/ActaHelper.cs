using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Cursos.Models;
using PdfSharp.Drawing;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;

namespace Cursos.Helpers
{
    public sealed class ActaHelper
    {
        private readonly static ActaHelper _instance = new ActaHelper();

        public static ActaHelper GetInstance()
        {
            return _instance;
        }

        public PdfDocument GenerarActaPDF(Jornada jornada)
        {
            // Verificar que el custom font resolver no esté seteado previamente
            if (!(GlobalFontSettings.FontResolver is FontResolver))
            {
                GlobalFontSettings.FontResolver = new FontResolver();
                GlobalFontSettings.DefaultFontEncoding = PdfFontEncoding.WinAnsi;
            }

            var pdfDocument = new PdfDocument();
            
            // Constantes para el layout - Optimizadas para que todo quepa en una página
            const int capacitadosPorPagina = 20;
            const double margenSuperior = 20;
            const double margenIzquierdo = 25;
            const double margenDerecho = 25;
            const double alturaFila = 16;
            const double alturaEncabezado = 22;
            const double espacioEntreSeccion = 5;

            var capacitados = jornada.RegistrosCapacitacion.ToList();
            int totalPaginas = (int)Math.Ceiling((double)capacitados.Count / capacitadosPorPagina);
            
            // Si no hay capacitados, crear al menos una página
            if (totalPaginas == 0) totalPaginas = 1;

            for (int numeroPagina = 1; numeroPagina <= totalPaginas; numeroPagina++)
            {
                var page = pdfDocument.AddPage();
                page.Size = PageSize.A4;
                page.Orientation = PageOrientation.Landscape;

                var gfx = XGraphics.FromPdfPage(page);
                var pageWidth = page.Width - margenIzquierdo - margenDerecho;
                
                double posicionY = margenSuperior;

                // Dibujar encabezado
                posicionY = DibujarEncabezado(gfx, jornada, margenIzquierdo, posicionY, pageWidth, numeroPagina, totalPaginas);
                posicionY += espacioEntreSeccion;

                // Dibujar tabla de capacitados
                var capacitadosPagina = capacitados
                    .Skip((numeroPagina - 1) * capacitadosPorPagina)
                    .Take(capacitadosPorPagina)
                    .ToList();

                posicionY = DibujarTablaCapacitados(gfx, capacitadosPagina, margenIzquierdo, posicionY, pageWidth, alturaFila, alturaEncabezado, numeroPagina, capacitadosPorPagina);

                // Reservar espacio para la encuesta en todas las páginas
                posicionY += espacioEntreSeccion;
                
                // En la última página, agregar encuesta
                if (numeroPagina == totalPaginas)
                {
                    posicionY = DibujarEncuesta(gfx, margenIzquierdo, posicionY, pageWidth);
                }
                else
                {
                    // Reservar el mismo espacio que ocuparía la encuesta (título + opciones + márgenes)
                    const double espacioEncuesta = 50; // 25 (título) + 25 (opciones) aproximadamente
                    posicionY += espacioEncuesta;
                }

                // Agregar firmas en todas las páginas
                posicionY += espacioEntreSeccion;
                DibujarFirmas(gfx, jornada, margenIzquierdo, posicionY, pageWidth);

                gfx.Dispose();
            }

            return pdfDocument;
        }

        private double DibujarEncabezado(XGraphics gfx, Jornada jornada, double margenIzquierdo, double posicionY, double pageWidth, int numeroPagina, int totalPaginas)
        {
            var fuenteTitulo = new XFont("Arial", 14, XFontStyle.Bold);
            var fuenteTexto = new XFont("Arial", 12, XFontStyle.Regular);
            var fuenteTextoNegrita = new XFont("Arial", 12, XFontStyle.Bold);

            // Información del curso
            var rectCurso = new XRect(margenIzquierdo, posicionY, pageWidth * 0.6, 25);
            var colorFondo = ColorHelper.FromHexToXColor(jornada.Curso.ColorDeFondo);
            gfx.DrawRectangle(new XSolidBrush(colorFondo), rectCurso);
            gfx.DrawRectangle(XPens.Black, rectCurso);
            
            var tf = new XTextFormatter(gfx);
            tf.Alignment = XParagraphAlignment.Center;
            tf.DrawString($"Capacitación: {jornada.Curso.Descripcion}", fuenteTextoNegrita, XBrushes.Black, rectCurso, XStringFormats.TopLeft);

            // Página at top right without box
            var rectPaginaTop = new XRect(margenIzquierdo + pageWidth * 0.7, posicionY, pageWidth * 0.3, 25);
            tf.Alignment = XParagraphAlignment.Right;
            tf.DrawString($"Página {numeroPagina}/{totalPaginas}", fuenteTextoNegrita, XBrushes.Black, rectPaginaTop, XStringFormats.TopLeft);

            posicionY += 28;

            // Fecha y lugar in one box
            var rectFechaLugar = new XRect(margenIzquierdo, posicionY, pageWidth * 0.6, 25);
            gfx.DrawRectangle(XBrushes.White, rectFechaLugar);
            gfx.DrawRectangle(XPens.Black, rectFechaLugar);
            tf.Alignment = XParagraphAlignment.Left;
            tf.DrawString($"Fecha: {jornada.Fecha.ToString("dd/MM/yyyy")} {jornada.Hora} - Lugar: {jornada.Lugar.NombreLugar}", fuenteTextoNegrita, XBrushes.Black, rectFechaLugar, XStringFormats.TopLeft);

            // Información de puntajes Min y Max a la derecha
            var rectPuntajes = new XRect(margenIzquierdo + pageWidth * 0.7, posicionY, pageWidth * 0.3, 25);
            tf.Alignment = XParagraphAlignment.Right;
            tf.DrawString($"Min: {jornada.Curso.PuntajeMinimo}   Max: {jornada.Curso.PuntajeMaximo}", fuenteTextoNegrita, XBrushes.Black, rectPuntajes, XStringFormats.TopLeft);

            return posicionY + 25;
        }

        private double DibujarTablaCapacitados(XGraphics gfx, List<RegistroCapacitacion> capacitados, double margenIzquierdo, double posicionY, double pageWidth, double alturaFila, double alturaEncabezado, int numeroPagina, int capacitadosPorPagina)
        {
            var fuenteEncabezado = new XFont("Arial", 8, XFontStyle.Bold);
            var fuenteTexto = new XFont("Arial", 7, XFontStyle.Regular);
            var tf = new XTextFormatter(gfx);

            // Definir anchos de columnas (porcentajes del ancho total)
            var anchos = new double[]
            {
                pageWidth * 0.02,  // Ordinal
                pageWidth * 0.20,  // Nombre (fusionado con Apellido) - reducido para nueva columna
                pageWidth * 0.08,  // Documento Completo
                pageWidth * 0.06,  // Fecha Nac
                pageWidth * 0.38,  // Empresa
                pageWidth * 0.16,  // Firma
                pageWidth * 0.04,  // 1er Reintento
                pageWidth * 0.04,  // 2do Reintento
                pageWidth * 0.02   // Nuevo Ordinal
            };

            var encabezados = new string[] { "#", "Nombre", "Documento", "Fecha Nac", "Empresa", "Firma", "Nota", "#" };

            // Dibujar encabezados
            double posicionX = margenIzquierdo;
            for (int i = 0; i < encabezados.Length; i++)
            {
                double anchoActual = anchos[i];
                
                // Para el encabezado "Nota", abarcar ambas columnas de reintento
                if (i == 6) // Índice de "Nota"
                {
                    anchoActual = anchos[6] + anchos[7];
                }
                else if (i == 7) // Nuevo Ordinal
                {
                    anchoActual = anchos[8];
                }
                
                var rect = new XRect(posicionX, posicionY, anchoActual, alturaEncabezado);
                gfx.DrawRectangle(XBrushes.White, rect);
                gfx.DrawRectangle(XPens.Black, rect);
                
                tf.Alignment = XParagraphAlignment.Center;
                tf.DrawString(encabezados[i], fuenteEncabezado, XBrushes.Black, rect, XStringFormats.TopLeft);
                
                posicionX += anchoActual;
            }

            posicionY += alturaEncabezado;

            // Dibujar filas de capacitados
            int ordinal = (numeroPagina - 1) * capacitadosPorPagina + 1;
            
            for (int fila = 0; fila < capacitadosPorPagina; fila++)
            {
                posicionX = margenIzquierdo;
                var registro = fila < capacitados.Count ? capacitados[fila] : null;

                // Dibujar cada columna
                for (int col = 0; col < anchos.Length; col++)
                {
                    double anchoCol = anchos[col];
                    var rect = new XRect(posicionX, posicionY, anchoCol, alturaFila);
                    gfx.DrawRectangle(XBrushes.White, rect);
                    gfx.DrawRectangle(XPens.Black, rect);

                    if (registro != null)
                    {
                        const double paddingInterno = 2; // Margen interno para el texto
                        
                        if (col == 0) // Ordinal
                        {
                            tf.Alignment = XParagraphAlignment.Center;
                            tf.DrawString(ordinal.ToString(), fuenteTexto, XBrushes.Black, rect, XStringFormats.TopLeft);
                        }
                        else if (col == 1) // Nombre
                        {
                            var rectTexto = new XRect(posicionX + paddingInterno, posicionY, anchoCol - paddingInterno, alturaFila);
                            tf.Alignment = XParagraphAlignment.Left;
                            tf.DrawString(registro.Capacitado.ApellidoNombre ?? "", fuenteTexto, XBrushes.Black, rectTexto, XStringFormats.TopLeft);
                        }
                        else if (col == 2) // Documento
                        {
                            var rectTexto = new XRect(posicionX + paddingInterno, posicionY, anchoCol - paddingInterno, alturaFila);
                            tf.Alignment = XParagraphAlignment.Left;
                            tf.DrawString(registro.Capacitado.DocumentoCompleto ?? "", fuenteTexto, XBrushes.Black, rectTexto, XStringFormats.TopLeft);
                        }
                        else if (col == 3) // Fecha Nacimiento
                        {
                            var rectTexto = new XRect(posicionX + paddingInterno, posicionY, anchoCol - paddingInterno, alturaFila);
                            var fechaNac = registro.Capacitado.Fecha?.ToString("dd/MM/yyyy") ?? "";
                            tf.DrawString(fechaNac, fuenteTexto, XBrushes.Black, rectTexto, XStringFormats.TopLeft);
                        }
                        else if (col == 4) // Empresa
                        {
                            var rectTexto = new XRect(posicionX + paddingInterno, posicionY, anchoCol - paddingInterno, alturaFila);
                            tf.Alignment = XParagraphAlignment.Left;
                            tf.DrawString(registro.Capacitado.Empresa?.NombreCompleto ?? "", fuenteTexto, XBrushes.Black, rectTexto, XStringFormats.TopLeft);
                        }
                        else if (col == 5) // Firma
                        {
                            // Siempre vacía
                        }
                        else if (col == 6) // 1er Reintento
                        {
                            // Vacío
                        }
                        else if (col == 7) // 2do Reintento
                        {
                            // Vacío
                        }
                        else if (col == 8) // Nuevo Ordinal
                        {
                            tf.Alignment = XParagraphAlignment.Center;
                            tf.DrawString(ordinal.ToString(), fuenteTexto, XBrushes.Black, rect, XStringFormats.TopLeft);
                        }
                    }

                    posicionX += anchoCol;
                }

                posicionY += alturaFila;
                ordinal++;
            }

            return posicionY;
        }

        private double DibujarEncuesta(XGraphics gfx, double margenIzquierdo, double posicionY, double pageWidth)
        {
            var fuenteTitulo = new XFont("Arial", 12, XFontStyle.Bold);
            var fuenteTexto = new XFont("Arial", 10, XFontStyle.Regular);
            var tf = new XTextFormatter(gfx);

            // Título de la encuesta
            tf.Alignment = XParagraphAlignment.Left;
            tf.DrawString("ENCUESTA: Cómo consideraron el curso los asistentes", fuenteTitulo, XBrushes.Black, 
                new XRect(margenIzquierdo, posicionY, pageWidth, 20), XStringFormats.TopLeft);

            posicionY += 25;

            // Opciones de la encuesta
            var opciones = new string[] { "Malo", "Regular", "Bueno", "Muy Bueno", "Excelente", "NS/NC" };
            double anchoOpcion = pageWidth / opciones.Length;
            double posicionX = margenIzquierdo;

            for (int i = 0; i < opciones.Length; i++)
            {
                var rect = new XRect(posicionX, posicionY, anchoOpcion - 5, 20);
                gfx.DrawRectangle(XBrushes.White, rect);
                gfx.DrawRectangle(XPens.Black, rect);
                
                // Crear un rectángulo con margen interno para el texto
                var rectTexto = new XRect(posicionX + 5, posicionY, anchoOpcion - 10, 20);
                tf.Alignment = XParagraphAlignment.Left;
                tf.DrawString(opciones[i], fuenteTexto, XBrushes.Black, rectTexto, XStringFormats.TopLeft);
                
                posicionX += anchoOpcion;
            }

            return posicionY + 25;
        }

        private void DibujarFirmas(XGraphics gfx, Jornada jornada, double margenIzquierdo, double posicionY, double pageWidth)
        {
            var fuenteTexto = new XFont("Arial", 11, XFontStyle.Regular);
            var tf = new XTextFormatter(gfx);

            // Calcular posiciones para las firmas
            double anchoSeccion = pageWidth / 2;
            double alturaSeccion = 60;

            // Firma del instructor
            var rectInstructor = new XRect(margenIzquierdo, posicionY, anchoSeccion - 20, alturaSeccion);
            
            // Línea de firma del instructor
            gfx.DrawLine(XPens.Black, margenIzquierdo + 50, posicionY + 40, margenIzquierdo + anchoSeccion - 70, posicionY + 40);
            
            // Texto del instructor
            tf.Alignment = XParagraphAlignment.Center;
            tf.DrawString($"Instructor: {jornada.Instructor.NombreCompleto}", fuenteTexto, XBrushes.Black, 
                new XRect(margenIzquierdo, posicionY + 50, anchoSeccion - 20, 20), XStringFormats.TopLeft);

            // Firma del coordinador
            var rectCoordinador = new XRect(margenIzquierdo + anchoSeccion, posicionY, anchoSeccion - 20, alturaSeccion);
            
            // Línea de firma del coordinador
            gfx.DrawLine(XPens.Black, margenIzquierdo + anchoSeccion + 50, posicionY + 40, margenIzquierdo + pageWidth - 70, posicionY + 40);
            
            // Texto del coordinador
            tf.DrawString("Coordinador: Alejandro Lacruz", fuenteTexto, XBrushes.Black, 
                new XRect(margenIzquierdo + anchoSeccion, posicionY + 50, anchoSeccion - 20, 20), XStringFormats.TopLeft);
        }
    }

    // Helper class para convertir colores hexadecimales
    public static class ColorHelper
    {
        public static Color FromHex(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return Color.LightBlue; // Color por defecto

            // Primero intentar convertir nombres de colores conocidos
            var namedColor = ConvertNamedColorToColor(hexColor.ToLowerInvariant());
            if (namedColor.HasValue)
                return namedColor.Value;

            try
            {
                // Remover el # si está presente
                hexColor = hexColor.TrimStart('#');
                
                if (hexColor.Length == 6)
                {
                    return Color.FromArgb(
                        Convert.ToInt32(hexColor.Substring(0, 2), 16),
                        Convert.ToInt32(hexColor.Substring(2, 2), 16),
                        Convert.ToInt32(hexColor.Substring(4, 2), 16)
                    );
                }
                else if (hexColor.Length == 8) // Con alpha
                {
                    return Color.FromArgb(
                        Convert.ToInt32(hexColor.Substring(0, 2), 16),
                        Convert.ToInt32(hexColor.Substring(2, 2), 16),
                        Convert.ToInt32(hexColor.Substring(4, 2), 16),
                        Convert.ToInt32(hexColor.Substring(6, 2), 16)
                    );
                }
            }
            catch
            {
                // En caso de error, devolver color por defecto
            }

            return Color.LightBlue;
        }

        private static Color? ConvertNamedColorToColor(string colorName)
        {
            switch (colorName)
            {
                case "darkseagreen":
                    return Color.DarkSeaGreen;
                case "pink":
                    return Color.Pink;
                case "lightblue":
                    return Color.LightBlue;
                case "sandybrown":
                    return Color.SandyBrown;
                case "lemonchiffon":
                    return Color.LemonChiffon;
                case "darkgrey":
                case "darkgray":
                    return Color.DarkGray;
                case "burlywood":
                    return Color.BurlyWood;
                case "lightseagreen":
                    return Color.LightSeaGreen;
                default:
                    return null;
            }
        }

        public static XColor FromHexToXColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return XColors.LightBlue; // Color por defecto

            // Primero intentar convertir nombres de colores conocidos
            var namedColor = ConvertNamedColorToXColor(hexColor.ToLowerInvariant());
            if (namedColor.HasValue)
                return namedColor.Value;

            try
            {
                // Remover el # si está presente
                hexColor = hexColor.TrimStart('#');
                
                if (hexColor.Length == 6)
                {
                    return XColor.FromArgb(
                        Convert.ToInt32(hexColor.Substring(0, 2), 16),
                        Convert.ToInt32(hexColor.Substring(2, 2), 16),
                        Convert.ToInt32(hexColor.Substring(4, 2), 16)
                    );
                }
                else if (hexColor.Length == 8) // Con alpha
                {
                    return XColor.FromArgb(
                        Convert.ToInt32(hexColor.Substring(0, 2), 16),
                        Convert.ToInt32(hexColor.Substring(2, 2), 16),
                        Convert.ToInt32(hexColor.Substring(4, 2), 16),
                        Convert.ToInt32(hexColor.Substring(6, 2), 16)
                    );
                }
            }
            catch
            {
                // En caso de error, devolver color por defecto
            }

            return XColors.LightBlue;
        }

        private static XColor? ConvertNamedColorToXColor(string colorName)
        {
            switch (colorName)
            {
                case "darkseagreen":
                    return XColors.DarkSeaGreen;
                case "pink":
                    return XColors.Pink;
                case "lightblue":
                    return XColors.LightBlue;
                case "sandybrown":
                    return XColors.SandyBrown;
                case "lemonchiffon":
                    return XColors.LemonChiffon;
                case "darkgrey":
                case "darkgray":
                    return XColors.DarkGray;
                case "burlywood":
                    return XColors.BurlyWood;
                case "lightseagreen":
                    return XColors.LightSeaGreen;
                default:
                    return null;
            }
        }
    }
}
