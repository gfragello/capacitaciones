using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Cursos.Models;
using Cursos.Helpers;
using System.IO;
using System.Drawing;
using System.Data.Entity.Validation;
using System.Diagnostics;
using Cursos.Models.Enums;

namespace Cursos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CustomToolsController : Controller
    {
        private CursosDbContext db = new CursosDbContext();
        private ValidationHelper validationHelper = ValidationHelper.GetInstance();

        // GET: CustomTools
        public ActionResult Index()
        {
            return View();
        }

        #region Importar Datos a Excel

        public ActionResult ImportarDatosExcel()
        {
            return View("ImportarDatosExcel");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportarDatosExcel(FormCollection formCollection)
        {
            if (Request != null)
            {

                HttpPostedFileBase file = Request.Files["UploadedFile"];

                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {

                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;

                    byte[] fileBytes = new byte[file.ContentLength];

                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));

                    var capacitados = new List<Capacitado>();

                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        LeerDatosExcel(package);
                    }
                }
            }

            return View();
        }

        private void LeerDatosExcel(ExcelPackage ep)
        {
            var lugares = GetLugares(ep);
            var instructores = GetInstructores(ep);
            var empresas = GetEmpresas(ep);
            var cursos = GetCursos(ep);

            var capacitados = GetCapacitados(ep, empresas);
            var jornadas = GetJornadas(ep, lugares, cursos, instructores);
            var registrosCapacitacion = GetRegistrosCapacitacion(ep, jornadas, capacitados);

            string usuarioModificacion = "maria.andrada@andritz.com";

            SaveLugares(lugares);
            SaveInstructores(instructores, usuarioModificacion);
            SaveEmpresas(empresas, usuarioModificacion);
            SaveCursos(cursos);

            SaveCapacitados(capacitados, usuarioModificacion);
            SaveJornadas(jornadas, usuarioModificacion);
            SaveRegistrosCapacitacion(registrosCapacitacion, usuarioModificacion);

            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                //TODO: Mejorar el mensaje de error que se muestra cuando ocurre un error durante la migración de datos
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }

        }

        private ExcelWorksheet GetWorksheetByName(ExcelPackage ep, string worksheetName)
        {
            return (from ws in ep.Workbook.Worksheets
                    where ws.Name == worksheetName
                    select ws).First();
        }

        private Dictionary<int, Lugar> GetLugares(ExcelPackage ep)
        {
            var ws = GetWorksheetByName(ep, "Lugar");
            var lugares = new Dictionary<int, Lugar>();

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            for (int i = 2; i <= TotalRows; i++)
            {
                Lugar l = null;

                int key = int.Parse(ws.Cells[i, 1].Value.ToString());
                string abrevLugar = ws.Cells[i, 2].Value.ToString();

                l = db.Lugares.Where(lugar => lugar.AbrevLugar == abrevLugar).FirstOrDefault();

                if (l == null)
                {
                    l = new Lugar();

                    l.AbrevLugar = abrevLugar;
                    l.NombreLugar = ws.Cells[i, 3].Value.ToString();
                }
                else
                {
                    //se incluye este else para poner un breakpoint
                }

                lugares.Add(key, l);
            }

            return lugares;
        }

        private Dictionary<int, Instructor> GetInstructores(ExcelPackage ep)
        {
            var ws = GetWorksheetByName(ep, "Instructores");
            var instructores = new Dictionary<int, Instructor>();

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            for (int i = 2; i <= TotalRows; i++)
            {
                Instructor ins = null;

                int key = int.Parse(ws.Cells[i, 1].Value.ToString());

                string nombre = ws.Cells[i, 3].Value.ToString();
                string apellido = ws.Cells[i, 2].Value.ToString();

                ins = db.Instructores.Where(inst => inst.Nombre == nombre && inst.Apellido == apellido).FirstOrDefault();

                if (ins == null)
                {
                    ins = new Instructor();

                    ins.Apellido = apellido;
                    ins.Nombre = nombre;
                    ins.Documento = ws.Cells[i, 4].Value != null ? ws.Cells[i, 4].Value.ToString() : null;

                    var FechaNacimientoStr = ws.Cells[i, 5].Value == null ? String.Empty : ws.Cells[i, 5].Value.ToString();
                    ins.FechaNacimiento = FechaNacimientoStr == String.Empty ? DateTime.Parse("01/01/2001") : DateTime.Parse(FechaNacimientoStr);

                    ins.Domicilio = ws.Cells[i, 6].Value != null ? ws.Cells[i, 6].Value.ToString() : null;
                    ins.Telefono = ws.Cells[i, 7].Value != null ? ws.Cells[i, 7].Value.ToString() : null;
                    ins.Activo = bool.Parse(ws.Cells[i, 9].Value.ToString());
                }
                else
                {
                    //se incluye este else para poner un breakpoint
                }

                instructores.Add(key, ins);
            }

            return instructores;
        }

        private Dictionary<int, Empresa> GetEmpresas(ExcelPackage ep)
        {
            var ws = GetWorksheetByName(ep, "Empresas");
            var empresas = new Dictionary<int, Empresa>();

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            for (int i = 2; i <= TotalRows; i++)
            {
                Empresa e = null;

                int key = int.Parse(ws.Cells[i, 1].Value.ToString());

                string nombreFantasia = ws.Cells[i, 2].Value.ToString();
                string RUT = ws.Cells[i, 5].Value != null ? ws.Cells[i, 5].Value.ToString() : null;

                if (!String.IsNullOrEmpty(RUT))
                    e = db.Empresas.Where(empresa => empresa.RUT == RUT).FirstOrDefault();
                else
                    e = db.Empresas.Where(empresa => empresa.NombreFantasia == nombreFantasia).FirstOrDefault();

                if (e == null)
                {
                    e = new Empresa();

                    e.NombreFantasia = nombreFantasia;
                    e.Domicilio = ws.Cells[i, 3].Value != null ? ws.Cells[i, 3].Value.ToString() : null;
                    e.RazonSocial = ws.Cells[i, 4].Value != null ? ws.Cells[i, 4].Value.ToString() : null;
                    e.RUT = RUT;
                    e.DepartamentoID = 13;
                }
                else
                {
                    //se incluye este else para poner un breakpoint
                }

                empresas.Add(key, e);
            }

            return empresas;
        }

        private Dictionary<int, Curso> GetCursos(ExcelPackage ep)
        {
            var ws = GetWorksheetByName(ep, "Cursos");
            var cursos = new Dictionary<int, Curso>();

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            for (int i = 2; i <= TotalRows; i++)
            {
                Curso c = null;

                int key = int.Parse(ws.Cells[i, 1].Value.ToString());

                string descripcion = ws.Cells[i, 2].Value.ToString();

                c = db.Cursos.Where(curso => curso.Descripcion == descripcion).FirstOrDefault();

                if (c == null)
                {
                    c = new Curso();

                    c.Descripcion = descripcion;
                    c.Costo = ws.Cells[i, 3].Value == null ? 0 : int.Parse(ws.Cells[i, 3].Value.ToString());
                    c.Horas = int.Parse(ws.Cells[i, 4].Value.ToString());
                    c.Modulo = ws.Cells[i, 5].Value.ToString();
                }
                else
                {
                    //se incluye este else para poner un breakpoint
                }

                cursos.Add(key, c);
            }

            return cursos;
        }

        private Dictionary<int, Capacitado> GetCapacitados(ExcelPackage ep, Dictionary<int, Empresa> empresas)
        {
            var ws = GetWorksheetByName(ep, "CAPACITADOS");
            var capacitados = new Dictionary<int, Capacitado>();

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            TipoDocumento td_Default = db.TiposDocumento.Where(td => td.Abreviacion == "OTR").First();
            TipoDocumento td_CIUruguay = db.TiposDocumento.Where(td => td.Abreviacion == "CI").First();

            for (int i = 2; i <= TotalRows; i++)
            {
                Capacitado c = null;

                int key = int.Parse(ws.Cells[i, 1].Value.ToString());

                TipoDocumento tipoDocumento = td_Default;

                string documento = ws.Cells[i, 5].Value.ToString();
                string documentoSinGuion = ws.Cells[i, 5].Value.ToString(); //en el archivo Excel, el documento puede estar sin guión

                if (documento.Contains("-")) //si el documento tiene un guión, podría ser una CI uruguaya
                {
                    documentoSinGuion = validationHelper.RemoveGuionCI(documento);
                }

                if (validationHelper.ValidateCI(documentoSinGuion)) //si el documento es una CI válida
                {
                    documento = documentoSinGuion;
                    tipoDocumento = td_CIUruguay;
                }

                c = db.Capacitados.Where(cap => cap.Documento == documento).FirstOrDefault();

                if (c == null)
                {
                    c = new Capacitado();

                    c.Nombre = ws.Cells[i, 2].Value.ToString();
                    c.Apellido = ws.Cells[i, 3].Value.ToString();
                    c.TipoDocumento = tipoDocumento;
                    c.Documento = documento;
                    c.Fecha = null;
                }

                var keyEmpresa = int.Parse(ws.Cells[i, 6].Value.ToString());
                c.Empresa = empresas[keyEmpresa];

                capacitados.Add(key, c);
            }

            return capacitados;
        }

        private Dictionary<int, Jornada> GetJornadas(ExcelPackage ep, Dictionary<int, Lugar> lugares, Dictionary<int, Curso> cursos, Dictionary<int, Instructor> instructores)
        {
            var ws = GetWorksheetByName(ep, "JORNADAS");
            var jornadas = new Dictionary<int, Jornada>();

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            for (int i = 2; i <= TotalRows; i++)
            {
                var j = new Jornada();

                int key = int.Parse(ws.Cells[i, 1].Value.ToString());

                j.Fecha = DateTime.Parse(ws.Cells[i, 2].Value.ToString());

                var keyLugar = int.Parse(ws.Cells[i, 3].Value.ToString());
                j.Lugar = lugares[keyLugar];

                var keyCurso = int.Parse(ws.Cells[i, 4].Value.ToString());
                j.Curso = cursos[keyCurso];

                var keyInstructor = int.Parse(ws.Cells[i, 5].Value.ToString());
                j.Instructor = instructores[keyInstructor];

                jornadas.Add(key, j);
            }

            return jornadas;
        }

        private List<RegistroCapacitacion> GetRegistrosCapacitacion(ExcelPackage ep, Dictionary<int, Jornada> jornadas, Dictionary<int, Capacitado> capacitados)
        {
            var ws = GetWorksheetByName(ep, "REGISTROS");
            var registrosCapacitacion = new List<RegistroCapacitacion>();

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            for (int i = 2; i <= TotalRows; i++)
            {
                var r = new RegistroCapacitacion();

                var keyJornada = int.Parse(ws.Cells[i, 1].Value.ToString());

                if (jornadas.ContainsKey(keyJornada))
                    r.Jornada = jornadas[keyJornada];
                else
                    continue;

                var keyCapacitado = int.Parse(ws.Cells[i, 7].Value.ToString());

                if (capacitados.ContainsKey(keyCapacitado))
                    r.Capacitado = capacitados[keyCapacitado];
                else
                    continue;

                string aprobadoTetxo = ws.Cells[i, 8].Value != null ? ws.Cells[i, 8].Value.ToString() : String.Empty;

                r.Aprobado = (aprobadoTetxo == "Si" || aprobadoTetxo == "S") ? true : false;
                r.Nota = int.Parse(ws.Cells[i, 10].Value.ToString());

                if (ws.Cells[i, 9].Value != null)
                    r.NotaPrevia = int.Parse(ws.Cells[i, 9].Value.ToString());
                else
                    r.NotaPrevia = null;

                r.FechaVencimiento = new DateTime(r.Jornada.Fecha.Year + r.Jornada.Curso.Vigencia, r.Jornada.Fecha.Month, r.Jornada.Fecha.Day);

                registrosCapacitacion.Add(r);
            }

            return registrosCapacitacion;
        }

        private void SaveLugares(Dictionary<int, Lugar> lugares)
        {
            foreach (var l in lugares)
            {
                //solo se agregan a la base de datos los lugares que no existían previamente
                if (l.Value.LugarID == 0)
                    db.Lugares.Add(l.Value);
            }
        }

        private void SaveInstructores(Dictionary<int, Instructor> instructores, string usuarioModificacion)
        {
            foreach (var i in instructores)
            {
                if (i.Value.InstructorID == 0)
                {
                    i.Value.ForzarUsuario(usuarioModificacion);
                    db.Instructores.Add(i.Value);
                }
            }
        }

        private void SaveEmpresas(Dictionary<int, Empresa> empresas, string usuarioModificacion)
        {
            foreach (var e in empresas)
            {
                if (e.Value.EmpresaID == 0)
                {
                    e.Value.ForzarUsuario(usuarioModificacion);
                    db.Empresas.Add(e.Value);
                }
            }
        }

        private void SaveCursos(Dictionary<int, Curso> cursos)
        {
            foreach (var c in cursos)
            {
                if (c.Value.CursoID == 0)
                    db.Cursos.Add(c.Value);
            }
        }

        private void SaveCapacitados(Dictionary<int, Capacitado> capacitados, string usuarioModificacion)
        {
            int total = 0;

            foreach (var c in capacitados)
            {
                if (c.Value.CapacitadoID == 0)
                {
                    c.Value.ForzarUsuario(usuarioModificacion);
                    db.Capacitados.Add(c.Value);
                    total++;
                }
            }
        }

        private void SaveJornadas(Dictionary<int, Jornada> jornadas, string usuarioModificacion)
        {
            foreach (var j in jornadas)
            {
                if (j.Value.JornadaID == 0)
                {
                    j.Value.ForzarUsuario(usuarioModificacion);

                    if (string.IsNullOrEmpty(j.Value.Hora))
                        j.Value.Hora = "13:00";

                    db.Jornada.Add(j.Value);
                }
            }
        }

        private void SaveRegistrosCapacitacion(List<RegistroCapacitacion> registrosCapacitacion, string usuarioModificacion)
        {
            foreach (var r in registrosCapacitacion)
            {
                r.ForzarUsuario(usuarioModificacion);
                db.RegistroCapacitacion.Add(r);
            }
        }

        #endregion

        #region Ingreso masivo email Empresas

        public ActionResult IngresoMasivoEmailEmpresas()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IngresoMasivoEmailEmpresas(FormCollection formCollection)
        {
            if (Request != null)
            {
                HttpPostedFileBase file = Request.Files["UploadedFile"];

                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;

                    byte[] fileBytes = new byte[file.ContentLength];

                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));

                    var capacitados = new List<Capacitado>();

                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        return CargarMailsEmpresas(package);
                    }
                }
            }

            return View();
        }

        private ActionResult CargarMailsEmpresas(ExcelPackage ep)
        {
            const int colEmpresaId = 1;
            const int colEmpresaNombre = 2;
            const int colEmail1 = 6;
            const int colEmail2 = 7;
            const int colEmail3 = 8;
            const int colEmail4 = 9;

            var ws = ep.Workbook.Worksheets.ElementAt(0);

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            using (ExcelPackage epWrite = new ExcelPackage())
            {
                //se inicializa un archivo Excel de salida
                var wsWrite = epWrite.Workbook.Worksheets.Add("Empresas sin actualizar");

                const int rowInicial = 2;
                int j = rowInicial + 1;

                wsWrite.Cells[rowInicial, colEmpresaId].Value = "Id";
                wsWrite.Cells[rowInicial, colEmpresaNombre].Value = "Nombre";

                for (int i = 2; i <= TotalRows; i++)
                {
                    int empresaId = int.Parse(ws.Cells[i, colEmpresaId].Value.ToString());
                    string empresaNombre = ws.Cells[i, colEmpresaNombre].Value.ToString();

                    var empresa = db.Empresas.Where(e => e.NombreFantasia == empresaNombre).FirstOrDefault();

                    if (empresa != null) //la empresas se buscan por nombre por lo que podría no haberse encontrado
                    {
                        string emails = string.Empty;

                        string email1 = ws.Cells[i, colEmail1].Value != null ? ws.Cells[i, colEmail1].Value.ToString() : string.Empty;
                        string email2 = ws.Cells[i, colEmail2].Value != null ? ws.Cells[i, colEmail2].Value.ToString() : string.Empty;
                        string email3 = ws.Cells[i, colEmail3].Value != null ? ws.Cells[i, colEmail3].Value.ToString() : string.Empty;
                        string email4 = ws.Cells[i, colEmail4].Value != null ? ws.Cells[i, colEmail4].Value.ToString() : string.Empty;

                        emails = email1;

                        if (!string.IsNullOrEmpty(emails))
                        {
                            emails += email2 != string.Empty ? string.Format(",{0}", email2) : string.Empty;
                            emails += email3 != string.Empty ? string.Format(",{0}", email3) : string.Empty;
                            emails += email4 != string.Empty ? string.Format(",{0}", email4) : string.Empty;

                            empresa.Email = emails;
                        }
                    }
                    else //no se encontró una empresa con ese nombre fantasía por lo que se agrega un registro al archivo de salida
                    {
                        wsWrite.Cells[j, colEmpresaId].Value = empresaId;
                        wsWrite.Cells[j, colEmpresaNombre].Value = empresaNombre;
                        j++;
                    }
                }

                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {

                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            Trace.TraceInformation("Property: {0} Error: {1}",
                                                    validationError.PropertyName,
                                                    validationError.ErrorMessage);
                        }
                    }
                }

                if (wsWrite.Dimension.End.Row > 1) //el archivo de salida solo se genera si se le ingresan registros
                {
                    wsWrite.Cells[ws.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream();
                    epWrite.SaveAs(stream);

                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    stream.Position = 0;
                    return File(stream, contentType, "EmpresasSinActualizar.xlsx");
                }
            }
            return View();
        }

        #endregion

        public ActionResult IniciarFechaVencimientoRegistros()
        {
            return View("IniciarFechaVencimientoRegistros");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IniciarFechaVencimientoRegistros(FormCollection formCollection)
        {
            DateTime FechaCambioDuracion = new DateTime(2012, 1, 1);
            int anioVencimiento;
            DateTime fechaVencimiento;

            foreach (var r in db.RegistroCapacitacion.ToList())
            {
                if (r.Jornada.Fecha >= FechaCambioDuracion)
                    anioVencimiento = r.Jornada.Fecha.Year + 3;
                else
                    anioVencimiento = r.Jornada.Fecha.Year + 5;

                int mesVencimiento = r.Jornada.Fecha.Month;
                int diaVencimiento = r.Jornada.Fecha.Day;

                try
                {
                    fechaVencimiento = new DateTime(anioVencimiento, mesVencimiento, diaVencimiento);
                }
                catch (Exception)
                {
                    if (mesVencimiento == 2 && diaVencimiento == 29)
                        diaVencimiento = 28;
                    else
                        throw;
                }

                try
                {
                    fechaVencimiento = new DateTime(anioVencimiento, mesVencimiento, diaVencimiento);
                }
                catch (Exception)
                {

                    throw;
                }

                r.FechaVencimiento = fechaVencimiento;
            }

            db.SaveChanges();

            return View();
        }

        public ActionResult ActualizarRegistrosRF()
        {
            return View("ActualizarRegistrosRF");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarRegistrosRF(FormCollection formCollection)
        {
            HttpContext.Server.ScriptTimeout = 600; //se establece un timeout de 10 minutos

            bool actualizarDB;
            Boolean.TryParse(Request["actualizarDB"], out actualizarDB);

            bool convertirRFaTV;
            Boolean.TryParse(Request["convertirRFaTV"], out convertirRFaTV);

            List<RegistroCapacitacion> registrosAConvertir;

            List<Jornada> jornadasCreadas = new List<Jornada>();

            Curso cursoParaAsignar;

            string fileName;

            if (convertirRFaTV) //se convertirán un total de 133 regitros RF que en realidad son TV
            {
                registrosAConvertir = db.RegistroCapacitacion.Where(r => r.Jornada.CursoId == 2 && r.Nota > 0).ToList();
                cursoParaAsignar = db.Cursos.Find(1); //Curso TV

                fileName = "actualizacionTV.xlsx";
            }
            else //se convertiran registros de TA y TV sin nota a RF
            {
                registrosAConvertir = db.RegistroCapacitacion.Where(r => (r.Jornada.CursoId == 1 || r.Jornada.CursoId == 3) && r.Nota == 0).ToList();
                cursoParaAsignar = db.Cursos.Find(2); //Curso RF

                fileName = "actualizacionRF.xlsx";
            }

            if (actualizarDB)
            {
                foreach (var r in registrosAConvertir)
                {
                    bool jornadaCreada;
                    var nuevaJornada = obtenerNuevaJornada(r, cursoParaAsignar, out jornadaCreada, ref jornadasCreadas);

                    r.Jornada = nuevaJornada;
                }

                foreach (var j in jornadasCreadas)
                {
                    db.Jornada.Add(j);
                }

                try
                {
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Retrieve the error messages as a list of strings.
                    var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                    // Join the list to a single string.
                    var fullErrorMessage = string.Join("; ", errorMessages);

                    // Combine the original exception message with the new one.
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                    // Throw a new DbEntityValidationException with the improved exception message.
                    throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                }
            }
            else
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Capacitados");

                    const int rowInicial = 1;
                    int i = rowInicial + 1;

                    ws.Cells[rowInicial, 1].Value = "Capacitado";
                    ws.Cells[rowInicial, 2].Value = "Documento";
                    ws.Cells[rowInicial, 3].Value = "Empresa";
                    ws.Cells[rowInicial, 4].Value = "Curso";
                    ws.Cells[rowInicial, 5].Value = "Se asociará a la jornada";

                    foreach (var r in registrosAConvertir)
                    {
                        bool jornadaCreada;
                        var nuevaJornada = obtenerNuevaJornada(r, cursoParaAsignar, out jornadaCreada, ref jornadasCreadas);

                        Color jornadaBGColor;

                        if (jornadaCreada)
                            jornadaBGColor = Color.PaleGreen;
                        else
                            jornadaBGColor = Color.PeachPuff;

                        ws.Cells[i, 1].Value = r.Capacitado.NombreCompleto;
                        ws.Cells[i, 2].Value = r.Capacitado.DocumentoCompleto;
                        ws.Cells[i, 3].Value = r.Capacitado.Empresa.NombreFantasia;
                        ws.Cells[i, 4].Value = r.Jornada.Curso.Descripcion;

                        ws.Cells[i, 5].Value = nuevaJornada.JornadaIdentificacionCompleta;
                        ws.Cells[i, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, 5].Style.Fill.BackgroundColor.SetColor(jornadaBGColor);
                        
                        i++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);

                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    stream.Position = 0;
                    return File(stream, contentType, fileName);
                }
            }

            return View();
        }

        public ActionResult ActualizarFechasRF()
        {
            return View("ActualizarFechasRF");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarFechasRF(FormCollection formCollection)
        {
            bool actualizarDB;
            Boolean.TryParse(Request["actualizarDB"], out actualizarDB);

            var registrosRF = db.RegistroCapacitacion.Where(r => r.Jornada.CursoId == 2).ToList();

            if (actualizarDB)
            {
                foreach (var r in registrosRF)
                {
                    r.FechaVencimiento = ObtenerUltimoDiaAnio(r.Jornada.Fecha);
                }

                db.SaveChanges();
            }
            else
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Capacitados");

                    const int rowInicial = 1;
                    int i = rowInicial + 1;

                    ws.Cells[rowInicial, 1].Value = "Capacitado";
                    ws.Cells[rowInicial, 2].Value = "Documento";
                    ws.Cells[rowInicial, 4].Value = "Jornada";
                    ws.Cells[rowInicial, 5].Value = "Nueva fecha de vencimiento";

                    foreach (var r in registrosRF)
                    {
                        r.FechaVencimiento = ObtenerUltimoDiaAnio(r.Jornada.Fecha);

                        ws.Cells[i, 1].Value = r.Capacitado.NombreCompleto;
                        ws.Cells[i, 2].Value = r.Capacitado.DocumentoCompleto;
                        ws.Cells[i, 4].Value = r.Jornada.JornadaIdentificacionCompleta;
                        ws.Cells[i, 5].Value = r.FechaVencimiento.ToShortDateString();

                        i++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);

                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    stream.Position = 0;
                    return File(stream, contentType, "RegistrosRFActualizados.xlsx");
                }
            }

            return View();
        }

        private DateTime ObtenerUltimoDiaAnio(DateTime fechaVencimientoActual)
        {
            return new DateTime(fechaVencimientoActual.Year, 12, 31);
        }

        public ActionResult UnificarCedulasRepetidas()
        {
            return View("UnificarCedulasRepetidas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UnificarCedulasRepetidas(FormCollection formCollection)
        {
            HttpContext.Server.ScriptTimeout = 600; //se establece un timeout de 10 minutos

            bool actualizarDB;
            Boolean.TryParse(Request["actualizarDB"], out actualizarDB);

            List<List<int>> gruposCapacitadosIdsParaUnificar = new List<List<int>>();

            HttpPostedFileBase file = null;

            if (Request != null)
            {
                file = Request.Files["UploadedFile"];

                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;

                    byte[] fileBytes = new byte[file.ContentLength];

                    var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
                }

                //si se actualiza la DB el archivo de origen ya tiene marcado en la columna F el capacitado que se persistirá
                //y en la columna G se indica cuando se pasa de un capacitado a otro
                if (actualizarDB)
                {
                    using (var ep = new ExcelPackage(file.InputStream))
                    {
                        var ws = ep.Workbook.Worksheets[1];

                        var TotalColumns = ws.Dimension.End.Column;
                        var TotalRows = ws.Dimension.End.Row;

                        const int cCapacitadoId = 1;
                        const int cSeleccionado = 6;
                        const int cNuevoCapacitado = 7;

                        Capacitado capacitadoBase = null;
                        List<Capacitado> capacitadosAEliminar = null;

                        for (int i = 2; i < TotalRows + 1; i++)
                        {
                            bool capacitadoNuevo = false;
                            if (ws.Cells[i, cNuevoCapacitado].Value != null)
                                capacitadoNuevo = ws.Cells[i, cNuevoCapacitado].Value.ToString() == "nuevo";


                            bool capacitadoSeleccionado = false;
                            if (ws.Cells[i, cSeleccionado].Value != null)
                                capacitadoSeleccionado = ws.Cells[i, cSeleccionado].Value.ToString() == "X";

                            int capacitadoActualId = int.Parse(ws.Cells[i, cCapacitadoId].Value.ToString());
                            var capacitadoActual = db.Capacitados
                                                     .Include("RegistrosCapacitacion")
                                                     .SingleOrDefault(x => x.CapacitadoID == capacitadoActualId); //.Find(int.Parse(ws.Cells[i, cCapacitadoId].Value.ToString()));

                            if (capacitadoNuevo)
                            {
                                //impactar los cambios del Capacitado anterior
                                if (capacitadoBase != null)
                                {
                                    UnificarCapacitados(capacitadoBase, capacitadosAEliminar);
                                    capacitadoBase = null;           
                                }

                                capacitadosAEliminar = new List<Capacitado>();
                            }

                            if (capacitadoSeleccionado)
                                    capacitadoBase = capacitadoActual;
                                else
                                    capacitadosAEliminar.Add(capacitadoActual);
                        }

                        if (capacitadoBase != null)
                        {
                            UnificarCapacitados(capacitadoBase, capacitadosAEliminar);
                        }

                        db.SaveChanges();
                    }
                }
                else
                {
                    var capacitados = new List<Capacitado>();

                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        gruposCapacitadosIdsParaUnificar = GruposCapacitadosIdsParaUnificar(package);
                    }

                    using (ExcelPackage package = new ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("Capacitados");

                        const int rowInicial = 1;
                        int i = rowInicial + 1;

                        ws.Cells[rowInicial, 1].Value = "Id";
                        ws.Cells[rowInicial, 2].Value = "Documento";
                        ws.Cells[rowInicial, 3].Value = "Nombre";
                        ws.Cells[rowInicial, 4].Value = "Apellido";
                        ws.Cells[rowInicial, 5].Value = "Empresa";
                        ws.Cells[rowInicial, 6].Value = "Seleccionado";

                        foreach (var capacitadosIdsParaUnificar in gruposCapacitadosIdsParaUnificar)
                        {
                            List<Capacitado> capacitadosParaUnificar = new List<Capacitado>();

                            foreach (var capacitadoId in capacitadosIdsParaUnificar)
                            {
                                var capacitado = db.Capacitados.Find(capacitadoId);
                                capacitadosParaUnificar.Add(capacitado);
                            }

                            int? posicionCapacitadoBase = seleccionarPosicionCapacitadoBase(capacitadosParaUnificar);
                            int j = 0;

                            foreach (var c in capacitadosParaUnificar)
                            {
                                ws.Cells[i, 1].Value = c.CapacitadoID;
                                ws.Cells[i, 2].Value = c.DocumentoCompleto;
                                ws.Cells[i, 3].Value = c.Nombre;
                                ws.Cells[i, 4].Value = c.Apellido;
                                ws.Cells[i, 5].Value = c.Empresa.NombreFantasia;

                                if (posicionCapacitadoBase != null)
                                {
                                    if (j == posicionCapacitadoBase)
                                        ws.Cells[i, 6].Value = "X";
                                }

                                j++;
                                i++;
                            }

                            if (posicionCapacitadoBase == null)
                            {
                                ws.Cells[i - capacitadosParaUnificar.Count, 1, i - 1, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                ws.Cells[i - capacitadosParaUnificar.Count, 1, i - 1, 6].Style.Fill.BackgroundColor.SetColor(Color.Salmon);
                            }

                            ws.Cells[i - capacitadosParaUnificar.Count, 1, i - 1, 6].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                        }

                        ws.Cells[ws.Dimension.Address].AutoFitColumns();

                        var stream = new MemoryStream();
                        package.SaveAs(stream);

                        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                        stream.Position = 0;
                        return File(stream, contentType, "UnificarCapacitados.xlsx");
                    }
                }
            }

            return View();
        }

        private List<List<int>> GruposCapacitadosIdsParaUnificar(ExcelPackage ep)
        {
            List<List<int>> grupos = new List<List<int>>();

            var ws = ep.Workbook.Worksheets[1];

            var TotalColumns = ws.Dimension.End.Column;
            var TotalRows = ws.Dimension.End.Row;

            const int cCapacitadoId = 1;
            const int cDocumento = 4;

            string documentoActual = ws.Cells[1, cDocumento].Value.ToString();
            List<int> grupo = new List<int>();

            for (int i = 1; i < TotalRows; i++)
            {
                if (documentoActual != ws.Cells[i, cDocumento].Value.ToString())
                {
                    documentoActual = ws.Cells[i, cDocumento].Value.ToString();
                    grupos.Add(grupo);
                    grupo = new List<int>();
                }

                grupo.Add(int.Parse(ws.Cells[i, cCapacitadoId].Value.ToString()));
            }

            return grupos;
        }

        private int? seleccionarPosicionCapacitadoBase(List<Capacitado> capacitadosParaUnificar)
        {
            int? posicionCapacitadoBase = null;
            int iActual = 0;

            foreach (var capacitadoActual in capacitadosParaUnificar)
            {
                for (int i = 0; i < capacitadosParaUnificar.Count; i++)
                {
                    if (i == iActual)
                        continue;

                    if (quitarTildes(capacitadoActual.Apellido).Contains(quitarTildes(capacitadosParaUnificar[i].Apellido)))
                    {
                        posicionCapacitadoBase = iActual;
                    }
                }

                iActual++;
            }

            return posicionCapacitadoBase;
        }

        private string quitarTildes(string textoOriginal)
        {
            return textoOriginal.Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");
        }

        private void UnificarCapacitados(Capacitado capacitadoBase, List<Capacitado> capacitadosAEliminar)
        {
            foreach (var ce in capacitadosAEliminar)
            {
                foreach (var r in ce.RegistrosCapacitacion)
                {
                    capacitadoBase.RegistrosCapacitacion.Add(r);
                }

                db.Capacitados.Remove(ce);
            }
        }

        private Jornada obtenerNuevaJornada(RegistroCapacitacion r, Curso cursoRF, out bool jornadaCreada, ref List<Jornada> jornadasCreadas)
        {
            jornadaCreada = false;
            string jornadaHora = "13:00";

            Jornada jornada = jornadasCreadas.Where(j => j.Curso.Descripcion == cursoRF.Descripcion &&
                                                         j.Fecha == r.Jornada.Fecha &&
                                                         j.Hora == jornadaHora &&
                                                         j.Lugar.NombreLugar == r.Jornada.Lugar.NombreLugar).FirstOrDefault();

            if (jornada == null)
            {
                jornada = new Jornada
                          {
                            Curso = cursoRF,
                            Fecha = r.Jornada.Fecha,
                            Hora = jornadaHora,
                            Instructor = r.Jornada.Instructor,
                            Lugar = r.Jornada.Lugar
                          };

                jornadasCreadas.Add(jornada);

                jornadaCreada = true;
            }

            return jornada;
        }

        public ActionResult AsignarCapacitadosFotos()
        {
            return View("AsignarCapacitadosFotos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarCapacitadosFotos(FormCollection formCollection)
        {
            List<String> mensajes = new List<string>();

            using (ExcelPackage package = new ExcelPackage())
            {
                var wsFotoYaAsociada = package.Workbook.Worksheets.Add("Foto ya asociada");
                var wsArchivoError = package.Workbook.Worksheets.Add("No se pudo asociar archivo");
                var wsDocumentoError = package.Workbook.Worksheets.Add("Error en documento");

                int rowFotoYaAsociada = 1;
                int rowArchivoError = 1;
                int rowErrorDocumento = 1;

                foreach (var file in Directory.GetFiles(Server.MapPath("~/Images/FotosCapacitadosImportar")))
                {
                    string nombreArchivo = System.IO.Path.GetFileName(file);

                    string ci = ObtenerCINombreArchivo(nombreArchivo);

                    if (!string.IsNullOrEmpty(ci))
                    {
                        var capacitado = db.Capacitados.Where(c => c.Documento.Contains(ci)).FirstOrDefault();

                        if (capacitado != null)
                        {
                            if (capacitado.PathArchivo == null)
                            {
                                if (capacitado.TipoDocumento.Abreviacion == "CI" &&
                                    !ValidationHelper.GetInstance().ValidateCI(capacitado.Documento))
                                {
                                    //mensajes.Add(string.Format("Cédula identidad incorrecta {0} - {1} - {2}", capacitado.CapacitadoID, capacitado.Documento, capacitado.NombreCompleto));
                                    //continue;

                                    TipoDocumento tipoDocumentoOtro = db.TiposDocumento.Find(3);

                                    //capacitado.Documento = ValidationHelper.GetInstance().RemoveGuionCI(capacitado.Documento);
                                    capacitado.TipoDocumento = tipoDocumentoOtro;

                                    cargarWSErrorAsignarCapacitadosFotos(ref wsDocumentoError, rowErrorDocumento, capacitado.CapacitadoID.ToString());
                                    rowErrorDocumento++;
                                }

                                string nombreArchivoDestino = PathArchivoHelper.GetInstance().ObtenerNombreFotoCapacitado(capacitado.CapacitadoID,
                                                                                                       System.IO.Path.GetExtension(file));

                                string nombreCarpetaDestino = PathArchivoHelper.GetInstance().ObtenerCarpetaFotoCapacitado(capacitado.CapacitadoID);

                                string pathCarpetaDestino = Path.Combine(Server.MapPath("~/Images/FotosCapacitados/"), nombreCarpetaDestino);
                                string pathArchivoDestino = Path.Combine(pathCarpetaDestino, nombreArchivoDestino);

                                PathArchivo pathArchivo = new PathArchivo
                                {
                                    NombreArchivo = nombreArchivoDestino,
                                    SubDirectorio = nombreCarpetaDestino,
                                    TipoArchivo = TiposArchivo.FotoCapacitado
                                };

                                if (!System.IO.Directory.Exists(pathCarpetaDestino))
                                    System.IO.Directory.CreateDirectory(pathCarpetaDestino);

                                if (!System.IO.File.Exists(pathArchivoDestino))
                                {
                                    System.IO.File.Copy(file, pathArchivoDestino);
                                }
                                else
                                {
                                    //cargarWSErrorAsignarCapacitadosFotos(ref wsArchivoError, rowArchivoError, string.Format("Ya existe el archivo {0}", pathArchivoDestino));
                                    rowArchivoError++;
                                }

                                capacitado.PathArchivo = pathArchivo;

                            }
                            else
                            {
                                mensajes.Add(string.Format("El capacitado {0} - {1} ya tiene una foto asociada", capacitado.DocumentoCompleto, capacitado.NombreCompleto));
                                cargarWSErrorAsignarCapacitadosFotos(ref wsFotoYaAsociada, rowFotoYaAsociada, capacitado.DocumentoCompleto);
                                rowFotoYaAsociada++;
                            }
                        }
                        else
                        {
                            mensajes.Add(string.Format("No se puedo asociar el archivo {0} a ningún capacitado", nombreArchivo));
                            cargarWSErrorAsignarCapacitadosFotos(ref wsArchivoError, rowArchivoError, nombreArchivo);
                            rowArchivoError++;

                        }
                    }
                    else //el nombre del archivo no contiene el documento
                    {
                        mensajes.Add(string.Format("No se puedo asociar el archivo {0} a ningún capacitado porque el nombre del archivo no contiene ningúm documento válido", nombreArchivo));
                        cargarWSErrorAsignarCapacitadosFotos(ref wsArchivoError, rowArchivoError, nombreArchivo);
                        rowArchivoError++;
                    }
                }

                try
                {
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    //TODO: Mejorar el mensaje de error que se muestra cuando ocurre un error durante la migración de datos
                    // Retrieve the error messages as a list of strings.
                    var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                    // Join the list to a single string.
                    var fullErrorMessage = string.Join("; ", errorMessages);

                    // Combine the original exception message with the new one.
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                    // Throw a new DbEntityValidationException with the improved exception message.
                    throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                }

                if (wsFotoYaAsociada.Dimension != null)
                    wsFotoYaAsociada.Cells[wsFotoYaAsociada.Dimension.Address].AutoFitColumns();

                if (wsArchivoError.Dimension != null)
                    wsArchivoError.Cells[wsArchivoError.Dimension.Address].AutoFitColumns();

                if (wsDocumentoError.Dimension != null)
                    wsDocumentoError.Cells[wsDocumentoError.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "erroresFotos.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        private void cargarWSErrorAsignarCapacitadosFotos(ref ExcelWorksheet ws, int rowActual, string error)
        {
            ws.Cells[rowActual, 1].Value = error;
        }

        [HttpGet]
        public ActionResult ObtenerCargando()
        {
            return PartialView("_CargandoPartial");
        }

        private string ObtenerCINombreArchivo(string nombreArchivo)
        {
            return System.Text.RegularExpressions.Regex.Match(nombreArchivo, @"\d+").Value;
        }

        public ActionResult IniciarNotificacionesVencimientos()
        {
            return View("IniciarNotificacionesVencimientos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IniciarNotificacionesVencimientos(FormCollection formCollection)
        {
            foreach (var r in db.RegistroCapacitacion)
            {
                NotificacionVencimiento n = new NotificacionVencimiento();
                n.Fecha = DateTime.Now;
                n.Estado = EstadoNotificacionVencimiento.NoNotificar;
                n.RegistroCapacitacion = r;

                db.NotificacionVencimientos.Add(n);
            }

            db.SaveChanges();

            return View();
        }

    }
}
