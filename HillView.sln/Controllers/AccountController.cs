using DTOs;
using Microsoft.IdentityModel.Tokens;
using Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.Caching;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HillView.sln.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly WebInfoService _webInfoService;
        public AccountController(AccountService accountService, WebInfoService webInfoService)
        {
            _accountService = accountService;
            _webInfoService = webInfoService;
        }

        #region Login
        [Route("login")]
        public async Task<ActionResult> Login(string message)
        {
            ViewBag.Message = message;
            ViewBag.WebLogo = await _webInfoService.GetBrandLogoAsync();
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    result = false,
                    message = "Please enter your email and password.",
                    redirectURL = ""
                });
            }

            var data = _accountService.AuthenticateUser(
                model, Request.UserHostAddress);

            if (data.UserId <= 0)
            {
                return Json(new
                {
                    result = false,
                    message = data.Role,
                    redirectURL = ""
                });
            }

            // Force first-time users to change their password.
            if (data.IsFirstLogin)
            {
                // Remove any existing authenticated token.
                var existingCookie = new HttpCookie(
                    ConstantVM.WebUserCookie, "")
                {
                    Expires = DateTime.UtcNow.AddDays(-1),
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection,
                    Path = "/"
                };

                Response.Cookies.Add(existingCookie);

                Session.Remove("UName");

                // Store only the identity needed after password change.
                Session["FirstLoginUserId"] = data.UserId;
                Session["FirstLoginEmail"] = data.Email;
                Session["FirstLoginUserName"] = data.UserName;
                Session["FirstLoginRole"] = data.Role;

                return Json(new
                {
                    result = true,
                    message = "Please change your password to continue.",
                    redirectURL = Url.Action(
                        "ChangePassword", "Account")
                });
            }

            // Normal login for returning users.
            GenerateJwtToken(
                data.UserId,
                data.Email,
                data.UserName,
                data.Role);

            Session.Remove("FirstLoginUserId");
            Session.Remove("FirstLoginEmail");
            Session.Remove("FirstLoginUserName");
            Session.Remove("FirstLoginRole");

            return Json(new
            {
                result = true,
                message = "",
                redirectURL = "/admin/dashboard"
            });
        }


        #endregion
        // GET: Account

        #region Register

        [HttpGet]
        [Route("oobg-register")]
        public ActionResult Register()
        {
            var model = new RegisterViewModel();
            LoadRegisterData(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("register")]
        public ActionResult Register(RegisterViewModel model)
        {
            // A new role can be added separately.
            if (!ModelState.IsValid)
            {
                LoadRegisterData(model);
                return View(model);
            }

            try
            {
                var success = _accountService.RegisterUser(
                    model,
                    Session["UName"] as string,
                    Request.UserHostAddress);

                model.Message = success
                    ? "User registered successfully."
                    : "Email already exists or the selected role is invalid.";

                if (success)
                {
                    ModelState.Clear();
                    model = new RegisterViewModel
                    {
                        Message = "User registered successfully."
                    };
                }
            }
            catch (Exception)
            {
                // Log the exception using your existing error logging convention.
                model.Message = "Unable to register the user. Please try again.";
            }

            LoadRegisterData(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("register/add-role")]
        public ActionResult AddRole(RegisterViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewRoleTitle))
            {
                model.Message = "Please enter a role name.";
            }
            else
            {
                try
                {
                    bool added = _accountService.AddRole(
                        model.NewRoleTitle,
                        Session["UName"] as string,
                        Request.UserHostAddress);

                    model.Message = added
                        ? "Role added successfully."
                        : "That role already exists.";
                }
                catch (Exception)
                {
                    model.Message = "Unable to add the role.";
                }
            }

            LoadRegisterData(model);
            return View("Register", model);
        }

        private void LoadRegisterData(RegisterViewModel model)
        {
            var roles = _accountService.GetRoles();

            model.Roles = new SelectList(
                roles, "Id", "Title", model.RoleId);

            model.AllRoles = _accountService.GetRoles();

            model.Users = _accountService.GetUsers();
        }

        #endregion

        #region Change Password

        [HttpGet]
        [Route("change-password")]
        public ActionResult ChangePassword()
        {
            bool isFirstLogin = Session["FirstLoginUserId"] != null;
            var user = GetLoggedInUserFromToken();

            if (!isFirstLogin && user == null)
                return RedirectToAction("Login");

            ViewBag.IsFirstLogin = isFirstLogin;

            return View(new ChangePasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("change-password")]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            bool isFirstLogin = Session["FirstLoginUserId"] != null;
            var user = GetLoggedInUserFromToken();

            if (!isFirstLogin && user == null)
                return RedirectToAction("Login");

            ViewBag.IsFirstLogin = isFirstLogin;

            if (!ModelState.IsValid)
                return View(model);

            if (string.Equals(
        model.CurrentPassword,
        model.NewPassword,
        StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    nameof(model.NewPassword),
                    "Your new password must be different from your current password.");

                return View(model);
            }

            try
            {
                int userId;
                bool changed;

                if (isFirstLogin)
                {
                    // Case 1: Forced password change on first login.
                    userId = Convert.ToInt32(Session["FirstLoginUserId"]);

                    changed = _accountService.ChangeFirstLoginPassword(
                        userId,
                        model.CurrentPassword,
                        model.NewPassword);

                    if (!changed)
                    {
                        ModelState.AddModelError(
                            "",
                            "Current password is incorrect, or the account is no longer eligible for this change.");

                        return View(model);
                    }

                    string email = Session["FirstLoginEmail"] as string;
                    string userName = Session["FirstLoginUserName"] as string;
                    string role = Session["FirstLoginRole"] as string;

                    if (string.IsNullOrWhiteSpace(email) ||
                        string.IsNullOrWhiteSpace(userName) ||
                        string.IsNullOrWhiteSpace(role))
                    {
                        ClearFirstLoginSession();
                        return RedirectToAction("Login");
                    }

                    ClearFirstLoginSession();

                    // Authenticate automatically after first password change.
                    GenerateJwtToken(userId, email, userName, role);

                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    // Case 2: Password change from the admin panel.
                    userId = user.UserId;

                    changed = _accountService.ChangeUserPassword(
                        userId,
                        model.CurrentPassword,
                        model.NewPassword);

                    if (!changed)
                    {
                        ModelState.AddModelError(
                            "",
                            "Current password is incorrect, or the account is not eligible for this change.");

                        return View(model);
                    }

                    TempData["SuccessMessage"] = "Password changed successfully.";

                    return RedirectToAction("Index", "Admin");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to change the password right now. Please try again.");

                return View(model);
            }
        }

        private void ClearFirstLoginSession()
        {
            Session.Remove("FirstLoginUserId");
            Session.Remove("FirstLoginEmail");
            Session.Remove("FirstLoginUserName");
            Session.Remove("FirstLoginRole");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int userId)
        {
            var loggedInUser = GetLoggedInUserFromToken();

            //if (loggedInUser == null ||
            //    !string.Equals(
            //        loggedInUser.Role,
            //        "Super Admin",
            //        StringComparison.OrdinalIgnoreCase))
            //{
            //    return new HttpStatusCodeResult(403);
            //}

            try
            {
                string defaultPassword =
                    System.Configuration.ConfigurationManager
                        .AppSettings["config:DefaultPassword"];

                if (string.IsNullOrWhiteSpace(defaultPassword))
                {
                    TempData["RegisterMessage"] =
                        "Default password is not configured.";

                    return RedirectToAction("Register");
                }

                bool reset = _accountService.ResetUserPassword(
                    userId, defaultPassword);

                TempData["RegisterMessage"] = reset
                    ? "Password reset successfully. The user must change it at next login."
                    : "Unable to reset the password. Please check the selected user.";

                return RedirectToAction("Register");
            }
            catch (Exception)
            {
                TempData["RegisterMessage"] =
                    "An error occurred while resetting the password.";

                return RedirectToAction("Register");
            }
        }

        #endregion

        #region Log Out
        private readonly ObjectCache _cache = MemoryCache.Default;
        public ActionResult LogOut()
        {

            HttpCookie jwtCookie = Request.Cookies[ConstantVM.WebUserCookie];
            if (jwtCookie != null)
            {
                string cacheKey = "GetUsernameIdUrlToken_" + jwtCookie.Value;
                if (_cache.Contains(cacheKey))
                {
                    _cache.Remove(cacheKey);
                }
            }
            Response.Cookies[ConstantVM.WebUserCookie].Expires = DateTime.Now.AddDays(-1);
            return RedirectToAction("Login", "Account");
        }

        #endregion

        #region Generate JWT Token
        private string GenerateJwtToken(
            int userId,
            string email,
            string userName,
            string userRoles)
        {
            string jwtKey = ConfigurationManager.AppSettings["config:JwtKey"];
            string issuer = ConfigurationManager.AppSettings["config:JwtIssuer"];
            string audience = ConfigurationManager.AppSettings["config:JwtAudience"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new ConfigurationErrorsException(
                    "config:JwtKey must be configured with at least 32 bytes.");
            }

            if (string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                throw new ConfigurationErrorsException(
                    "JWT issuer and audience must be configured.");
            }

            int expiryDays = int.Parse(
                ConfigurationManager.AppSettings["config:JwtExpiryDays"]
                ?? "3");

            int adminExpiryDays = int.Parse(
                ConfigurationManager.AppSettings["config:JwtAdminExpiryDays"]
                ?? "30");

            int tokenDays = string.Equals(
                userRoles, "Admin", StringComparison.OrdinalIgnoreCase)
                ? adminExpiryDays
                : expiryDays;

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Name, userName ?? string.Empty),
        new Claim(ClaimTypes.Email, email ?? string.Empty)
    };

            if (!string.IsNullOrWhiteSpace(userRoles))
            {
                claims.Add(new Claim(ClaimTypes.Role, userRoles));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddDays(tokenDays);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: credentials);

            string tokenString =
                new JwtSecurityTokenHandler().WriteToken(token);

            Session["UName"] = userName;

            Response.Cookies.Add(new HttpCookie(
                ConstantVM.WebUserCookie, tokenString)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Expires = expires,
                Path = "/"
            });

            return tokenString;
        }

        public LoggedInUserVM GetLoggedInUserFromToken()
        {
            var cookie = Request.Cookies[ConstantVM.WebUserCookie];

            if (cookie == null ||
                string.IsNullOrWhiteSpace(cookie.Value))
            {
                return null;
            }

            try
            {
                string jwtKey =
                    ConfigurationManager.AppSettings["config:JwtKey"];

                string issuer =
                    ConfigurationManager.AppSettings["config:JwtIssuer"];

                string audience =
                    ConfigurationManager.AppSettings["config:JwtAudience"];

                if (string.IsNullOrWhiteSpace(jwtKey) ||
                    string.IsNullOrWhiteSpace(issuer) ||
                    string.IsNullOrWhiteSpace(audience))
                {
                    return null;
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var handler = new JwtSecurityTokenHandler();

                ClaimsPrincipal principal = handler.ValidateToken(
                    cookie.Value,
                    validationParameters,
                    out SecurityToken validatedToken);

                // Ensure the token uses the expected signing algorithm.
                if (!(validatedToken is JwtSecurityToken jwtToken) ||
                    jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256)
                {
                    return null;
                }

                var userIdValue = principal
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdValue, out int userId))
                {
                    return null;
                }

                return new LoggedInUserVM
                {
                    UserId = userId,
                    UserName = principal.FindFirst(ClaimTypes.Name)?.Value,
                    Email = principal.FindFirst(ClaimTypes.Email)?.Value,
                    Role = principal.FindFirst(ClaimTypes.Role)?.Value
                };
            }
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        #endregion


        [ChildActionOnly]
        public ActionResult UserProfile()
        {
            var user = GetLoggedInUserFromToken();
            return PartialView("_UserProfile", user);
        }

    }
}