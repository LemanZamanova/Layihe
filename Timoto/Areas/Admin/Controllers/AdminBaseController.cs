using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isAuthenticated = context.HttpContext.Session.GetString("IsAdminAuthenticated");
            var userRole = context.HttpContext.Session.GetString("AdminRole");

            if (isAuthenticated != "true" || string.IsNullOrWhiteSpace(userRole))
            {
                context.Result = new RedirectToActionResult("Login", "Home", new { area = "Admin" });
                return;
            }

            ViewBag.AdminRole = userRole;
            base.OnActionExecuting(context);
        }
    }
    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var sessionRole = context.HttpContext.Session.GetString("AdminRole");

            if (string.IsNullOrEmpty(sessionRole) || !_roles.Contains(sessionRole))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Error", new { area = "" });
            }
        }
    }


}
