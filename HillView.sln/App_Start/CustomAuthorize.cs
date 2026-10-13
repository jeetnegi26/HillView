using DTOs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace HillView.sln.App_Start
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var jwtTokenCookie = filterContext.HttpContext.Request.Cookies[ConstantVM.WebUserCookie];
            if (jwtTokenCookie == null)
            {
                var loginUrl = $"/Login";
                filterContext.Result = new RedirectResult(loginUrl);
                return;
            }
            var jwtHandler = new JwtSecurityTokenHandler();
            var token = jwtHandler.ReadToken(jwtTokenCookie.Value) as JwtSecurityToken;
            if (token != null)
            {
                var user = new ClaimsPrincipal(new ClaimsIdentity(token.Claims, "jwt"));
                var currentUrl = filterContext.HttpContext.Request.Url.AbsolutePath;
                var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

                if (user.IsInRole("SuperAdmin"))
                {
                    if (!currentUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        filterContext.Result = new RedirectResult("/Admin/Index");
                    }
                    return;
                }
                else if (user.IsInRole("User"))
                {
                    var loginUrl = $"/";
                    filterContext.Result = new RedirectResult(loginUrl);
                    return;
                }
                //else
                //{
                //    if (IsUserAuthorized(userRoles, currentUrl))
                //    {
                //        return;
                //    }
                //    else
                //    {
                //        filterContext.Result = new RedirectResult("/Unauthorized");
                //        return;
                //    }
                //}
            }
            else
            {
                filterContext.Result = new RedirectResult("/Login");
                return;
            }
        }
        private bool IsUserAuthorized(List<string> userRoles, string currentUrl)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["RtpDb"].ConnectionString;
            bool hasAccess = false;
            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("checkRoleBaseAccess", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    string roles = string.Join(",", userRoles);
                    command.Parameters.AddWithValue("@RoleNames", roles);
                    command.Parameters.AddWithValue("@FormUrl", currentUrl);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            hasAccess = true;
                        }
                    }
                }
            }
            return hasAccess;
        }
    }
}