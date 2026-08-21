using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

public class BaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Donor";
        base.OnActionExecuting(context);
    }
}
