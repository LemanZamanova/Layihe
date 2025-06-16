using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;
using Timoto.Services.Interface;
using Timoto.ViewModels;

namespace Timoto.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public BookingController(AppDbContext context, UserManager<AppUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Confirm(BookingVM model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!_context.Cars.Any(c => c.Id == model.CarId))
                return RedirectToAction("Index", "Car");

            if (!DateTime.TryParse($"{model.PickupDate} {model.PickupTime}", out DateTime start) ||
                !DateTime.TryParse($"{model.CollectionDate} {model.CollectionTime}", out DateTime end))
            {
                ModelState.AddModelError(string.Empty, "Invalid date or time format.");
                return await ReturnDetailView(model.CarId);
            }

            if ((start - DateTime.Now).TotalDays > 30)
            {
                ModelState.AddModelError(string.Empty, "You cannot book a car for dates too far in the future.");
                return await ReturnDetailView(model.CarId);
            }

            if (start < DateTime.Now || end < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "You cannot book a car for past dates.");
                return await ReturnDetailView(model.CarId);
            }

            if (end <= start)
            {
                ModelState.AddModelError(string.Empty, "Return date must be after pickup date.");
                return await ReturnDetailView(model.CarId);
            }


            if ((end - start).TotalHours < 24)
            {
                ModelState.AddModelError(string.Empty, "Minimum booking duration must be at least 24 hours.");
                return await ReturnDetailView(model.CarId);
            }

            bool isOverlap = _context.Bookings.Any(b =>
                b.CarId == model.CarId &&
                ((start >= b.StartDate && start < b.EndDate) ||
                 (end > b.StartDate && end <= b.EndDate) ||
                 (start <= b.StartDate && end >= b.EndDate)));

            if (isOverlap)
            {
                ModelState.AddModelError(string.Empty, "This car is already booked for the selected time range.");
                return await ReturnDetailView(model.CarId);
            }

            var cards = await _context.UserCards
                .Where(c => c.UserId == user.Id && !c.IsDeleted)
                .Select(c => new SelectListItem
                {
                    Text = $"{c.CardHolderName} - **** **** **** {c.CardNumber.Substring(c.CardNumber.Length - 4)}",
                    Value = c.Id.ToString()
                }).ToListAsync();

            if (!cards.Any())
            {
                ModelState.AddModelError(string.Empty, "You have not added any cards yet. Please add a card to proceed.");
            }

            var vm = new BookingConfirmVM
            {
                BookingVM = model,
                FullName = $"{user.Name} {user.Surname}",
                Email = user.Email,
                Phone = user.Phone,
                UserCards = cards
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(BookingConfirmVM vm)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!DateTime.TryParse($"{vm.BookingVM.PickupDate} {vm.BookingVM.PickupTime}", out DateTime startDate) ||
                !DateTime.TryParse($"{vm.BookingVM.CollectionDate} {vm.BookingVM.CollectionTime}", out DateTime endDate))
            {
                ModelState.AddModelError(string.Empty, "Invalid date or time format.");
                return RedirectToAction("Confirm", "Booking", vm);
            }

            var booking = new Booking
            {
                CarId = vm.BookingVM.CarId,
                UserId = user.Id,
                Name = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                StartDate = startDate,
                EndDate = endDate,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(booking.Email, "Booking Confirmed",
                $"Your booking from {startDate} to {endDate} has been successfully confirmed.");

            return RedirectToAction("Detail", "Car", new { id = booking.CarId });
        }


        private async Task<IActionResult> ReturnDetailView(int carId)
        {
            var car = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.CarFeatures).ThenInclude(cf => cf.Feature)
                .Include(c => c.BodyType)
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.DriveType)
                .FirstOrDefaultAsync(c => c.Id == carId);

            if (car == null) return RedirectToAction("Index", "Car");

            var vm = new DetailVM
            {
                Cars = car
            };

            return View("~/Views/Car/Detail.cshtml", vm);
        }
    }
}
