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
                case "dmmono":
                    if (isBold)
                    {
                        if (isItalic)
                            return new FontResolverInfo("DMMono#bi");
                        return new FontResolverInfo("DMMono#b");
                    }
                    if (isItalic)
                        return new FontResolverInfo("DMMono#i");
                    return new FontResolverInfo("DMMono#");

                //se pueden agregar nuevas fuentes
                //copia los archivos de fuentes dentro de fonts/<Nombre de fuente>
                //IMPORTANTE: la propiedad ""Build Action" de los archivos de fuente debe ser "Embedded Resource" ver ejemplo de DMMono

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

            //default font face
            return new FontResolverInfo("DMMono#");

            // We pass all other font requests to the default handler.
            // When running on a web server without sufficient permission, you can return a default font at this stage.
            //return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
        }

        /// <summary>
        /// Return the font data for the fonts.
        /// </summary>
        public /*override*/ byte[] GetFont(string faceName)
        {
            switch (faceName)
            {
                case "DMMono#":
                    return FontHelper.GetInstance().DMMonoRegular;

                case "DMMono#b":
                    return FontHelper.GetInstance().DMMonoBold;

                case "DMMono#i":
                    return FontHelper.GetInstance().DMMonoItalic;

                case "DMMono#bi":
                    return FontHelper.GetInstance().DMMonoBoldItalic;

                case "AnonymousPro#":
                    return FontHelper.GetInstance().AnonymousProRegular;

                case "AnonymousPro#b":
                    return FontHelper.GetInstance().AnonymousProBold;

                case "AnonymousPro#i":
                    return FontHelper.GetInstance().AnonymousProItalic;

                case "AnonymousPro#bi":
                    return FontHelper.GetInstance().AnonymousProBoldItalic;
            }

            //return base.GetFont(faceName);
            return null;
        }
    }
}