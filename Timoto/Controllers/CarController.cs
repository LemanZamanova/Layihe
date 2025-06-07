using Microsoft.AspNetCore.Mvc;

namespace Timoto.Controllers
{

    public class CarController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
