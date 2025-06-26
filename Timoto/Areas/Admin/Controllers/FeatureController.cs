using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FeatureController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public FeatureController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.Features.Where(f => !f.IsDeleted).ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feature feature)
        {
            if (!ModelState.IsValid) return View(feature);

            _context.Features.Add(feature);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int id)
        {
            var feature = await _context.Features.FindAsync(id);
            if (feature == null) return NotFound();

            return View(feature);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Update(int id, Feature feature)
        {
            var exist = await _context.Features.FindAsync(id);
            if (exist == null) return NotFound();

            if (!ModelState.IsValid) return View(feature);

            exist.Name = feature.Name;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Delete(int id)
        {
            var feature = await _context.Features.FindAsync(id);
            if (feature == null) return NotFound();

            feature.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
