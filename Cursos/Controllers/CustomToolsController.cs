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

namespace Cursos.Controllers
{
    [Authorize(Users = "guillefra@gmail.com,alejandro.lacruz@gmail.com")]
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

            SaveLugares(lugares);
            SaveInstructores(instructores);
            SaveEmpresas(empresas);
            SaveCursos(cursos);

            SaveCapacitados(capacitados);
            SaveJornadas(jornadas);
            SaveRegistrosCapacitacion(registrosCapacitacion);

            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException e)
            {
                //TODO: Mejorar el mensaje de error que se muestra cuando ocurre un error durante la migración de datos
                /*
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
                */
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
                    ins.Documento = ws.Cells[i, 4].Value.ToString();

                    var FechaNacimientoStr = ws.Cells[i, 5].Value == null ? String.Empty : ws.Cells[i, 5].Value.ToString();
                    ins.FechaNacimiento = FechaNacimientoStr == String.Empty ? DateTime.Parse("13/09/1980") : DateTime.Parse(FechaNacimientoStr);

                    ins.Domicilio = ws.Cells[i, 6].Value.ToString();
                    ins.Telefono = ws.Cells[i, 7].Value.ToString();
                    ins.Activo = bool.Parse(ws.Cells[i, 9].Value.ToString());
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
                string RUT = ws.Cells[i, 5].Value.ToString();

                if (!String.IsNullOrEmpty(RUT))
                    e = db.Empresas.Where(empresa => empresa.RUT == RUT).FirstOrDefault();
                else
                    e = db.Empresas.Where(empresa => empresa.NombreFantasia == nombreFantasia).FirstOrDefault();

                if (e == null)
                {
                    e = new Empresa();

                    e.NombreFantasia = nombreFantasia;
                    e.Domicilio = ws.Cells[i, 3].Value.ToString();
                    e.RazonSocial = ws.Cells[i, 4].Value.ToString();
                    e.RUT = RUT;
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

            TipoDocumento td_Default = db.TiposDocumento.Where(td => td.Abreviacion == "N/E").First();
            TipoDocumento td_CIUruguay = db.TiposDocumento.Where(td => td.Abreviacion == "CI").First();

            for (int i = 2; i <= TotalRows; i++)
            {
                var c = new Capacitado();

                int key = int.Parse(ws.Cells[i, 1].Value.ToString());

                TipoDocumento tipoDocumento = td_Default;

                string documento = ws.Cells[i, 4].Value.ToString();
                string documentoSinGuion = ws.Cells[i, 4].Value.ToString(); //en el archivo Excel, el documento puede estar sin guión

                if (documento.Contains("-")) //si el documento tiene un guión, podría ser una CI uruguaya
                {
                    documentoSinGuion = validationHelper.RemoveGuionCI(documento);
                }

                if (validationHelper.ValidateCI(documentoSinGuion)) //si el documento es una CI válida
                {
                    documento = documentoSinGuion;
                    tipoDocumento = td_CIUruguay;
                }

                c.Nombre = ws.Cells[i, 2].Value.ToString();
                c.Apellido = ws.Cells[i, 3].Value.ToString();
                c.TipoDocumento = tipoDocumento;
                c.Documento = documento;
                c.Fecha = DateTime.Parse("13/09/1980");

                var keyEmpresa = int.Parse(ws.Cells[i, 7].Value.ToString());
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

                var keyCapacitado = int.Parse(ws.Cells[i, 6].Value.ToString());

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

        private void SaveInstructores(Dictionary<int, Instructor> instructores)
        {
            foreach (var i in instructores)
            {
                if (i.Value.InstructorID == 0)
                    db.Instructores.Add(i.Value);
            }
        }

        private void SaveEmpresas(Dictionary<int, Empresa> empresas)
        {
            foreach (var e in empresas)
            {
                if (e.Value.EmpresaID == 0)
                    db.Empresas.Add(e.Value);
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

        private void SaveCapacitados(Dictionary<int, Capacitado> capacitados)
        {
            foreach (var c in capacitados)
            {
                if (c.Value.CapacitadoID == 0)
                    db.Capacitados.Add(c.Value);
            }
        }

        private void SaveJornadas(Dictionary<int, Jornada> jornadas)
        {
            foreach (var j in jornadas)
            {
                if (j.Value.JornadaID == 0)
                    db.Jornada.Add(j.Value);
            }
        }

        private void SaveRegistrosCapacitacion(List<RegistroCapacitacion> registrosCapacitacion)
        {
            foreach (var r in registrosCapacitacion)
            {
                db.RegistroCapacitacion.Add(r);
            }
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

                //db.SaveChanges();
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

        private Jornada obtenerNuevaJornada(RegistroCapacitacion r, Curso cursoRF, out bool jornadaCreada, ref List<Jornada> jornadasCreadas)
        {
            jornadaCreada = false;

            Jornada jornada = jornadasCreadas.Where(j => j.Curso.Descripcion == cursoRF.Descripcion &&
                                                         j.Fecha == r.Jornada.Fecha &&
                                                         j.Hora == r.Jornada.Hora &&
                                                         j.Lugar.NombreLugar == r.Jornada.Lugar.NombreLugar).FirstOrDefault();

            if (jornada == null)
            {
                jornada = new Jornada
                          {
                            Curso = cursoRF,
                            Fecha = r.Jornada.Fecha,
                            Hora = r.Jornada.Hora,
                            Instructor = r.Jornada.Instructor,
                            Lugar = r.Jornada.Lugar
                          };

                jornadasCreadas.Add(jornada);

                jornadaCreada = true;
            }

            return jornada;
        }

    }
}
