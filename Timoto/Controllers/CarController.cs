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

    public class CarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public CarController(AppDbContext context, UserManager<AppUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }
        public async Task<IActionResult> Index(CarFilterVM filter)
        {
            var query = _context.Cars
                .Include(c => c.BodyType)
                 .Include(c => c.VehicleType)
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.DriveType)
                 .Include(c => c.Bookings)
                .Include(c => c.CarFeatures)
                .ThenInclude(cf => cf.Feature)
                .Include(c => c.CarImages)
                .AsQueryable();

            if (filter.SelectedVehicleTypeIds.Any())
                query = query.Where(c => filter.SelectedVehicleTypeIds.Contains(c.VehicleTypeId));

            if (filter.SelectedBodyTypeIds != null && filter.SelectedBodyTypeIds.Any())
                query = query.Where(c => filter.SelectedBodyTypeIds.Contains(c.BodyTypeId));

            if (filter.SelectedSeatCounts != null && filter.SelectedSeatCounts.Any())
                query = query.Where(c => filter.SelectedSeatCounts.Contains(c.Seats));

            if (filter.MinPrice.HasValue)
                query = query.Where(c => c.DailyPrice >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(c => c.DailyPrice <= filter.MaxPrice.Value);

            if (filter.SelectedEngineRanges != null && filter.SelectedEngineRanges.Any())
            {
                foreach (var range in filter.SelectedEngineRanges)
                {
                    var parts = range.Split('-');
                    if (int.TryParse(parts[0], out int min))
                    {
                        int max = parts[1] == "max" ? int.MaxValue : int.Parse(parts[1]);
                        query = query.Where(c => c.EngineSize >= min && c.EngineSize <= max);
                    }
                }
            }

            // ViewBag-lə filtrdə lazım olanlar
            ViewBag.BodyTypes = _context.BodyTypes.ToList();
            ViewBag.VehicleTypes = _context.VehicleTypes.ToList();
            ViewBag.SeatOptions = _context.Cars
                 .Select(c => c.Seats)
                 .Distinct()
                 .OrderBy(s => s)
                 .ToList();
            ViewBag.EngineRanges = new List<(int Min, int Max)>
                    {
                       (1000, 2000),
                       (2001, 4000),
                       (4001, 6000),
                       (6001, int.MaxValue)
                     };

            var currentUser = await _userManager.GetUserAsync(User);
            var favoriteCarIds = new List<int>();
            if (currentUser != null)
            {
                favoriteCarIds = await _context.FavoriteCars
                .Where(f => f.UserId == currentUser.Id)
                .Select(f => f.CarId)
                .ToListAsync();
            }


            var vm = new CarFilterVM
            {
                Cars = query.ToList(),
                SelectedBodyTypeIds = filter.SelectedBodyTypeIds ?? new List<int>(),
                SelectedVehicleTypeIds = filter.SelectedVehicleTypeIds ?? new List<int>(),
                SelectedSeatCounts = filter.SelectedSeatCounts ?? new List<int>(),
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,

                FavoriteCarIds = favoriteCarIds
            };

            return View(vm);
        }


        public async Task<IActionResult> Add()
        {

            CarFormVM carFormVM = new CarFormVM
            {
                FuelTypes = _context.FuelTypes.Select(f => new SelectListItem { Value = f.Id.ToString(), Text = f.Name }),
                TransmissionTypes = _context.TransmissionTypes.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name }),
                DriveTypes = _context.DriveTypes.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }),
                BodyTypes = _context.BodyTypes.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name }),
                AllFeatures = _context.Features.ToList()
            };


            return View(carFormVM);
        }
        [HttpPost]
        public IActionResult Like(int id)
        {
            var car = _context.Cars.FirstOrDefault(c => c.Id == id);
            if (car == null)
                return NotFound();

            car.LikeCount++;
            _context.SaveChanges();

            return RedirectToAction("Index");
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavoriteAjax(int carId)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null)
                return Json(new { success = false });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, requiresLogin = true });
            }

            bool isFavorite = false;

            var existing = await _context.FavoriteCars
                .FirstOrDefaultAsync(f => f.CarId == carId && f.UserId == user.Id);

            if (existing != null)
            {
                _context.FavoriteCars.Remove(existing);
                car.LikeCount = Math.Max(0, car.LikeCount - 1);
            }
            else
            {
                _context.FavoriteCars.Add(new FavoriteCar { CarId = carId, UserId = user.Id });
                car.LikeCount++;
                isFavorite = true;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isFavorite,
                likeCount = car.LikeCount
            });
        }





        public async Task<IActionResult> Detail(int id)
        {
            var car = await _context.Cars
                .Include(c => c.BodyType)
                .Include(c => c.VehicleType)
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.DriveType)
                .Include(c => c.CarFeatures)
                .ThenInclude(cf => cf.Feature)
                .Include(c => c.CarImages)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null) return NotFound();
            var vm = new DetailVM
            {
                Cars = car,
                BookingVM = new BookingVM
                {
                    CarId = car.Id
                }

            };
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookingVM model)
        {
            if (!ModelState.IsValid)
            {
                return await ReturnDetailViewWithErrors(model.CarId);
            }

            DateTime startDate;
            DateTime endDate;
            try
            {
                startDate = DateTime.Parse($"{model.PickupDate} {model.PickupTime}");
                endDate = DateTime.Parse($"{model.CollectionDate} {model.CollectionTime}");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "The date and time are not in a valid format.");
                return await ReturnDetailViewWithErrors(model.CarId);
            }

            if ((startDate - DateTime.Now).TotalDays > 30)
            {
                ModelState.AddModelError(string.Empty, "Booking is not allowed for dates that are too far in the future.");
                return await ReturnDetailViewWithErrors(model.CarId);
            }

            bool isOverlap = _context.Bookings.Any(b =>
                b.CarId == model.CarId &&
                ((startDate >= b.StartDate && startDate < b.EndDate) ||
                 (endDate > b.StartDate && endDate <= b.EndDate) ||
                 (startDate <= b.StartDate && endDate >= b.EndDate)));

            if (isOverlap)
            {
                ModelState.AddModelError(string.Empty, "This car has already been booked for these dates.");
                return await ReturnDetailViewWithErrors(model.CarId);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var confirmModel = new BookingConfirmVM
            {
                BookingVM = model,
                FullName = $"{user.Name} {user.Surname}",
                Email = user.Email,
                Phone = user.Phone
            };

            return RedirectToAction("Confirm", "Booking", model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeBooking(BookingConfirmVM model)
        {
            if (!ModelState.IsValid)
                return View("Book", model);

            var user = await _userManager.GetUserAsync(User);

            var booking = new Booking
            {
                CarId = model.BookingVM.CarId,
                StartDate = DateTime.Parse($"{model.BookingVM.PickupDate} {model.BookingVM.PickupTime}"),
                EndDate = DateTime.Parse($"{model.BookingVM.CollectionDate} {model.BookingVM.CollectionTime}"),
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                Name = model.FullName.Split(' ')[0],
                Surname = model.FullName.Split(' ').Length > 1 ? model.FullName.Split(' ')[1] : "",
                Email = model.Email,
                Phone = model.Phone,
                UserId = user?.Id
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();


            //  refresh zamanı yenidən POST olmasın
            return RedirectToAction("Success");
        }
        public IActionResult Success()
        {
            return View(); // booking success page
        }
        private async Task<IActionResult> ReturnDetailViewWithErrors(int carId)
        {
            var car = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.BodyType)
                .Include(c => c.VehicleType)
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.DriveType)
                .Include(c => c.CarFeatures)
                .ThenInclude(cf => cf.Feature)
                .FirstOrDefaultAsync(c => c.Id == carId);

            if (car == null)
                return NotFound();

            var detailVM = new DetailVM
            {
                Cars = car
            };

            return View("Detail", detailVM);
        }



        public async Task<IActionResult> LoadMoreCars(int skip = 0, int take = 6)
        {
            var cars = await _context.Cars
                .Where(c => !c.IsDeleted)
                .Include(c => c.CarImages)
                .Include(c => c.Bookings)
                .OrderByDescending(c => c.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var result = cars.Select(c => new
            {
                c.Id,
                c.Name,
                DailyPrice = c.DailyPrice,
                LikeCount = c.LikeCount,
                ImageUrl = c.CarImages.FirstOrDefault(ci => ci.IsMain)?.ImageUrl,
                Seats = c.Seats,
                Doors = c.Doors,
                LuggageVolume = c.LuggageVolume,
                BodyTypeName = c.BodyType?.Name,
                IsReserved = c.Bookings.Any(b =>
                    !b.IsDeleted &&
                    DateTime.Now >= b.StartDate &&
                    DateTime.Now <= b.EndDate)
            }).ToList();

            return Json(result);
        }




    }
}
