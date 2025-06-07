using Microsoft.AspNetCore.Mvc;

namespace Timoto.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
