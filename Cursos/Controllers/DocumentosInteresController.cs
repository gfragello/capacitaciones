using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;

namespace Cursos.Controllers
{
    [Authorize]
    public class DocumentosInteresController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: DocumentosInteres
        public ActionResult Index()
        {
            var documentos = db.DocumentosInteres.OrderBy(o => o.Nombre).ToList();
            return View(documentos);
        }

        // GET: DocumentosInteres/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentoInteres documento = db.DocumentosInteres.Find(id);
            if (documento == null)
            {
                return HttpNotFound();
            }

            return View(documento);
        }

        // GET: DocumentosInteres/Create
        [Authorize(Roles = "Administrador")]
        public ActionResult Create()
        {
            var documento = new DocumentoInteres();
            return View(documento);
        }

        // POST: DocumentosInteres/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public ActionResult Create([Bind(Include = "DocumentoInteresID,Nombre,Descripcion,NombreArchivo,Activo")] DocumentoInteres documento)
        {
            if (ModelState.IsValid)
            {
                db.DocumentosInteres.Add(documento);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(documento);
        }

        // GET: DocumentosInteres/Edit/5
        [Authorize(Roles = "Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentoInteres documento = db.DocumentosInteres.Find(id);
            if (documento == null)
            {
                return HttpNotFound();
            }

            return View(documento);
        }

        // POST: DocumentosInteres/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public ActionResult Edit([Bind(Include = "DocumentoInteresID,Nombre,Descripcion,NombreArchivo,Activo")] DocumentoInteres documento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(documento).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(documento);
        }

        // GET: DocumentosInteres/Delete/5
        [Authorize(Roles = "Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentoInteres documento = db.DocumentosInteres.Find(id);
            if (documento == null)
            {
                return HttpNotFound();
            }

            return View(documento);
        }

        // POST: DocumentosInteres/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            DocumentoInteres documento = db.DocumentosInteres.Find(id);
            db.DocumentosInteres.Remove(documento);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: DocumentosInteres/Download/5
        public ActionResult Download(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            DocumentoInteres documento = db.DocumentosInteres.Find(id);
            if (documento == null)
            {
                return HttpNotFound();
            }

            // Verificar que el documento esté activo
            if (!documento.Activo)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, "El documento no está disponible");
            }

            // Construir la ruta del archivo
            string filePath = Server.MapPath("~/documents/" + documento.NombreArchivo);

            // Verificar que el archivo existe
            if (!System.IO.File.Exists(filePath))
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, "El archivo no fue encontrado");
            }

            try
            {
                // Obtener el tipo de contenido basado en la extensión del archivo
                string contentType = GetContentType(documento.NombreArchivo);

                // Retornar el archivo para descarga
                return File(filePath, contentType, documento.NombreArchivo);
            }
            catch (Exception ex)
            {
                // Log del error
                System.Diagnostics.Debug.WriteLine($"Error al descargar archivo {documento.NombreArchivo}: {ex.Message}");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Error al descargar el archivo");
            }
        }

        private string GetContentType(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".ppt":
                    return "application/vnd.ms-powerpoint";
                case ".pptx":
                    return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".txt":
                    return "text/plain";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".zip":
                    return "application/zip";
                case ".rar":
                    return "application/x-rar-compressed";
                default:
                    return "application/octet-stream";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
