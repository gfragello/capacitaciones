using System.IO;
using System.Web;

namespace Cursos.Helpers.Storage
{
    public interface IFileStorageService
    {
        void Save(string subDirectory, string fileName, HttpPostedFileBase file);
        Stream OpenRead(string subDirectory, string fileName);
        string ResolvePhysicalPath(string subDirectory, string fileName = null);
        string ResolveUrl(string subDirectory, string fileName);
        bool Exists(string subDirectory, string fileName);
        void Delete(string subDirectory, string fileName);
        void Move(string sourceSubDirectory, string sourceFileName, string destinationSubDirectory, string destinationFileName);
    }
}
