using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;
using PagedList;
using OfficeOpenXml;
using System.IO;
using System.Drawing;
using Cursos.Helpers;
using Cursos.Models.Enums;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador,AdministradorExterno,ConsultaEmpresa,ConsultaGeneral,InscripcionesExternas,InstructorExterno")]
    public class CapacitadosController : Controller
    {
        private CursosDbContext db = new CursosDbContext();

        // GET: Capacitados
        public ActionResult Index(string documento, 
                                  string currentNombre, string nombre,
                                  string currentApellido, string apellido, 
                                  int? currentEmpresaID, int? EmpresaID, 
                                  int? currentCursoID, int? CursoID, 
                                  int? page, bool? exportarExcel)
        {
            //no se permite el acceso al Index de capacitados a los usuarios que solamente tienen el rol InscripcionesExternas
            if (User.IsInRole("InscripcionesExternas") && !User.IsInRole("ConsultaEmpresa"))
                return RedirectToAction("Disponibles", "Jornadas");

            if (nombre != null) //si el parámetro vino con algún valor es porque se presionó buscar y se resetea la página a 1
                page = 1;
            else
                nombre = currentNombre;

            ViewBag.CurrentNombre = nombre;

            if (apellido != null)
                page = 1;
            else
                apellido = currentApellido;

            ViewBag.CurrentApellido = apellido;

            if (EmpresaID != null)
                page = 1;
            else
            { 
                if (currentEmpresaID == null)
                    currentEmpresaID = -1;

                EmpresaID = currentEmpresaID;
            }

            ViewBag.CurrentEmpresaID = EmpresaID;

            if (CursoID != null)
                page = 1;
            else
            {
                if (currentCursoID == null)
                    currentCursoID = -1;

                CursoID = currentCursoID;
            }

            ViewBag.CurrentCursoID = CursoID;

            List<Empresa> empresasDD;

            if (User.IsInRole("ConsultaEmpresa")) //para este rol solo se muestran los Capacitados de esa empresa
            {
                var empresaUsuario = db.EmpresasUsuarios.Where(eu => eu.Usuario == User.Identity.Name).FirstOrDefault();

                empresasDD = db.Empresas.Where(e => e.EmpresaID == empresaUsuario.EmpresaID).ToList();
                EmpresaID = empresaUsuario.EmpresaID;

                ViewBag.EmpresaID = new SelectList(empresasDD, "EmpresaID", "NombreFantasia", EmpresaID);
            }
            else
            {
                empresasDD = db.Empresas.OrderBy(e => e.NombreFantasia).ToList();
                empresasDD.Insert(0, new Empresa { EmpresaID = -1, NombreFantasia = "Todas" });
                ViewBag.EmpresaID = new SelectList(empresasDD, "EmpresaID", "NombreFantasia", EmpresaID);
            }

            List<Curso> cursosDD = db.Cursos.OrderBy(c => c.Descripcion).ToList();
            cursosDD.Insert(0, new Curso { CursoID = -1, Descripcion = "Todos" });
            ViewBag.CursoID = new SelectList(cursosDD, "CursoID", "Descripcion", CursoID);

            var capacitados = db.Capacitados.Include(c => c.Empresa).Include(c => c.RegistrosCapacitacion);

            if (!String.IsNullOrEmpty(documento))
                capacitados = capacitados.Where(c => c.Documento.Contains(documento));

            if (!String.IsNullOrEmpty(nombre))
                capacitados = capacitados.Where(c => c.Nombre.Contains(nombre));

            if (!String.IsNullOrEmpty(apellido))
                capacitados = capacitados.Where(c => c.Apellido.Contains(apellido));

            if (EmpresaID != -1)
                capacitados = capacitados.Where(c => c.EmpresaID == (int)EmpresaID);

            if (CursoID != -1)
                capacitados = capacitados.Where(c => c.RegistrosCapacitacion.Any(r => r.Jornada.CursoId == (int)CursoID));

            capacitados = capacitados.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre);

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            ViewBag.Cursos = db.Cursos.OrderBy(c => c.Descripcion).ToList();

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
                return View(capacitados.ToPagedList(pageNumber, pageSize));
            else
                return ExportDataExcel(capacitados.ToList(), CursoID);
        }

        [Authorize(Roles = "Administrador,ConsultaEmpresa")]
        public ActionResult ConsultaDocumento(string documento)
        {
            if (!string.IsNullOrEmpty(documento))
            {
                int pageSize = 100;
                int pageNumber = 1;

                var capacitados = db.Capacitados.Where(c => c.Documento == documento).Include(c => c.RegistrosCapacitacion);
                ViewBag.Capacitados = capacitados.OrderBy(c => c.Apellido).ToPagedList(pageNumber, pageSize);
            }
            else
            {
                ViewBag.Capacitados = new List<Capacitado>().ToPagedList(1,1);
            }

            ViewBag.Cursos = db.Cursos.OrderBy(c => c.Descripcion).ToList();

            return View();
        }

        // GET: Capacitados/Details/5
        [Authorize(Roles = "Administrador,AdministradorExterno,ConsultaEmpresa,ConsultaGeneral")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var capacitados = db.Capacitados.Include(c => c.RegistrosCapacitacion);
            var capacitado = capacitados.Where(c => c.CapacitadoID == (int)id).First();

            if (capacitado == null)
            {
                return HttpNotFound();
            }
            return View(capacitado);
        }

        // GET: Capacitados/Create 
        //Si se especifica un valor en el parametro documentoTemplate, se muestra el valor pre cargado en la pantalla
        //Si se especifica un valor de jornadaId, luego de crear el usuario se lo agrega automáticamente a la jornada
        [Authorize(Roles = "Administrador,AdministradorExterno,InscripcionesExternas")]
        public ActionResult Create(int? jornadaId)
        {
            ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia");
            ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion");

            ViewBag.JornadaId = jornadaId;

            Jornada j = null;

            if (jornadaId != null)
                j = db.Jornada.Find(jornadaId);

            if (j != null)
                ViewBag.JornadaIdentificacionCompleta = j.JornadaIdentificacionCompleta;
            else
                ViewBag.JornadaIdentificacionCompleta = string.Empty;

            return View();
        }

        // POST: Capacitados/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CapacitadoID,Nombre,Apellido,Documento,Fecha,EmpresaID,TipoDocumentoID")] Capacitado capacitado, HttpPostedFileBase upload, int? jornadaId)
        {
            if (ModelState.IsValid)
            {
                capacitado.SetearAtributosControl();

                db.Capacitados.Add(capacitado);
                db.SaveChanges();

                if (upload != null && upload.ContentLength > 0)
                {
                    string nombreArchivo = PathArchivoHelper.GetInstance().ObtenerNombreFotoCapacitado(capacitado.CapacitadoID,
                                                                                                       System.IO.Path.GetExtension(upload.FileName));

                    string carpetaArchivo = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(capacitado.CapacitadoID);

                    string pathDirectorio = Path.Combine(Server.MapPath("~/Images/FotosCapacitados/"), carpetaArchivo);

                    capacitado.PathArchivo = PathArchivoHelper.GetInstance().ObtenerPathArchivo(nombreArchivo,
                                                                                                carpetaArchivo,
                                                                                                pathDirectorio,
                                                                                                upload,
                                                                                                TiposArchivo.FotoCapacitado);

                    db.Entry(capacitado).State = EntityState.Modified;
                    db.SaveChanges();
                }

                //si durante la cración se recibe un id de jornada, el capacitado es agregado a esa jornada
                if (jornadaId != null)
                {
                    Jornada j = db.Jornada.Find(jornadaId);

                    if (j == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                    //se vuelve a cargar el capacitado para leer entidades asociadas
                    capacitado = db.Capacitados.Where(c => c.CapacitadoID == capacitado.CapacitadoID).Include(c => c.TipoDocumento).FirstOrDefault();

                    var nuevoRC = new RegistroCapacitacion();
                    nuevoRC.SetearAtributosControl();

                    nuevoRC.Jornada = j;
                    nuevoRC.Capacitado = capacitado;
                    nuevoRC.Nota = 0;
                    nuevoRC.Aprobado = true;
                    nuevoRC.FechaVencimiento = j.ObtenerFechaVencimiento(true);

                    if (j.PermiteEnviosOVAL)
                        nuevoRC.EnvioOVALEstado = EstadosEnvioOVAL.PendienteEnvio;
                    else
                        nuevoRC.EnvioOVALEstado = EstadosEnvioOVAL.NoEnviar;

                    db.RegistroCapacitacion.Add(nuevoRC);
                    db.SaveChanges();

                    //si la incripción fue registrada por un usuario con perfil para inscripciones externas, se notifica por email
                    if (System.Web.HttpContext.Current.User.IsInRole("InscripcionesExternas"))
                        NotificacionesEMailHelper.GetInstance().EnviarEmailsNotificacionInscripcionExterna(nuevoRC, true);

                    return RedirectToAction("Details", "Jornadas", new { id = jornadaId });
                }

                return RedirectToAction("Details", "Capacitados", new { id = capacitado.CapacitadoID });
            }

            ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion", capacitado.TipoDocumentoID);
            ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia", capacitado.EmpresaID);

            return View(capacitado);
        }

        // GET: Capacitados/Edit/5
        [Authorize(Roles = "Administrador,AdministradorExterno")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Capacitado capacitado = db.Capacitados.Find(id);
            if (capacitado == null)
            {
                return HttpNotFound();
            }

            if (capacitado.PuedeModificarse())
            {
                ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia", capacitado.EmpresaID);
                ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion", capacitado.TipoDocumentoID);
                return View(capacitado);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
        }

        // POST: Capacitados/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CapacitadoID,Nombre,Apellido,Documento,Fecha,EmpresaID,TipoDocumentoID,PathArchivoID")] Capacitado capacitado, HttpPostedFileBase upload)
        {
            if (ModelState.IsValid)
            {
                PathArchivo pathArchivo = null;

                if (upload != null && upload.ContentLength > 0)
                {
                    string nombreArchivo = PathArchivoHelper.GetInstance().ObtenerNombreFotoCapacitado(capacitado.CapacitadoID,
                                                                                                       System.IO.Path.GetExtension(upload.FileName));
                        
                    string carpetaArchivo = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(capacitado.CapacitadoID);

                    string pathDirectorio = Path.Combine(Server.MapPath("~/Images/FotosCapacitados/"), carpetaArchivo);

                    pathArchivo = PathArchivoHelper.GetInstance().ObtenerPathArchivo(nombreArchivo,
                                                                                     carpetaArchivo,
                                                                                     pathDirectorio,
                                                                                     upload,
                                                                                     TiposArchivo.FotoCapacitado);

                    db.Entry(pathArchivo).State = EntityState.Added;

                    capacitado.PathArchivo = pathArchivo;
                    //db.SaveChanges();
                }

                capacitado.SetearAtributosControl();

                /*
                if (pathArchivo != null)
                    capacitado.PathArchivoID = pathArchivo.PathArchivoId;
                */

                db.Entry(capacitado).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = capacitado.CapacitadoID });
            }
            ViewBag.EmpresaID = new SelectList(db.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia", capacitado.EmpresaID);
            ViewBag.TipoDocumentoID = new SelectList(db.TiposDocumento.ToList(), "TipoDocumentoID", "Descripcion", capacitado.TipoDocumentoID);
            return View(capacitado);
        }

        // GET: Capacitados/Delete/5
        [Authorize(Roles = "Administrador,AdministradorExterno")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Capacitado capacitado = db.Capacitados.Find(id);
            if (capacitado == null)
            {
                return HttpNotFound();
            }
            
            if (capacitado.PuedeModificarse())   
                return View(capacitado);
            else
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
        }

        // POST: Capacitados/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Capacitado capacitado = db.Capacitados.Find(id);
            db.Capacitados.Remove(capacitado);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Capacitados/CargarFoto/5
        public ActionResult CargarFoto(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var capacitado = db.Capacitados.Where(c => c.CapacitadoID == (int)id).First();

            if (capacitado == null)
            {
                return HttpNotFound();
            }
            return View(capacitado);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarFoto(int? capacitadoId)
        {
            if (capacitadoId == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            var capacitado = db.Capacitados.Find(capacitadoId);

            if (capacitado == null)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            var pathFotoCapacitado = capacitado.PathArchivo;
            capacitado.PathArchivo = null;

            string pathCompleto = Request.MapPath("~/Images/FotosCapacitados/" + pathFotoCapacitado.SubDirectorio + "/" + pathFotoCapacitado.NombreArchivo);
            if (System.IO.File.Exists(pathCompleto))
            {
                System.IO.File.Delete(pathCompleto);
            }

            db.PathArchivos.Remove(pathFotoCapacitado);

            db.SaveChanges();

            return RedirectToAction("Edit", new { id = capacitadoId });
        }

        private ActionResult ExportDataExcel(List<Capacitado> capacitados, int? CursoID)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Capacitados");

                const int rowEncabezadoVencimientos = 1;
                const int rowInicial = 2;
                int i = rowInicial + 1;
                
                ws.Cells[rowInicial, 1].Value = "Apellido";
                ws.Cells[rowInicial, 2].Value = "Nombre";
                ws.Cells[rowInicial, 3].Value = "Documento";
                ws.Cells[rowInicial, 4].Value = "Empresa";

                var colIinicioCursos = 5;
                var j = colIinicioCursos;

                List<Curso> cursos;

                if (CursoID == -1)
                    cursos = db.Cursos.OrderBy(cu => cu.Descripcion).ToList();
                else
                    cursos = db.Cursos.Where(cu => cu.CursoID == CursoID).ToList();

                foreach (var curso in cursos)
                {
                    ws.Cells[rowInicial, j].Value = curso.Descripcion.Substring(0, 2);

                    ws.Cells[rowInicial, j].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    ws.Cells[rowInicial, j].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[rowInicial, j].Style.Fill.BackgroundColor.SetColor(Color.FromName(curso.ColorDeFondo));

                    j++;
                }

                //sobre los cabezales de los cursos se pone el encabezado "Vencimientos"
                ws.Cells[rowEncabezadoVencimientos, colIinicioCursos].Value = "Vencimientos";
                ws.Cells[rowEncabezadoVencimientos, colIinicioCursos, rowEncabezadoVencimientos, j - 1].Merge = true;
                ws.Cells[rowEncabezadoVencimientos, colIinicioCursos, rowEncabezadoVencimientos, j - 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws.Cells[rowEncabezadoVencimientos, colIinicioCursos, rowEncabezadoVencimientos, j - 1].Style.Font.Bold = true;

                //se ponen en negrita los encabezados
                ws.Cells[rowInicial, 1, rowInicial, j-1].Style.Font.Bold = true;

                var bgColor = Color.White;

                foreach (var c in capacitados)
                {
                    ws.Cells[i, 1].Value = c.Apellido;
                    ws.Cells[i, 2].Value = c.Nombre;
                    ws.Cells[i, 3].Value = c.DocumentoCompleto;
                    ws.Cells[i, 4].Value = c.Empresa.NombreFantasia;

                    j = colIinicioCursos;

                    foreach (var curso in cursos)
                    {
                        if (c.UltimoRegistroCapacitacionPorCurso(curso.CursoID, true).Count > 0)
                        {
                            var r = c.UltimoRegistroCapacitacionPorCurso(curso.CursoID, true)[0];

                            ws.Cells[i, j].Value = r.FechaVencimiento.ToShortDateString();

                            if (r.FechaVencimiento < DateTime.Now) //si el regsitro ya está vencido
                            {
                                ws.Cells[i, j].Style.Font.Color.SetColor(Color.Red);
                                ws.Cells[i, j].Style.Font.Bold = true;
                            }
                        }
                        else
                        {
                            ws.Cells[i, j].Value = "-";
                        }

                        ws.Cells[i, j].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, j].Style.Fill.BackgroundColor.SetColor(Color.FromName(curso.ColorDeFondo));

                        j++;
                    }

                    //se seleccionan las columnas con datos del capacitado para setear el background color.
                    if (bgColor != Color.White)
                    {
                        ws.Cells[i - 1, 1, i - 1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i - 1, 1, i - 1, 4].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    //se pone un borde alrededor del renglón del encabezado
                    ws.Cells[i - 1, 1, i - 1, j-1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;

                    i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "capacitados.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        public ActionResult ObtenerCapacitadoIDPorDocumento(string documento)
        {
            return Json(BuscarCapacitadoIDPorDocumento(documento), JsonRequestBehavior.AllowGet);
        }

        public int BuscarCapacitadoIDPorDocumento(string documento)
        {
            var capacitado = db.Capacitados.Where(c => c.Documento == documento).FirstOrDefault();

            if (capacitado != null)
                return capacitado.CapacitadoID;
            else
                return -1;
        }

        public ActionResult CargarFotoCapacitado(Capacitado model, int capacitadoId, HttpPostedFileBase foto)
        {
            var capacitado = db.Capacitados.Where(c => c.CapacitadoID == capacitadoId).FirstOrDefault();

            if (capacitado != null)
            {
                if (capacitado.CargarFoto(foto))
                {
                    db.Entry(capacitado).State = EntityState.Modified;
                    db.SaveChanges();

                    return Json(true, JsonRequestBehavior.AllowGet);
                }

            }

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RotarFotoCapacitado(int capacitadoId, string direccion)
        {
            var capacitado = db.Capacitados.Where(c => c.CapacitadoID == capacitadoId).FirstOrDefault();

            if (capacitado != null)
                return Json(capacitado.RotarFoto(direccion), JsonRequestBehavior.AllowGet);

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        //el parámetro jornadaIdExcluir indica que los capacitados que participaron de esas jornadas no pueden ser seleccionados
        [HttpGet]
        public ActionResult ObtenerSelecionarCapacitados(string documento,
                                                         int jornadaIdExcluir)
        {
            var capacitados = db.Capacitados.Include(c => c.Empresa).Include(c => c.RegistrosCapacitacion);

            if (!String.IsNullOrEmpty(documento))
                capacitados = capacitados.Where(c => c.Documento.Contains(documento));

            ViewBag.JornadaIdExcluir = jornadaIdExcluir;

            return PartialView("_SeleccionarCapacitadosPartial", capacitados.ToList());
        }
    }
}
