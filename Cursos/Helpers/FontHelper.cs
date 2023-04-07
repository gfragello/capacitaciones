using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Cursos.Helpers
{
    public sealed class FontHelper
    {
        private readonly static FontHelper _instance = new FontHelper();

        public static FontHelper GetInstance()
        {
            return _instance;
        }

        // Tip: I used JetBrains dotPeek to find the names of the resources (just look how dots in folder names are encoded).
        // Make sure the fonts have compile type "Embedded Resource". Names are case-sensitive.
        public byte[] AnonymousProRegular
        {
            get { return LoadFontData("Cursos.fonts.AnonymousPro.AnonymousPro-Regular.ttf"); }
        }

        public byte[] AnonymousProBold
        {
            get { return LoadFontData("Cursos.fonts.AnonymousPro.AnonymousPro-Bold.ttf"); }
        }

        public byte[] AnonymousProItalic
        {
            get { return LoadFontData("Cursos.fonts.AnonymousPro.AnonymousPro-Italic.ttf"); }
        }

        public byte[] AnonymousProBoldItalic
        {
            get { return LoadFontData("Cursos.fonts.AnonymousPro.AnonymousPro-BoldItalic.ttf"); }
        }
 
        public byte[] DMMonoRegular
        {
            get { return LoadFontData("Cursos.fonts.DMMono.DMMono-Regular.ttf"); }
        }

        public byte[] DMMonoBold
        {
            get { return LoadFontData("Cursos.fonts.DMMono.DMMono-Medium.ttf"); }
        }

        public byte[] DMMonoItalic
        {
            get { return LoadFontData("Cursos.fonts.DMMono.DMMono-Italic.ttf"); }
        }

        public byte[] DMMonoBoldItalic
        {
            get { return LoadFontData("Cursos.fonts.DMMono.DMMono-MediumItalic.ttf"); }
        }

        /// <summary>
        /// Returns the specified font from an embedded resource.
        /// </summary>
        private byte[] LoadFontData(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    throw new ArgumentException("No resource with name " + name);

                int count = (int)stream.Length;
                byte[] data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }
    }
}