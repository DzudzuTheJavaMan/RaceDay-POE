using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters;

public class RoleRequiredAttribute : ActionFilterAttribute
{
    private readonly string _role;

    public RoleRequiredAttribute(string role)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var role = context.HttpContext.Session.GetString("Role");

        if (userId == null)
            context.Result = new RedirectToActionResult("Login", "Account", null);
        else if (role != _role)
            context.Result = new RedirectToActionResult("Index", "Home", null);
    }
}