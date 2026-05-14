using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cursos.Models;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Cursos.Helpers
{
    public sealed class CertificadoHelper
    {
        private readonly static CertificadoHelper _instance = new CertificadoHelper();
        private const string FontFamilyName = "Arial";
        private const double PageMargin = 42;
        private const double BorderWidth = 3;
        private const double BorderRadius = 12;
        private const double LogoWidth = 140;
        private const double LogoHeight = 84;
        private const double SignatureWidth = 157;
        private const double SignatureHeight = 79;
        private const double SignatureHorizontalOffset = 8;
        private const double SignatureVerticalOffset = 12;
        private const double SignatureLineOverlap = 8;
        private const double TableHeaderHeight = 24;
        private const double TableCellPaddingX = 10;
        private const double TableCellPaddingY = 7;
        private const double MinimumFontSize = 9;

        public static CertificadoHelper GetInstance()
        {
            return _instance;
        }

        public PdfDocument GenerarCertificado(Capacitado c)
        {
            //se verifica que el custom font resolver no esté seteado previamente
            if (!(GlobalFontSettings.FontResolver is FontResolver))
            {
                GlobalFontSettings.FontResolver = new FontResolver();
            }

            var pdfDocument = new PdfDocument();
            PdfPage page = pdfDocument.AddPage();
            page.Size = PageSize.A4;
            var registrosVigentes = c.ObtenerRegistrosCapacitacionVigentes();

            using (XGraphics gfx = XGraphics.FromPdfPage(page))
            {
                var pageRect = new XRect(0, 0, page.Width.Point, page.Height.Point);
                var contentRect = DibujarMarcoInstitucional(gfx, pageRect);

                double posicionY = DibujarEncabezado(gfx, contentRect, c);
                posicionY = DibujarTablaCursos(gfx, contentRect, posicionY, registrosVigentes);
                posicionY = DibujarTextoAcreditacion(gfx, contentRect, posicionY);
                posicionY = DibujarFecha(gfx, contentRect, posicionY);
                DibujarFirma(gfx, contentRect, posicionY);
            }

            return pdfDocument;
        }

        private XRect DibujarMarcoInstitucional(XGraphics gfx, XRect pageRect)
        {
            var frameRect = new XRect(PageMargin - 14, PageMargin - 10, pageRect.Width - ((PageMargin - 14) * 2), pageRect.Height - ((PageMargin - 10) * 2));
            var shadowRect = frameRect;
            shadowRect.Offset(4, 4);

            gfx.DrawRoundedRectangle(new XSolidBrush(XColor.FromArgb(32, 0, 0, 0)), shadowRect, new XSize(BorderRadius, BorderRadius));
            gfx.DrawRoundedRectangle(new XPen(CertificadoStyles.VerdeInstitucional, BorderWidth), XBrushes.White, frameRect, new XSize(BorderRadius, BorderRadius));

            return new XRect(PageMargin, PageMargin, pageRect.Width - (PageMargin * 2), pageRect.Height - (PageMargin * 2));
        }

        private double DibujarEncabezado(XGraphics gfx, XRect contentRect, Capacitado capacitado)
        {
            double posicionY = contentRect.Top + 12;
            DibujarLogo(gfx, contentRect, posicionY);
            posicionY += LogoHeight + 28;

            var titulo = CrearTextoAjustado(gfx, "CERTIFICADO", contentRect.Width, 32, 32, XFontStyle.Bold, 1);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left, posicionY, contentRect.Width, titulo.Height), titulo, XBrushes.Black, XStringAlignment.Center, false);
            posicionY += titulo.Height + 22;

            var nombre = CrearTextoAjustado(gfx, capacitado.NombreCompleto, contentRect.Width - 20, 26, MinimumFontSize, XFontStyle.Bold, 2);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left + 10, posicionY, contentRect.Width - 20, nombre.Height), nombre, XBrushes.Black, XStringAlignment.Center, false);
            posicionY += nombre.Height + 12;

            var documento = CrearTextoAjustado(gfx, capacitado.DocumentoCompleto, contentRect.Width - 40, 14, 14, XFontStyle.Regular, 1);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left + 20, posicionY, contentRect.Width - 40, documento.Height), documento, new XSolidBrush(CertificadoStyles.TextoSecundario), XStringAlignment.Center, false);
            posicionY += documento.Height + 24;

            var introduccion = CrearTextoAjustado(gfx, "Se certifica que ha realizado a satisfacción los siguientes cursos:", contentRect.Width - 60, 14, 14, XFontStyle.Regular, 2);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left + 30, posicionY, contentRect.Width - 60, introduccion.Height), introduccion, XBrushes.Black, XStringAlignment.Center, false);
            posicionY += introduccion.Height + 16;

            gfx.DrawLine(new XPen(CertificadoStyles.AzulInstitucional, 1), contentRect.Left + 95, posicionY, contentRect.Right - 95, posicionY);
            return posicionY + 20;
        }

        private double DibujarTablaCursos(XGraphics gfx, XRect contentRect, double posicionY, IList<RegistroCapacitacion> registros)
        {
            double tablaX = contentRect.Left;
            double anchoTabla = contentRect.Width;
            double anchoVencimiento = 128;
            double anchoCurso = anchoTabla - anchoVencimiento;
            var bordeTabla = new XPen(CertificadoStyles.BordeTabla, 0.75);
            var pincelFilaAlternada = new XSolidBrush(CertificadoStyles.GrisSuave);
            var pincelEncabezado = new XSolidBrush(CertificadoStyles.VerdeInstitucional);

            var rectCursoEncabezado = new XRect(tablaX, posicionY, anchoCurso, TableHeaderHeight);
            var rectVencimientoEncabezado = new XRect(tablaX + anchoCurso, posicionY, anchoVencimiento, TableHeaderHeight);
            var rectCursoEncabezadoContenido = new XRect(rectCursoEncabezado.X + TableCellPaddingX, rectCursoEncabezado.Y, rectCursoEncabezado.Width - (TableCellPaddingX * 2), rectCursoEncabezado.Height);
            var rectVencimientoEncabezadoContenido = new XRect(rectVencimientoEncabezado.X + TableCellPaddingX, rectVencimientoEncabezado.Y, rectVencimientoEncabezado.Width - (TableCellPaddingX * 2), rectVencimientoEncabezado.Height);

            gfx.DrawRectangle(bordeTabla, pincelEncabezado, rectCursoEncabezado);
            gfx.DrawRectangle(bordeTabla, pincelEncabezado, rectVencimientoEncabezado);

            var fuenteEncabezado = CrearFuente(10, XFontStyle.Bold);
            DibujarTextoSimple(gfx, rectCursoEncabezadoContenido, "Curso", fuenteEncabezado, XBrushes.White, XStringAlignment.Near, true);
            DibujarTextoSimple(gfx, rectVencimientoEncabezadoContenido, "Vencimiento", fuenteEncabezado, XBrushes.White, XStringAlignment.Center, true);

            posicionY += TableHeaderHeight;

            for (int i = 0; i < registros.Count; i++)
            {
                var registro = registros[i];
                string vencimientoTexto = registro.FechaVencimiento.HasValue
                    ? registro.FechaVencimiento.Value.ToString("dd/MM/yyyy")
                    : "Sin vencimiento";

                var cursoLayout = CrearTextoAjustado(gfx, registro.Jornada.Curso.Descripcion, anchoCurso - (TableCellPaddingX * 2), 10, MinimumFontSize, XFontStyle.Regular, 2);
                var vencimientoLayout = CrearTextoAjustado(gfx, vencimientoTexto, anchoVencimiento - (TableCellPaddingX * 2), 10, 10, XFontStyle.Regular, 2);
                double altoContenido = Math.Max(cursoLayout.Height, vencimientoLayout.Height);
                double altoFila = Math.Max(28, altoContenido + (TableCellPaddingY * 2));

                var rectCurso = new XRect(tablaX, posicionY, anchoCurso, altoFila);
                var rectVencimiento = new XRect(tablaX + anchoCurso, posicionY, anchoVencimiento, altoFila);
                var rellenoFila = i % 2 == 0 ? XBrushes.White : pincelFilaAlternada;

                gfx.DrawRectangle(bordeTabla, rellenoFila, rectCurso);
                gfx.DrawRectangle(bordeTabla, rellenoFila, rectVencimiento);

                var rectCursoContenido = new XRect(rectCurso.X + TableCellPaddingX, rectCurso.Y + TableCellPaddingY, rectCurso.Width - (TableCellPaddingX * 2), rectCurso.Height - (TableCellPaddingY * 2));
                var rectVencimientoContenido = new XRect(rectVencimiento.X + TableCellPaddingX, rectVencimiento.Y + TableCellPaddingY, rectVencimiento.Width - (TableCellPaddingX * 2), rectVencimiento.Height - (TableCellPaddingY * 2));

                DibujarBloqueTexto(gfx, rectCursoContenido, cursoLayout, XBrushes.Black, XStringAlignment.Near, true);
                DibujarBloqueTexto(gfx, rectVencimientoContenido, vencimientoLayout, new XSolidBrush(CertificadoStyles.TextoSecundario), XStringAlignment.Center, true);

                posicionY += altoFila;
            }

            return posicionY + 24;
        }

        private double DibujarTextoAcreditacion(XGraphics gfx, XRect contentRect, double posicionY)
        {
            var acreditacion = CrearTextoAjustado(gfx, "Cumpliendo con los requisitos académicos y de asistencia exigidos para su aprobación.", contentRect.Width - 60, 11, 11, XFontStyle.Regular, 2);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left + 30, posicionY, contentRect.Width - 60, acreditacion.Height), acreditacion, XBrushes.Black, XStringAlignment.Center, false);
            return posicionY + acreditacion.Height + 28;
        }

        private double DibujarFecha(XGraphics gfx, XRect contentRect, double posicionY)
        {
            var fecha = CrearTextoAjustado(gfx, DateTime.Today.ToString("dd/MM/yyyy"), contentRect.Width, 12, 12, XFontStyle.Regular, 1);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left, posicionY, contentRect.Width, fecha.Height), fecha, XBrushes.Black, XStringAlignment.Center, false);
            return posicionY + fecha.Height + 26;
        }

        private void DibujarFirma(XGraphics gfx, XRect contentRect, double posicionY)
        {
            string pathArchivo = HttpContext.Current.Server.MapPath("~/images/certificados/firma-alejandro-lacruz.png");
            double posicionX = contentRect.Left + ((contentRect.Width - SignatureWidth) / 2) + SignatureHorizontalOffset;
            double firmaY = posicionY + SignatureVerticalOffset;
            double lineaY = posicionY + SignatureHeight - SignatureLineOverlap;

            using (XImage image = XImage.FromFile(pathArchivo))
            {
                gfx.DrawImage(image, posicionX, firmaY, SignatureWidth, SignatureHeight);
            }

            double textoY = lineaY;
            gfx.DrawLine(new XPen(CertificadoStyles.GrisMedio, 0.75), contentRect.Left + ((contentRect.Width - 180) / 2), lineaY, contentRect.Left + ((contentRect.Width + 180) / 2), lineaY);

            textoY += 8;
            var firmante = CrearTextoAjustado(gfx, "Alejandro Lacruz", contentRect.Width, 11, 11, XFontStyle.Regular, 1);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left, textoY, contentRect.Width, firmante.Height), firmante, XBrushes.Black, XStringAlignment.Center, false);
            textoY += firmante.Height + 4;

            var cargo = CrearTextoAjustado(gfx, "Coordinador General", contentRect.Width, 11, 11, XFontStyle.Regular, 1);
            DibujarBloqueTexto(gfx, new XRect(contentRect.Left, textoY, contentRect.Width, cargo.Height), cargo, new XSolidBrush(CertificadoStyles.TextoSecundario), XStringAlignment.Center, false);
        }

        private void DibujarLogo(XGraphics gfx, XRect contentRect, double posicionY)
        {
            string pathArchivo = HttpContext.Current.Server.MapPath("~/images/logos/CSL_logo_main_h.png");
            double posicionX = contentRect.Left + ((contentRect.Width - LogoWidth) / 2);

            using (XImage image = XImage.FromFile(pathArchivo))
            {
                gfx.DrawImage(image, posicionX, posicionY, LogoWidth, LogoHeight);
            }
        }

        private TextBlockLayout CrearTextoAjustado(XGraphics gfx, string texto, double anchoDisponible, double tamanoPreferido, double tamanoMinimo, XFontStyle estilo, int maximoLineas)
        {
            int lineasPermitidas = Math.Max(1, maximoLineas);

            for (double tamano = tamanoPreferido; tamano >= tamanoMinimo; tamano -= 1)
            {
                var fuente = CrearFuente(tamano, estilo);
                var lineas = AjustarTexto(gfx, texto, fuente, anchoDisponible);

                if (lineas.Count <= lineasPermitidas)
                    return new TextBlockLayout(fuente, lineas);
            }

            var fuenteFinal = CrearFuente(tamanoMinimo, estilo);
            var lineasFinales = AjustarTexto(gfx, texto, fuenteFinal, anchoDisponible);

            if (lineasFinales.Count > lineasPermitidas)
            {
                var lineasVisibles = lineasFinales.Take(lineasPermitidas - 1).ToList();
                string textoRestante = string.Join(" ", lineasFinales.Skip(lineasPermitidas - 1));
                lineasVisibles.Add(TruncarConPuntosSuspensivos(gfx, textoRestante, fuenteFinal, anchoDisponible));
                lineasFinales = lineasVisibles;
            }

            return new TextBlockLayout(fuenteFinal, lineasFinales);
        }

        private List<string> AjustarTexto(XGraphics gfx, string texto, XFont fuente, double anchoDisponible)
        {
            string textoNormalizado = (texto ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(textoNormalizado))
                return new List<string> { string.Empty };

            var lineas = new List<string>();
            string lineaActual = string.Empty;

            foreach (string palabra in textoNormalizado.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (MedirTexto(gfx, palabra, fuente) > anchoDisponible)
                {
                    if (!string.IsNullOrEmpty(lineaActual))
                    {
                        lineas.Add(lineaActual);
                        lineaActual = string.Empty;
                    }

                    var segmentos = DividirPalabra(gfx, palabra, fuente, anchoDisponible);
                    for (int i = 0; i < segmentos.Count - 1; i++)
                        lineas.Add(segmentos[i]);

                    lineaActual = segmentos.Last();
                    continue;
                }

                string candidata = string.IsNullOrEmpty(lineaActual) ? palabra : lineaActual + " " + palabra;
                if (MedirTexto(gfx, candidata, fuente) <= anchoDisponible)
                {
                    lineaActual = candidata;
                    continue;
                }

                lineas.Add(lineaActual);
                lineaActual = palabra;
            }

            if (!string.IsNullOrEmpty(lineaActual))
                lineas.Add(lineaActual);

            return lineas.Count == 0 ? new List<string> { string.Empty } : lineas;
        }

        private List<string> DividirPalabra(XGraphics gfx, string palabra, XFont fuente, double anchoDisponible)
        {
            var segmentos = new List<string>();
            string segmentoActual = string.Empty;

            foreach (char caracter in palabra)
            {
                string candidata = segmentoActual + caracter;
                if (!string.IsNullOrEmpty(segmentoActual) && MedirTexto(gfx, candidata, fuente) > anchoDisponible)
                {
                    segmentos.Add(segmentoActual);
                    segmentoActual = caracter.ToString();
                    continue;
                }

                segmentoActual = candidata;
            }

            if (!string.IsNullOrEmpty(segmentoActual))
                segmentos.Add(segmentoActual);

            return segmentos.Count == 0 ? new List<string> { string.Empty } : segmentos;
        }

        private string TruncarConPuntosSuspensivos(XGraphics gfx, string texto, XFont fuente, double anchoDisponible)
        {
            const string sufijo = "...";
            string textoNormalizado = string.Join(" ", (texto ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            if (string.IsNullOrEmpty(textoNormalizado) || MedirTexto(gfx, textoNormalizado, fuente) <= anchoDisponible)
                return textoNormalizado;

            if (MedirTexto(gfx, sufijo, fuente) > anchoDisponible)
                return sufijo;

            string textoRecortado = textoNormalizado;
            while (textoRecortado.Length > 0 && MedirTexto(gfx, textoRecortado + sufijo, fuente) > anchoDisponible)
                textoRecortado = textoRecortado.Substring(0, textoRecortado.Length - 1).TrimEnd();

            return string.IsNullOrEmpty(textoRecortado) ? sufijo : textoRecortado + sufijo;
        }

        private double MedirTexto(XGraphics gfx, string texto, XFont fuente)
        {
            return gfx.MeasureString(texto ?? string.Empty, fuente).Width;
        }

        private void DibujarBloqueTexto(XGraphics gfx, XRect rect, TextBlockLayout layout, XBrush brush, XStringAlignment alineacionHorizontal, bool centrarVerticalmente)
        {
            double posicionY = rect.Top;
            if (centrarVerticalmente)
                posicionY += Math.Max(0, (rect.Height - layout.Height) / 2);

            foreach (string linea in layout.Lines)
            {
                var rectLinea = new XRect(rect.Left, posicionY, rect.Width, layout.LineHeight);
                gfx.DrawString(linea, layout.Font, brush, rectLinea, CrearStringFormat(alineacionHorizontal));
                posicionY += layout.LineHeight;
            }
        }

        private void DibujarTextoSimple(XGraphics gfx, XRect rect, string texto, XFont fuente, XBrush brush, XStringAlignment alineacionHorizontal, bool centrarVerticalmente)
        {
            var layout = new TextBlockLayout(fuente, new[] { texto });
            DibujarBloqueTexto(gfx, rect, layout, brush, alineacionHorizontal, centrarVerticalmente);
        }

        private XFont CrearFuente(double tamano, XFontStyle estilo)
        {
            return new XFont(FontFamilyName, tamano, estilo, XPdfFontOptions.WinAnsiDefault);
        }

        private XStringFormat CrearStringFormat(XStringAlignment alineacionHorizontal)
        {
            return new XStringFormat
            {
                Alignment = alineacionHorizontal,
                LineAlignment = XLineAlignment.Near
            };
        }

        private void AgregarMarcaDeAgua(string texto, PdfPage page, XGraphics gfx)
        {
            // Create the font for drawing the watermark.
            var font = CrearFuente(80, XFontStyle.Bold);

            // Get the size (in points) of the text.
            var size = gfx.MeasureString(texto, font);

            // Define a rotation transformation at the center of the page.
            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
            gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
            gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

            // Create a string format.
            var format = new XStringFormat();
            format.Alignment = XStringAlignment.Near;
            format.LineAlignment = XLineAlignment.Near;

            // Create a dimmed red brush.
            XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));

            // Draw the string.
            gfx.DrawString(texto, font, brush,
                new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                format);
        }

        private static class CertificadoStyles
        {
            public static readonly XColor VerdeInstitucional = XColor.FromArgb(0, 163, 108);
            public static readonly XColor AzulInstitucional = XColor.FromArgb(0, 112, 192);
            public static readonly XColor GrisSuave = XColor.FromArgb(245, 247, 249);
            public static readonly XColor GrisMedio = XColor.FromArgb(170, 178, 188);
            public static readonly XColor TextoSecundario = XColor.FromArgb(55, 65, 81);
            public static readonly XColor BordeTabla = XColor.FromArgb(212, 220, 226);
        }

        private sealed class TextBlockLayout
        {
            public TextBlockLayout(XFont fuente, IEnumerable<string> lineas)
            {
                Font = fuente;
                Lines = lineas != null ? lineas.ToList() : new List<string> { string.Empty };
                if (Lines.Count == 0)
                    Lines.Add(string.Empty);

                LineHeight = fuente.Size * 1.3;
            }

            public XFont Font { get; private set; }
            public List<string> Lines { get; private set; }
            public double LineHeight { get; private set; }
            public double Height { get { return Lines.Count * LineHeight; } }
        }
    }
}