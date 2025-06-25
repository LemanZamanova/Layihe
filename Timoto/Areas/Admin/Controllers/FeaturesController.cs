
using Microsoft.AspNetCore.Mvc;

namespace Timoto.Areas.Admin.Controllers
{
    public class FeaturesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
