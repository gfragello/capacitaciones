using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Cursos.Models;
using PdfSharp.Drawing;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Charting;
using PdfSharp.Drawing.Layout;
using System.Web.UI;
using PdfSharp.Fonts;

namespace Cursos.Helpers
{
    public sealed class CertificadoHelper
    {
        private readonly static CertificadoHelper _instance = new CertificadoHelper();
        XGraphicsState _state;

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
                GlobalFontSettings.DefaultFontEncoding = PdfFontEncoding.WinAnsi;
            }
            
            using (PdfDocument pdfDocument = new PdfDocument())
            {
                PdfPage page = pdfDocument.AddPage();
                page.Size = PageSize.A4;

                XGraphics gfx = XGraphics.FromPdfPage(page);

                //XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.WinAnsi);

                IniciarContenedorConFondo(gfx);

                var fuenteTexto = new XFont("NotoSansMono", 36, XFontStyle.Bold);
                AgregarTextoCertificado("CERTIFICADO", gfx, page.Width, 50, 50, fuenteTexto, XParagraphAlignment.Center, false);

                fuenteTexto = new XFont("NotoSansMono", 38, XFontStyle.Bold);
                AgregarTextoCertificado(c.NombreCompleto, gfx, page.Width, 130, 60, fuenteTexto, XParagraphAlignment.Center, false);

                fuenteTexto = new XFont("NotoSansMono", 20, XFontStyle.Regular);
                AgregarTextoCertificado(c.DocumentoCompleto, gfx, page.Width, 190, 30, fuenteTexto, XParagraphAlignment.Center, false);

                fuenteTexto = new XFont("NotoSansMono", 18, XFontStyle.Regular, XPdfFontOptions.UnicodeDefault);
                AgregarTextoCertificado("Por haber realizado a satisfacción los cursos:", gfx, page.Width, 230, 20, fuenteTexto, XParagraphAlignment.Center, false);

                //primero se calcula el largo máximo de los nombres de curso que se va a mostrar
                int largoMaximo = 0;
                foreach (var r in c.ObtenerRegistrosCapacitacionVigentes())
                {
                    if (r.Jornada.Curso.Descripcion.Length > largoMaximo)
                        largoMaximo = r.Jornada.Curso.Descripcion.Length;
                }

                const int posicionYInicial = 270;
                int item = 0;
                int alturaItem = 20;

                //se agrega primero el cabezal de los cursos
                string texoCabezal =
                string.Format("   {0, -25} {1, -12} {2, -12}", "Curso", "Realizado", "Vencimiento");
                int posicionY = posicionYInicial + (alturaItem * item);
                fuenteTexto = new XFont("NotoSansMono", 16, XFontStyle.Bold); //se pone fuente negrita para el encabezado de los cursos realizados
                AgregarTextoCertificado(texoCabezal, gfx, page.Width, posicionY, alturaItem, fuenteTexto, XParagraphAlignment.Left, false);
                item++;

                fuenteTexto = new XFont("NotoSansMono", 16, XFontStyle.Regular);
                foreach (var r in c.ObtenerRegistrosCapacitacionVigentes())
                {
                    string textoRegistro =
                    string.Format("   {0, -25} {1, -12} {2, -12}", r.Jornada.Curso.Descripcion,
                                                      r.Jornada.Fecha.ToShortDateString(),
                                                      r.FechaVencimiento.ToShortDateString());

                    posicionY = posicionYInicial + (alturaItem * item);

                    AgregarTextoCertificado(textoRegistro, gfx, page.Width, posicionY, alturaItem, fuenteTexto, XParagraphAlignment.Left, false);
                    item++;
                }

                fuenteTexto = new XFont("NotoSansMono", 18, XFontStyle.Regular);
                posicionY += 40;
                AgregarTextoCertificado("Cumpliendo con los requisitos académicos y de", gfx, page.Width, posicionY, 20, fuenteTexto, XParagraphAlignment.Center, false);

                posicionY += 20;
                AgregarTextoCertificado("asistencia exigidos para su aprobación.", gfx, page.Width, posicionY, 20, fuenteTexto, XParagraphAlignment.Center, false);

                posicionY += 50;
                fuenteTexto = new XFont("NotoSansMono", 22, XFontStyle.Regular);
                AgregarTextoCertificado(DateTime.Today.ToString("dd/MM/yyyy"), gfx, page.Width, posicionY, 20, fuenteTexto, XParagraphAlignment.Center, false);

                posicionY += 60;
                AgregarFirma(gfx, posicionY);

                fuenteTexto = new XFont("NotoSansMono", 16, XFontStyle.Regular);

                posicionY += 60;
                AgregarTextoCertificado("Alejandro Lacruz", gfx, page.Width, posicionY, 20, fuenteTexto, XParagraphAlignment.Center, false);

                posicionY += 20;
                AgregarTextoCertificado("Coordinador General", gfx, page.Width, posicionY, 20, fuenteTexto, XParagraphAlignment.Center, false);

                CerrarContenedorConFondo(gfx);

                AgregarLogo(gfx);

                //AgregarMarcaDeAgua("NO VÁLIDO - Prueba", page, gfx);

                return pdfDocument;
            }
        }

        private void IniciarContenedorConFondo(XGraphics gfx)
        {
            XColor ShadowColor = XColors.Gainsboro;

            //ancho de los bordes que rodean el certificado
            const double borderWidth = 4.5;

            XPen borderGreen = new XPen(XColor.FromArgb(0, 163, 108), borderWidth);
            var rectGreen = new XRect(7, 7, 580, 820);

            XPen borderBlue = new XPen(XColor.FromArgb(0, 112, 192), borderWidth);
            var rectBlue = new XRect(12, 12, 570, 810);

            XPen borderSky = new XPen(XColor.FromArgb(156, 194, 229), borderWidth);
            var rectSky = new XRect(17, 17, 560, 800);

            const int dEllipse = 15;
            
            var rect2 = rectGreen;
            rect2.Offset(borderWidth, borderWidth);

            gfx.DrawRoundedRectangle(new XSolidBrush(ShadowColor), rect2, new XSize(dEllipse + 8, dEllipse + 8));

            gfx.DrawRoundedRectangle(borderGreen, new XSolidBrush(XColors.White), rectGreen, new XSize(dEllipse, dEllipse));
            gfx.DrawRoundedRectangle(borderBlue, new XSolidBrush(XColors.White), rectBlue, new XSize(dEllipse, dEllipse));
            gfx.DrawRoundedRectangle(borderSky, new XSolidBrush(XColors.White), rectSky, new XSize(dEllipse, dEllipse));

            rectGreen.Inflate(-5, -5);
            rectGreen.Inflate(-10, -5);
            rectGreen.Y += 20;
            rectGreen.Height -= 20;

            _state = gfx.Save();
            gfx.TranslateTransform(rectGreen.X, rectGreen.Y);
        }

        private void CerrarContenedorConFondo(XGraphics gfx)
        {
            gfx.Restore(_state);
        }

        private void AgregarTextoCertificado(string texto, XGraphics gfx, XUnit pageWidth, double posicionY, double altura, XFont fuente, XParagraphAlignment alignment, bool mostrarControl)
        {
            var tf = new XTextFormatter(gfx);          

            var rect = new XRect(-10, posicionY, pageWidth - 10, altura);
            if (mostrarControl) gfx.DrawRectangle(XBrushes.LightSkyBlue, rect); //rectángulo de control para ver la posición del texto

            tf.Alignment = alignment;
            tf.DrawString(texto, fuente, XBrushes.Black, rect, XStringFormats.TopLeft);
        }

        private void AgregarFirma(XGraphics gfx, double posicionY)
        {
            string pathArchivo = HttpContext.Current.Server.MapPath("~/images/certificados/firma-alejandro-lacruz.png");
            XImage image = XImage.FromFile(pathArchivo);

            gfx.DrawImage(image, 220, posicionY, 157, 79);
        }

        private void AgregarLogo(XGraphics gfx)
        {
            string pathArchivo = HttpContext.Current.Server.MapPath("~/images/logos/CSL_logo_main_h_300.png");
            XImage image = XImage.FromFile(pathArchivo);

            gfx.DrawImage(image, 230, 700);
        }

        private void AgregarMarcaDeAgua(string texto, PdfPage page, XGraphics gfx)
        {
            // Create the font for drawing the watermark.
            var font = new XFont("NotoSansMono", 80, XFontStyle.Bold);

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
    }
}