
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VehicleRentalSystem.Controllers
{
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(
            ActionExecutingContext context)
        {
            var isAdmin = context.HttpContext.Session
                .GetString("IsAdmin");

            if (isAdmin != "true")
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Admin",
                    null
                );

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
