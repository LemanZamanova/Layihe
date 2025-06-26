using Microsoft.AspNetCore.Mvc;

public class ErrorController : Controller
{
    [Route("Error/AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
