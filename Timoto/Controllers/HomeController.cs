using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.ViewModels;

namespace Timoto.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cars = _context.Cars
                .Include(c => c.BodyType)
                .OrderByDescending(c => c.LikeCount)
                .Take(6)
                .ToList();

            var vm = new HomeVM { Cars = cars };
            return View(vm);
        }
    }
}
