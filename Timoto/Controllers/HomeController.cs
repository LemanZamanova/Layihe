using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;
using Timoto.ViewModels;

namespace Timoto.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars
                .Include(c => c.BodyType)
                .Include(c => c.CarImages)
                .OrderByDescending(c => c.LikeCount)
                .Take(6)
                .ToListAsync();

            var user = await _userManager.GetUserAsync(User);
            List<int> favoriteCarIds = new();

            if (user != null)
            {
                favoriteCarIds = await _context.FavoriteCars
                    .Where(f => f.UserId == user.Id)
                    .Select(f => f.CarId)
                    .ToListAsync();
            }

            var vm = new HomeVM
            {
                Cars = cars,
                FavoriteCarIds = favoriteCarIds
            };

            return View(vm);
        }
    }
}
