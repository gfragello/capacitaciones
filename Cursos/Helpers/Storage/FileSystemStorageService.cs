using System.IO;
using System.Web;

namespace Cursos.Helpers.Storage
{
    public class FileSystemStorageService : IFileStorageService
    {
        private const string PathFotosCapacitados = "~/Images/FotosCapacitados/";
        private readonly static FileSystemStorageService _instance = new FileSystemStorageService();

        public static FileSystemStorageService GetInstance()
        {
            return _instance;
        }

        public void Save(string subDirectory, string fileName, HttpPostedFileBase file)
        {
            string pathDirectorio = this.ResolvePhysicalPath(subDirectory);

            if (!Directory.Exists(pathDirectorio))
                Directory.CreateDirectory(pathDirectorio);

            file.SaveAs(this.ResolvePhysicalPath(subDirectory, fileName));
        }

        public Stream OpenRead(string subDirectory, string fileName)
        {
            return File.Open(this.ResolvePhysicalPath(subDirectory, fileName), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public string ResolvePhysicalPath(string subDirectory, string fileName = null)
        {
            string pathBase = HttpContext.Current.Server.MapPath(PathFotosCapacitados);

            if (string.IsNullOrWhiteSpace(subDirectory))
                return pathBase;

            string pathDirectorio = Path.Combine(pathBase, subDirectory);

            if (string.IsNullOrWhiteSpace(fileName))
                return pathDirectorio;

            return Path.Combine(pathDirectorio, fileName);
        }

        public string ResolveUrl(string subDirectory, string fileName)
        {
            string relativePath = string.IsNullOrWhiteSpace(subDirectory)
                ? string.Format("{0}/{1}", PathFotosCapacitados.TrimEnd('/'), fileName)
                : string.Format("{0}/{1}/{2}", PathFotosCapacitados.TrimEnd('/'), subDirectory.Trim('/'), fileName);

            return VirtualPathUtility.ToAbsolute(relativePath);
        }

        public bool Exists(string subDirectory, string fileName)
        {
            return File.Exists(this.ResolvePhysicalPath(subDirectory, fileName));
        }

        public void Delete(string subDirectory, string fileName)
        {
            File.Delete(this.ResolvePhysicalPath(subDirectory, fileName));
        }

        public void Move(string sourceSubDirectory, string sourceFileName, string destinationSubDirectory, string destinationFileName)
        {
            string pathDestinoDirectorio = this.ResolvePhysicalPath(destinationSubDirectory);
            if (!Directory.Exists(pathDestinoDirectorio))
                Directory.CreateDirectory(pathDestinoDirectorio);

            File.Move(this.ResolvePhysicalPath(sourceSubDirectory, sourceFileName),
                      this.ResolvePhysicalPath(destinationSubDirectory, destinationFileName));
        }
    }
}
