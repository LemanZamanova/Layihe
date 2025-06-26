using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BodyTypesController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public BodyTypesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var bodyTypes = await _context.BodyTypes
                .Where(b => !b.IsDeleted)
                .Include(b => b.Cars)
                .AsNoTracking()
                .ToListAsync();

            return View(bodyTypes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BodyType bodyType)
        {
            if (!ModelState.IsValid) return View(bodyType);

            bool exists = await _context.BodyTypes
                .AnyAsync(b => b.Name.ToLower().Trim() == bodyType.Name.ToLower().Trim() && !b.IsDeleted);

            if (exists)
            {
                ModelState.AddModelError(nameof(BodyType.Name), $"{bodyType.Name} already exists");
                return View(bodyType);
            }

            bodyType.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.BodyTypes.AddAsync(bodyType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var bodyType = await _context.BodyTypes.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (bodyType is null) return NotFound();

            return View(bodyType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Update(int? id, BodyType bodyType)
        {
            if (id is null || id <= 0) return BadRequest();
            if (!ModelState.IsValid) return View(bodyType);

            var existed = await _context.BodyTypes.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (existed is null) return NotFound();

            bool duplicate = await _context.BodyTypes
                .AnyAsync(b => b.Id != id && b.Name.ToLower().Trim() == bodyType.Name.ToLower().Trim() && !b.IsDeleted);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(BodyType.Name), $"{bodyType.Name} already exists");
                return View(bodyType);
            }

            existed.Name = bodyType.Name;
            existed.CreatedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null || id <= 0) return BadRequest();

            var bodyType = await _context.BodyTypes.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (bodyType is null) return NotFound();

            _context.BodyTypes.Remove(bodyType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
