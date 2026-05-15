using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Cursos.Helpers
{
    public class FontResolver : IFontResolver
    {
        public /*override*/ FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Ignore case of font names.
            var name = familyName.ToLower();

            // Deal with the fonts we know.
            switch (name)
            {
                case "liberationsans":
                case "liberation sans":
                    if (isBold) return new FontResolverInfo("LiberationSans#b");

                    return new FontResolverInfo("LiberationSans#");

                case "notosans":
                case "arial":
                case "helvetica":
                case "sans-serif":
                    if (isBold) return new FontResolverInfo("LiberationSans#b");

                    return new FontResolverInfo("LiberationSans#");

                case "notosansmono": //la fuente NotoSasMono no cuenta con representación en cursivas (Italic)
                    if (isBold) return new FontResolverInfo("NotoSansMono#b");

                    return new FontResolverInfo("NotoSansMono#");

                //se pueden agregar nuevas fuentes
                //copia los archivos de fuentes dentro de fonts/<Nombre de fuente>
                //IMPORTANTE: la propiedad "Build Action" de los archivos de fuente debe ser "Embedded Resource" ver ejemplo de DMMono

                //case "dmmono":
                //    if (isBold)
                //    {
                //        if (isItalic)
                //            return new FontResolverInfo("DMMono#bi");
                //        return new FontResolverInfo("DMMono#b");
                //    }
                //    if (isItalic)
                //        return new FontResolverInfo("DMMono#i");
                //    return new FontResolverInfo("DMMono#");

                //case "anonymouspro":
                //    if (isBold)
                //    {
                //        if (isItalic)
                //            return new FontResolverInfo("AnonymousPro#bi");
                //        return new FontResolverInfo("AnonymousPro#b");
                //    }
                //    if (isItalic)
                //        return new FontResolverInfo("AnonymousPro#i");
                //    return new FontResolverInfo("AnonymousPro#");

            }

            // Avoid PlatformFontResolver in Azure App Service: PDFsharp can hit GetFontData
            // and fail with STATUS_ACCESS_DENIED when reading system fonts.
            return new FontResolverInfo(isBold ? "LiberationSans#b" : "LiberationSans#");
        }

        /// <summary>
        /// Return the font data for the fonts.
        /// </summary>
        public /*override*/ byte[] GetFont(string faceName)
        {
            switch (faceName)
            {
                case "NotoSans#":
                    return FontHelper.GetInstance().NotoSansRegular;

                case "NotoSans#b":
                    return FontHelper.GetInstance().NotoSansBold;

                case "NotoSansMono#":
                    return FontHelper.GetInstance().NotoSansMonoRegular;

                case "NotoSansMono#b":
                    return FontHelper.GetInstance().NotoSansMonoBold;

                case "LiberationSans#":
                    return FontHelper.GetInstance().LiberationSansRegular;

                case "LiberationSans#b":
                    return FontHelper.GetInstance().LiberationSansBold;

                //case "DMMono#":
                //    return FontHelper.GetInstance().DMMonoRegular;

                //case "DMMono#b":
                //    return FontHelper.GetInstance().DMMonoBold;

                //case "DMMono#i":
                //    return FontHelper.GetInstance().DMMonoItalic;

                //case "DMMono#bi":
                //    return FontHelper.GetInstance().DMMonoBoldItalic;

                //case "AnonymousPro#":
                //    return FontHelper.GetInstance().AnonymousProRegular;

                //case "AnonymousPro#b":
                //    return FontHelper.GetInstance().AnonymousProBold;

                //case "AnonymousPro#i":
                //    return FontHelper.GetInstance().AnonymousProItalic;

                //case "AnonymousPro#bi":
                //    return FontHelper.GetInstance().AnonymousProBoldItalic;
            }

            //return base.GetFont(faceName);
            return null;
        }
    }
}
