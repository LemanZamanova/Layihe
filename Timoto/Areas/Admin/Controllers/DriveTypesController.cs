using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DriveTypesController : Controller
    {
        private readonly AppDbContext _context;

        public DriveTypesController(AppDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var driveTypes = await _context.DriveTypes
                .Where(f => !f.IsDeleted)
                .Include(f => f.Cars)
                .AsNoTracking()
                .ToListAsync();

            return View(driveTypes);
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Timoto.Models.DriveType driveType)
        {
            if (!ModelState.IsValid) return View(driveType);

            bool exists = await _context.DriveTypes
                .AnyAsync(d => d.Name.ToLower().Trim() == driveType.Name.ToLower().Trim() && !d.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError(nameof(FuelType.Name), $"{driveType.Name} already exists");
                return View(driveType);
            }

            driveType.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.DriveTypes.AddAsync(driveType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Update(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var driveType = await _context.DriveTypes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (driveType is null) return NotFound();

            return View(driveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Timoto.Models.DriveType driveType)
        {
            if (id is null || id <= 0) return BadRequest();
            if (!ModelState.IsValid) return View(driveType);

            var existed = await _context.DriveTypes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
            if (existed is null) return NotFound();

            bool duplicate = await _context.DriveTypes
                .AnyAsync(f => f.Id != id && f.Name.ToLower().Trim() == driveType.Name.ToLower().Trim() && !f.IsDeleted);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(FuelType.Name), $"{driveType.Name} already exists");
                return View(driveType);
            }

            existed.Name = driveType.Name;
            existed.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var driveType = await _context.DriveTypes.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
            if (driveType is null) return NotFound();




            _context.DriveTypes.Remove(driveType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }







    }
}
