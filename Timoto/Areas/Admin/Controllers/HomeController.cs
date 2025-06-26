using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.Areas.ViewModels;
using Timoto.DAL;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public HomeController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("IsAdminAuthenticated") != "true")
                return RedirectToAction("Login");
            ViewBag.TotalCars = await _context.Cars.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalFavorites = await _context.FavoriteCars.CountAsync();

            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(AdminLoginVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            string adminUser = _config["AdminCredentials:Username"];
            string adminPass = _config["AdminCredentials:Password"];
            string moderatorUser = _config["ModeratorCredentials:Username"];
            string moderatorPass = _config["ModeratorCredentials:Password"];

            if (vm.Username == adminUser && vm.Password == adminPass)
            {
                HttpContext.Session.SetString("IsAdminAuthenticated", "true");
                HttpContext.Session.SetString("AdminRole", "Admin");
                return RedirectToAction("Index");
            }
            else if (vm.Username == moderatorUser && vm.Password == moderatorPass)
            {
                HttpContext.Session.SetString("IsAdminAuthenticated", "true");
                HttpContext.Session.SetString("AdminRole", "Moderator");
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Invalid credentials");
            return View(vm);
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsAdminAuthenticated");
            return RedirectToAction("Login");
        }
    }
}
