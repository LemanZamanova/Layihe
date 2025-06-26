using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VehicleTypesController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public VehicleTypesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vehicleTypes = await _context.VehicleTypes
                .Where(v => !v.IsDeleted)
                .Include(v => v.Cars)
                .AsNoTracking()
                .ToListAsync();

            return View(vehicleTypes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VehicleType vehicleType)
        {
            if (!ModelState.IsValid) return View(vehicleType);

            bool exists = await _context.VehicleTypes
                .AnyAsync(v => v.Name.ToLower().Trim() == vehicleType.Name.ToLower().Trim() && !v.IsDeleted);

            if (exists)
            {
                ModelState.AddModelError(nameof(VehicleType.Name), $"{vehicleType.Name} already exists");
                return View(vehicleType);
            }

            vehicleType.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.VehicleTypes.AddAsync(vehicleType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vehicleType is null) return NotFound();

            return View(vehicleType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Update(int? id, VehicleType vehicleType)
        {
            if (id is null || id <= 0) return BadRequest();
            if (!ModelState.IsValid) return View(vehicleType);

            var existed = await _context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (existed is null) return NotFound();

            bool duplicate = await _context.VehicleTypes
                .AnyAsync(v => v.Id != id && v.Name.ToLower().Trim() == vehicleType.Name.ToLower().Trim() && !v.IsDeleted);

            if (duplicate)
            {
                ModelState.AddModelError(nameof(VehicleType.Name), $"{vehicleType.Name} already exists");
                return View(vehicleType);
            }

            existed.Name = vehicleType.Name;
            existed.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vehicleType is null) return NotFound();

            _context.VehicleTypes.Remove(vehicleType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
