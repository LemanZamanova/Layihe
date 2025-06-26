using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.ViewModels;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FavoriteCarController : Controller
    {
        private readonly AppDbContext _context;

        public FavoriteCarController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var favorites = await _context.FavoriteCars
                .Include(fc => fc.Car)
                .Include(fc => fc.User)
                .ToListAsync();

            var grouped = favorites
                .GroupBy(f => f.Car)
                .Select(g => new FavoriteCarGroupVM
                {
                    Car = g.Key,
                    TotalLikes = g.Count(),
                    Users = g.Select(f => f.User).ToList()
                })
                .ToList();

            return View(grouped);
        }

    }
}
