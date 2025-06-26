using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FuelTypesController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public FuelTypesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var fuelTypes = await _context.FuelTypes
                .Where(f => !f.IsDeleted)
                .Include(f => f.Cars)
                .AsNoTracking()
                .ToListAsync();

            return View(fuelTypes);
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FuelType fuelType)
        {
            if (!ModelState.IsValid) return View(fuelType);

            bool exists = await _context.FuelTypes
                .AnyAsync(f => f.Name.ToLower().Trim() == fuelType.Name.ToLower().Trim() && !f.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError(nameof(FuelType.Name), $"{fuelType.Name} already exists");
                return View(fuelType);
            }

            fuelType.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.FuelTypes.AddAsync(fuelType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Update(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var fuelType = await _context.FuelTypes.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (fuelType is null) return NotFound();

            return View(fuelType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Update(int? id, FuelType fuelType)
        {
            if (id is null || id <= 0) return BadRequest();
            if (!ModelState.IsValid) return View(fuelType);

            var existed = await _context.FuelTypes.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (existed is null) return NotFound();

            bool duplicate = await _context.FuelTypes
                .AnyAsync(f => f.Id != id && f.Name.ToLower().Trim() == fuelType.Name.ToLower().Trim() && !f.IsDeleted);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(FuelType.Name), $"{fuelType.Name} already exists");
                return View(fuelType);
            }

            existed.Name = fuelType.Name;
            existed.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var fuelType = await _context.FuelTypes.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (fuelType is null) return NotFound();




            _context.FuelTypes.Remove(fuelType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
