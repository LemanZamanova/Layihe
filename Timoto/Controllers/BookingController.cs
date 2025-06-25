using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using Timoto.DAL;
using Timoto.Models;
using Timoto.Services.Interface;
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
        public IActionResult StartStripeCheckout([FromBody] BookingConfirmVM vm)
        {
            var fullNameParts = vm.FullName?.Split(' ', 2);
            var name = fullNameParts?.FirstOrDefault() ?? "";
            var surname = fullNameParts?.Length > 1 ? fullNameParts[1] : "";

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
                SuccessUrl = "https://localhost:7206/Booking/Success",
                CancelUrl = "https://localhost:7206/Booking/Cancel",

                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
            {
                { "carId", vm.CarId.ToString() },
                { "pickupDate", $"{vm.PickupDate}T{vm.PickupTime}" },
                { "returnDate", $"{vm.CollectionDate}T{vm.CollectionTime}" },
                { "name", name },
                { "surname", surname },
                { "email", vm.Email ?? "" },
                { "phone", vm.Phone ?? "" },
                { "userId", vm.UserId.ToString() },
                { "totalAmount", vm.TotalAmount.ToString("F2", CultureInfo.InvariantCulture) }
            }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Json(new { sessionId = session.Id });
        }



        public async Task<IActionResult> Success()
        {
            var user = await _userManager.GetUserAsync(User);

            if (TempData["BookingData"] == null)
            {
                ViewBag.Message = "Payment was successful, but booking data is missing.";
                return View();
            }

            var bookingVM = JsonConvert.DeserializeObject<BookingVM>((string)TempData["BookingData"]);

            if (!DateTime.TryParse($"{bookingVM.PickupDate} {bookingVM.PickupTime}", out DateTime startDate) ||
                !DateTime.TryParse($"{bookingVM.CollectionDate} {bookingVM.CollectionTime}", out DateTime endDate))
            {
                ViewBag.Message = "Invalid booking dates.";
                return View();
            }


            bool isOverlap = _context.Bookings.Any(b =>
                b.CarId == bookingVM.CarId &&
                !b.IsDeleted &&
                (
                    (startDate >= b.StartDate && startDate < b.EndDate) ||
                    (endDate > b.StartDate && endDate <= b.EndDate) ||
                    (startDate <= b.StartDate && endDate >= b.EndDate)
                ));

            if (!isOverlap)
            {
                var booking = new Booking
                {
                    CarId = bookingVM.CarId,
                    UserId = user.Id,
                    Name = TempData["UserName"].ToString(),
                    Email = TempData["UserEmail"].ToString(),
                    Phone = TempData["UserPhone"].ToString(),
                    StartDate = startDate,
                    EndDate = endDate,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
            }

            ViewBag.Message = "✅ Payment was successful and your booking is confirmed!";
            return View();
        }

        public IActionResult Cancel()
        {
            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        [Route("/stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    "whsec_5TpB0dBXKjCpvzdFNuA4PMjm8szW3HcL"
                );

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntentJson = stripeEvent.Data.Object.ToString();
                    var paymentIntent = JsonConvert.DeserializeObject<PaymentIntent>(paymentIntentJson);


                    if (paymentIntent == null)
                        throw new Exception("PaymentIntent deserialization failed.");

                    var metadata = paymentIntent.Metadata;

                    if (metadata == null ||
                        !metadata.ContainsKey("carId") ||
                        !metadata.ContainsKey("pickupDate") ||
                        !metadata.ContainsKey("collectionDate") ||
                        !metadata.ContainsKey("name") ||
                        !metadata.ContainsKey("email") ||
                        !metadata.ContainsKey("phone") ||
                        !metadata.ContainsKey("userId") ||
                        !metadata.ContainsKey("totalAmount"))
                    {
                        throw new Exception("Required metadata is missing.");
                    }

                    int carId = int.Parse(metadata["carId"]);
                    DateTime pickupDate = DateTime.Parse(metadata["pickupDate"]);
                    DateTime collectionDate = DateTime.Parse(metadata["collectionDate"]);
                    string name = metadata["name"];
                    string surname = metadata.ContainsKey("surname") ? metadata["surname"] : "";
                    string email = metadata["email"];
                    string phone = metadata["phone"];
                    string userId = metadata["userId"];
                    decimal totalAmount = decimal.Parse(metadata["totalAmount"]);

                    bool isOverlap = _context.Bookings.Any(b =>
                        b.CarId == carId && !b.IsDeleted &&
                        ((pickupDate >= b.StartDate && pickupDate < b.EndDate) ||
                         (collectionDate > b.StartDate && collectionDate <= b.EndDate) ||
                         (pickupDate <= b.StartDate && collectionDate >= b.EndDate)));

                    if (!isOverlap)
                    {
                        var booking = new Booking
                        {
                            CarId = carId,
                            UserId = userId,
                            Name = name,
                            Surname = surname,
                            Email = email,
                            Phone = phone,
                            StartDate = pickupDate,
                            EndDate = collectionDate,
                            TotalAmount = totalAmount,
                            StripePaymentIntentId = paymentIntent.Id,
                            PaymentStatus = paymentIntent.Status,
                            CreatedAt = DateTime.Now,
                            IsDeleted = false
                        };

                        _context.Bookings.Add(booking);
                        await _context.SaveChangesAsync();


                        var pdfPath = await _pdfService.GenerateBookingPdfAsync(booking);
                        await _emailService.SendEmailAsync(email, "Booking Confirmed",
                            $"Dear {name},<br/><br/>Your booking from <b>{pickupDate:dd MMM yyyy HH:mm}</b> to <b>{collectionDate:dd MMM yyyy HH:mm}</b> has been successfully confirmed.<br/><br/>See attached PDF receipt.",
                            pdfPath);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stripe webhook processing error: {ex.Message}");
                return BadRequest();
            }
        }





    }
}
