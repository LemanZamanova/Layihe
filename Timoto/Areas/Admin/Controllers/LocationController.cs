using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LocationController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public LocationController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var locations = await _context.Locations
                .Where(l => !l.IsDeleted)
                .Include(l => l.Cars)
                .AsNoTracking()
                .ToListAsync();

            return View(locations);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Location location)
        {
            if (!ModelState.IsValid) return View(location);

            bool exists = await _context.Locations
                .AnyAsync(l => l.Name.ToLower().Trim() == location.Name.ToLower().Trim() && !l.IsDeleted);

            if (exists)
            {
                ModelState.AddModelError(nameof(location.Name), $"{location.Name} already exists");
                return View(location);
            }

            location.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (location is null) return NotFound();

            return View(location);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]
        public async Task<IActionResult> Update(int? id, Location location)
        {
            if (id is null || id <= 0) return BadRequest();
            if (!ModelState.IsValid) return View(location);

            var existed = await _context.Locations.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (existed is null) return NotFound();

            bool duplicate = await _context.Locations
                .AnyAsync(l => l.Id != id && l.Name.ToLower().Trim() == location.Name.ToLower().Trim() && !l.IsDeleted);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(location.Name), $"{location.Name} already exists");
                return View(location);
            }

            existed.Name = location.Name;
            existed.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize("Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (location is null) return NotFound();

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }




    }

}

