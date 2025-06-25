using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TransmissionTypesController : Controller
    {
        private readonly AppDbContext _context;

        public TransmissionTypesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var transmissionTypes = await _context.TransmissionTypes
                .Where(t => !t.IsDeleted)
                .Include(t => t.Cars)
                .AsNoTracking()
                .ToListAsync();

            return View(transmissionTypes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransmissionType transmissionType)
        {
            if (!ModelState.IsValid) return View(transmissionType);

            bool exists = await _context.TransmissionTypes
                .AnyAsync(t => t.Name.ToLower().Trim() == transmissionType.Name.ToLower().Trim() && !t.IsDeleted);

            if (exists)
            {
                ModelState.AddModelError(nameof(TransmissionType.Name), $"{transmissionType.Name} already exists");
                return View(transmissionType);
            }

            transmissionType.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.TransmissionTypes.AddAsync(transmissionType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var transmissionType = await _context.TransmissionTypes.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (transmissionType is null) return NotFound();

            return View(transmissionType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, TransmissionType transmissionType)
        {
            if (id is null || id <= 0) return BadRequest();
            if (!ModelState.IsValid) return View(transmissionType);

            var existed = await _context.TransmissionTypes.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (existed is null) return NotFound();

            bool duplicate = await _context.TransmissionTypes
                .AnyAsync(t => t.Id != id && t.Name.ToLower().Trim() == transmissionType.Name.ToLower().Trim() && !t.IsDeleted);

            if (duplicate)
            {
                ModelState.AddModelError(nameof(TransmissionType.Name), $"{transmissionType.Name} already exists");
                return View(transmissionType);
            }

            existed.Name = transmissionType.Name;
            existed.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var transmissionType = await _context.TransmissionTypes.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (transmissionType is null) return NotFound();

            _context.TransmissionTypes.Remove(transmissionType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
