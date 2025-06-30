using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using Timoto.DAL;
using Timoto.Models;
using Timoto.Services.Interface;
using Timoto.Utilities.Enums;
using Timoto.ViewModels;


namespace Timoto.Controllers
{
    //[Authorize]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IPdfService _pdfService;

        public BookingController(AppDbContext context, UserManager<AppUser> userManager, IEmailService emailService, IPdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _pdfService = pdfService;
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

            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == model.CarId);
            if (car == null) return RedirectToAction("Index", "Car");

            int totalDays = (int)Math.Ceiling((end - start).TotalDays);
            decimal totalAmount = car.DailyPrice * totalDays;
            model.TotalAmount = totalAmount;

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
                UserCards = cards,
                BookingSummary = $"From {model.PickupDate} {model.PickupTime} to {model.CollectionDate} {model.CollectionTime} — {totalDays} day(s), ${totalAmount}"
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
                $"Your booking from {startDate:dd MMM yyyy HH:mm} to {endDate:dd MMM yyyy HH:mm} has been successfully confirmed.\nTotal Amount: ${vm.BookingVM.TotalAmount}");

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
        [HttpPost]
        public async Task<IActionResult> StartStripeCheckout([FromBody] BookingConfirmVM vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(); // əgər login olmayıbsa

            DateTime pickupDateTime = DateTime.Parse($"{vm.PickupDate} {vm.PickupTime}");
            DateTime collectionDateTime = DateTime.Parse($"{vm.CollectionDate} {vm.CollectionTime}");

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
        {
            new()
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = (long)(vm.TotalAmount * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Car Booking"
                    }
                },
                Quantity = 1
            }
        },
                SuccessUrl = "https://localhost:7206/Booking/Success?sessionId={CHECKOUT_SESSION_ID}",
                CancelUrl = "https://localhost:7206/Booking/Cancel",
                Metadata = new Dictionary<string, string>
        {
            { "carId", vm.CarId.ToString() },
            { "pickupDate", pickupDateTime.ToString("o") },
            { "collectionDate", collectionDateTime.ToString("o") },
            { "name", vm.FullName?.Split(" ")[0] ?? user.Name },
            { "surname", vm.FullName?.Split(" ").Skip(1).FirstOrDefault() ?? user.Surname },
            { "email", vm.Email ?? user.Email },
            { "phone", vm.Phone ?? user.Phone },
            { "userId", user.Id }, // ✅ user-in ID-si mütləq verilir
            { "totalAmount", vm.TotalAmount.ToString("F2", CultureInfo.InvariantCulture) }
        }
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Json(new { sessionId = session.Id });
        }


        public async Task<IActionResult> Success(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                ViewBag.Message = "Session ID not found.";
                return View();
            }

            try
            {
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(sessionId);

                if (session.PaymentStatus != "paid")
                {
                    ViewBag.Message = "Payment was not completed.";
                    return View();
                }

                var intent = session.PaymentIntentId;
                var metadata = session.PaymentIntent?.Metadata ?? session.Metadata;

                int carId = int.Parse(metadata["carId"]);
                DateTime pickupDate = DateTime.Parse(metadata["pickupDate"], CultureInfo.InvariantCulture);
                DateTime collectionDate = DateTime.Parse(metadata["collectionDate"], CultureInfo.InvariantCulture);

                string name = metadata["name"];
                string surname = metadata["surname"];
                string email = metadata["email"];
                string phone = metadata["phone"];
                string userId = metadata["userId"];
                decimal totalAmount = decimal.Parse(metadata["totalAmount"], CultureInfo.InvariantCulture);

                var booking = await _context.Bookings
                    .Include(b => b.Car)
                    .FirstOrDefaultAsync(b => b.StripePaymentIntentId == intent);

                if (booking == null)
                {
                    bool isOverlap = await _context.Bookings.AnyAsync(b =>
                        b.CarId == carId && !b.IsDeleted &&
                        ((pickupDate >= b.StartDate && pickupDate < b.EndDate) ||
                         (collectionDate > b.StartDate && collectionDate <= b.EndDate) ||
                         (pickupDate <= b.StartDate && collectionDate >= b.EndDate)));

                    if (!isOverlap)
                    {
                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone))
                        {
                            ViewBag.Message = "Invalid user data: name, email or phone is missing.";
                            return View();
                        }

                        booking = new Booking
                        {
                            CarId = carId,
                            UserId = userId,
                            Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name,
                            Surname = string.IsNullOrWhiteSpace(surname) ? "" : surname,
                            Email = string.IsNullOrWhiteSpace(email) ? "no-email@domain.com" : email,
                            Phone = string.IsNullOrWhiteSpace(phone) ? "0000000000" : phone,
                            StartDate = pickupDate,
                            EndDate = collectionDate,
                            TotalAmount = totalAmount,
                            StripePaymentIntentId = string.IsNullOrWhiteSpace(intent) ? Guid.NewGuid().ToString() : intent,
                            PaymentStatus = string.IsNullOrWhiteSpace(session.PaymentStatus) ? "unknown" : session.PaymentStatus,
                            Status = BookingStatus.Scheduled,
                            CreatedAt = DateTime.Now,
                            LatePenaltyAmount = 0m,

                        };

                        _context.Bookings.Add(booking);
                        await _context.SaveChangesAsync();

                        booking.Car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == carId);

                        var pdfPath = await _pdfService.GenerateBookingPdfAsync(booking);
                        await _emailService.SendEmailAsync(email, "Booking Confirmed",
                            $"Dear {name},<br/><br/>Your booking from <b>{pickupDate:dd MMM yyyy HH:mm}</b> to <b>{collectionDate:dd MMM yyyy HH:mm}</b> has been successfully confirmed.<br/><br/>PDF receipt is attached.",
                            pdfPath);
                    }
                    else
                    {
                        ViewBag.Message = "This car is already booked for the selected date range.";
                        return View();
                    }
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occurred while confirming your booking.";

                var fullError = ex.Message;
                if (ex.InnerException != null)
                    fullError += " | Inner: " + ex.InnerException.Message;

                if (ex.InnerException?.InnerException != null)
                    fullError += " | Deep: " + ex.InnerException.InnerException.Message;

                ViewBag.Error = fullError;

                return View();
            }
        }



        public IActionResult Cancel()
        {
            TempData["Warning"] = "Payment was cancelled.";
            return RedirectToAction("Index", "Home");
        }




    }
}
