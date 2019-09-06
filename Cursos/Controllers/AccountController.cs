using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Cursos.Models;
using System.Net;
using Cursos.Helpers;
using System.Collections.Generic;
using Microsoft.AspNet.Identity.EntityFramework;
using PagedList;
using System.Data.Entity;
using System.IO;
using System.Drawing;
using OfficeOpenXml;

namespace Cursos.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        private ApplicationDbContext db = new ApplicationDbContext();
        private CursosDbContext dbcursos = new CursosDbContext();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ActionResult Index(string currentNombreUsuario, string nombreUsuario,
                                  int? currentEmpresaID, int? EmpresaID, 
                                  string currentRoleId, string RoleId,
                                  int? page, bool? exportarExcel)
        {
            if (nombreUsuario != null) //si el parámetro vino con algún valor es porque se presionó buscar y se resetea la página a 1
                page = 1;
            else
                nombreUsuario = currentNombreUsuario;

            ViewBag.CurrentNombreUsuario = nombreUsuario;

            if (EmpresaID != null)
                page = 1;
            else
            {
                if (currentEmpresaID == null)
                    currentEmpresaID = -1;

                EmpresaID = currentEmpresaID;
            }

            ViewBag.CurrentEmpresaID = EmpresaID;

            if (RoleId != null)
                page = 1;
            else
            {
                if (currentRoleId == null)
                    currentRoleId = string.Empty;

                RoleId = currentRoleId;
            }

            ViewBag.CurrentRoleId = RoleId;

            var users = (IQueryable<ApplicationUser>) db.Users;

            List<IdentityRole> rolesDD = db.Roles.OrderBy(r => r.Name).ToList();
            rolesDD.Insert(0, new IdentityRole { Id = string.Empty, Name = "Todas" } );
            ViewBag.RoleId = new SelectList(rolesDD, "Id", "Name", RoleId);

            List<Empresa> empresasDD = dbcursos.Empresas.OrderBy(e => e.NombreFantasia).ToList();
            empresasDD.Insert(0, new Empresa { EmpresaID = -1, NombreFantasia = "Todas" });
            ViewBag.EmpresaID = new SelectList(empresasDD, "EmpresaID", "NombreFantasia", EmpresaID);

            if (!String.IsNullOrEmpty(nombreUsuario))
                users = users.Where(u => u.UserName.Contains(nombreUsuario));

            if (EmpresaID != -1 && EmpresaID != null)
            {
                var usuariosEmpresa = new List<String>();

                foreach (var empresaUsuario in dbcursos.EmpresasUsuarios.Where(eu => eu.EmpresaID == EmpresaID))
                {
                    usuariosEmpresa.Add(empresaUsuario.Usuario);
                }

                users = users.Where(u => usuariosEmpresa.Contains(u.UserName));
            }

            if (!String.IsNullOrEmpty(RoleId))
                users = users.Where(u => u.Roles.Select(r => r.RoleId).Contains(RoleId));

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            users = users.OrderBy(o => o.Email);

            ViewBag.TotalUsuarios = users.Count();

            bool exportar = exportarExcel != null ? (bool)exportarExcel : false;

            if (!exportar)
                return View(users.ToPagedList(pageNumber, pageSize));
            else
                return ExportDataExcel(users.ToList());

        }

        // GET: Account/Edit/5
        [Authorize(Roles = "Administrador")]
        public ActionResult Edit(String id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var usuario = db.Users.Where(u => u.Id == id).FirstOrDefault();
            if (usuario == null)
            {
                return HttpNotFound();
            }

            string rolActual = UsuarioHelper.GetInstance().ObtenerRoleName(usuario.Roles.ElementAt(0).RoleId);
            ViewBag.RoleName = new SelectList(db.Roles.OrderBy(r => r.Name).ToList(), "Name", "Name", rolActual);

            if (rolActual == "ConsultaEmpresa")
                ViewBag.EmpresaID = new SelectList(dbcursos.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia", UsuarioHelper.GetInstance().ObtenerEmpresaAsociada(usuario.UserName).EmpresaID);
            else
                ViewBag.EmpresaID = new SelectList(dbcursos.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia");

            ViewBag.PermitirInscripcionesExternas = false;

            foreach (var ur in usuario.Roles)
            {
                var role = db.Roles.Where(r => r.Id == ur.RoleId).FirstOrDefault();

                if (role.Name == "InscripcionesExternas")
                {
                    ViewBag.PermitirInscripcionesExternas = true;
                    break;
                }
            } 

            return View(usuario);
        }

        // POST: Account/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit()
        {
            string id = Request["Id"];
            string roleNameEditado = Request["RoleName"];
            Empresa empresaEditada = dbcursos.Empresas.Find(int.Parse(Request["EmpresaID"]));

            //https://blog.productiveedge.com/asp-net-mvc-checkboxfor-explained
            bool permitirInscripcionesExternas = Request["PermitirInscripcionesExternas"].Contains("true") ? true : false;

            var usuario = db.Users.Where(u => u.Id == id).FirstOrDefault();

            bool usuarioPuedeInscripcionesExternas = UsuarioHelper.GetInstance().UsuarioTieneRol(usuario.UserName, "InscripcionesExternas");

            string roleNameActual = UsuarioHelper.GetInstance().ObtenerRoleName(usuario.Roles.ElementAt(0).RoleId);
            var empresaActual = UsuarioHelper.GetInstance().ObtenerEmpresaAsociada(usuario.UserName);

            bool quitarAsociacionEmpresa = false;

            //si se modificó el rol del usuario
            if (roleNameActual != roleNameEditado)
            {
                if (roleNameActual == "ConsultaEmpresa") //si antes el usuario estaba asociado a una empresa, se debe remover la asociación a esta empresa
                    quitarAsociacionEmpresa = true;

                this.UserManager.RemoveFromRole(id, roleNameActual);
                await this.UserManager.AddToRoleAsync(id, roleNameEditado);
            }

            //si está seleccionada la opción "PermitirInscripcionesExternas" y el usuario editado no tenía el rol asociado, se le agrega el rol "InscripcionesExternas"
            if (permitirInscripcionesExternas)
            { 
                if (!usuarioPuedeInscripcionesExternas && roleNameEditado == "ConsultaEmpresa")
                    await this.UserManager.AddToRoleAsync(id, "InscripcionesExternas");
            }
            else //si no está seleccionada la opción "PermitirInscripcionesExternas" y el usuario editado no tenía el rol asociado anteriormente, se retira el rol
            {
                if (usuarioPuedeInscripcionesExternas)
                    this.UserManager.RemoveFromRole(id, "InscripcionesExternas");
            }

            int empresaActualID = -1;
            if (empresaActual != null)
                empresaActualID = empresaActual.EmpresaID;

            int empresaEditadaID = -1;
            if (empresaEditada != null)
                empresaEditadaID = empresaEditada.EmpresaID;

            //si el usuario estaba asociado a una empresa y se cambió esa empresa, o si tenía el rol "ConsultaEmpresa" y cambió por otro 
            if ((empresaActualID != -1 && empresaActualID != empresaEditadaID) || quitarAsociacionEmpresa)
                this.RemoverAsociacionUsuarioAEmpresa(usuario.UserName);

            if (roleNameEditado == "ConsultaEmpresa")
                if (empresaActualID != empresaEditadaID)
                    this.AsociarUsuarioAEmpresa(empresaEditadaID, usuario.UserName);

            dbcursos.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Empresas/Delete/5
        [Authorize(Roles = "Administrador")]
        public ActionResult Delete(String id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var usuario = db.Users.Where(u => u.Id == id).FirstOrDefault();
            if (usuario == null)
            {
                return HttpNotFound();
            }
            return View(usuario);
        }

        // POST: Empresas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(String id)
        {
            var usuario = db.Users.Where(u => u.Id == id).FirstOrDefault();

            //si era un usuario de tipo ConsultaEmpresa se debe remover la asociación con la empresa
            if (UsuarioHelper.GetInstance().ObtenerRoleName(usuario.Roles.ElementAt(0).RoleId) == "ConsultaEmpresa")
                RemoverAsociacionUsuarioAEmpresa(usuario.UserName);

            db.Users.Remove(usuario);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            //si se intenta hacer cualquier acción desde jornadas.csl.uy, se redirige a la página de Jornadas Disponibles
            //if (Request.Url.Host == "jornadas.csl.uy" || System.Web.HttpContext.Current.User.IsInRole("InscripcionesExternas"))

                if (Request.Url.Host == "jornadas.csl.uy")
                    return RedirectToAction("Disponibles", "Jornadas");



            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [Authorize(Roles = "Administrador")]
        public ActionResult Register()
        {
            ViewBag.RoleName = new SelectList(db.Roles.OrderBy(r => r.Name).ToList(), "Name", "Name");
            ViewBag.EmpresaID = new SelectList(dbcursos.Empresas.OrderBy(e => e.NombreFantasia).ToList(), "EmpresaID", "NombreFantasia");

            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    await this.UserManager.AddToRoleAsync(user.Id, model.RoleName);

                    if (model.RoleName == "ConsultaEmpresa" && model.PermitirInscripcionesExternas)
                        await this.UserManager.AddToRoleAsync(user.Id, "InscripcionesExternas");

                    if (model.RoleName == "ConsultaEmpresa")
                        this.AsociarUsuarioAEmpresa(model.EmpresaID, model.Email);

                    return RedirectToAction("Index");
                }

                ViewBag.RoleName = new SelectList(db.Roles.ToList(), "Name", "Name");
                ViewBag.EmpresaID = new SelectList(dbcursos.Empresas.ToList(), "EmpresaID", "NombreFantasia");

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //-----------------------

        // GET: /Account/ForcePassword/123456
        [AllowAnonymous]
        public ActionResult ForcePassword(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var usuario = db.Users.Where(u => u.Id == id).FirstOrDefault();
            if (usuario == null)
            {
                return HttpNotFound();
            }

            return View(usuario);
        }

        //
        // POST: /Account/ForcePassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForcePassword()
        {
            string id = Request["Id"];
            string nuevaPassword = Request["NuevaPassword"];

            var usuario = db.Users.Where(u => u.Id == id).FirstOrDefault();
            if (usuario == null)
            {
                return HttpNotFound();
            }

            var result = UserManager.PasswordValidator.ValidateAsync(nuevaPassword).Result;

            if (result.Succeeded)
            {
                UserManager.RemovePassword(id);
                UserManager.AddPassword(id, nuevaPassword);

                return RedirectToAction("Index", "Account");
            }

            AddErrors(result);
            return View(usuario);
        }

        //-----------------------------

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion

        private void AsociarUsuarioAEmpresa(int empresaID, string usuario)
        {
            EmpresaUsuario eu = new EmpresaUsuario { Usuario = usuario };

            dbcursos.Empresas.Find(empresaID).Usuarios.Add(eu);
            dbcursos.SaveChanges();
        }

        private void RemoverAsociacionUsuarioAEmpresa(string usuario)
        {
            var eu = dbcursos.EmpresasUsuarios.Where(eus => eus.Usuario == usuario).FirstOrDefault();
            dbcursos.EmpresasUsuarios.Remove(eu);
            dbcursos.SaveChanges();
        }

        private ActionResult ExportDataExcel(List<ApplicationUser> usuarios)
        {
             using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Usuarios");

                int rowHeader = 1;
                int i = rowHeader + 1;

                const int colUsuario = 1;
                const int colRol = 2;
                const int colEmpresa = 3;

                ws.Cells[rowHeader, colUsuario].Value = "Usuario";
                ws.Cells[rowHeader, colRol].Value = "Rol";
                ws.Cells[rowHeader, colEmpresa].Value = "Empresa";

                var bgColor = Color.White;

                foreach (var u in usuarios)
                {
                    ws.Cells[i, colUsuario].Value = u.Email;
                    ws.Cells[i, colRol].Value = UsuarioHelper.GetInstance().ObtenerRoleName(u.Roles.ElementAt(0).RoleId);
                    ws.Cells[i, colEmpresa].Value = UsuarioHelper.GetInstance().ObtenerNombreEmpresaAsociada(u.Email);

                    //se seleccionan las columnas con datos del capacitado para setear el background color.
                    if (bgColor != Color.White)
                    {
                        ws.Cells[i, colUsuario, i, colEmpresa].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[i, colUsuario, i, colEmpresa].Style.Fill.BackgroundColor.SetColor(bgColor);
                    }

                    //se pone un borde alrededor del renglón
                    ws.Cells[i, colUsuario, i, colEmpresa].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    bgColor = bgColor == Color.White ? Color.WhiteSmoke : Color.White;

                    i++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "usuarios.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }
    }
}