using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.Areas.ViewModels;
using Timoto.DAL;


namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.User)
                .Where(b => !b.IsDeleted)
                .ToListAsync();

            return View(bookings);
        }


        public async Task<IActionResult> Detail(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }
        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Update(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, UpdateBookingVM updatedBooking)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = updatedBooking.Status;
            booking.StartDate = updatedBooking.StartDate;
            booking.EndDate = updatedBooking.EndDate;
            booking.TotalAmount = updatedBooking.TotalAmount;
            booking.LatePenaltyAmount = updatedBooking.LatePenaltyAmount;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [RoleAuthorize("Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
