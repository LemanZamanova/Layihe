using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;
using Timoto.Services.Interface;
using Timoto.Utilities.Enums;
using Timoto.Utilities.Extensions;
using Timoto.ViewModels;

namespace Timoto.Controllers
{

    public class CarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public CarController(AppDbContext context, UserManager<AppUser> userManager, IEmailService emailService, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _env = env;
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


        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var vm = new CarFormVM
            {
                FuelTypes = await _context.FuelTypes.Select(f => new SelectListItem { Value = f.Id.ToString(), Text = f.Name }).ToListAsync(),
                TransmissionTypes = await _context.TransmissionTypes.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name }).ToListAsync(),
                DriveTypes = await _context.DriveTypes.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToListAsync(),
                BodyTypes = await _context.BodyTypes.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name }).ToListAsync(),
                Locations = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync(),
                AllFeatures = await _context.Features.ToListAsync(),
                VehicleTypes = await _context.VehicleTypes.Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Name }).ToListAsync(),
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CarFormVM vm, IFormFile MainImage, List<IFormFile> OtherImages)
        {
            ModelState.Clear();
            TempData["Success"] = "Your car listing has been submitted and is awaiting moderator approval.";
            vm.FuelTypes = await _context.FuelTypes.Select(f => new SelectListItem { Value = f.Id.ToString(), Text = f.Name }).ToListAsync();
            vm.TransmissionTypes = await _context.TransmissionTypes.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name }).ToListAsync();
            vm.DriveTypes = await _context.DriveTypes.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToListAsync();
            vm.BodyTypes = await _context.BodyTypes.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name }).ToListAsync();
            vm.AllFeatures = await _context.Features.ToListAsync();
            vm.Locations = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
            vm.VehicleTypes = await _context.VehicleTypes.Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Name }).ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (MainImage == null || !MainImage.ValidateType("image/") || !MainImage.ValidateSize(FileSize.MB, 5))
            {
                ModelState.AddModelError("MainImage", "Main image is required and must be a valid image under 5MB.");
                return View(vm);
            }

            var newCar = new Car
            {
                Name = vm.Car.Name,
                Year = vm.Car.Year,
                DailyPrice = vm.Car.DailyPrice,
                Description = vm.Car.Description,
                FuelTypeId = vm.Car.FuelTypeId,
                TransmissionTypeId = vm.Car.TransmissionTypeId,
                DriveTypeId = vm.Car.DriveTypeId,
                BodyTypeId = vm.Car.BodyTypeId,
                VehicleTypeId = vm.Car.VehicleTypeId,
                LocationId = vm.LocationId,
                Location = vm.Car.Location,
                Latitude = vm.Car.Latitude,
                Longitude = vm.Car.Longitude,
                Seats = vm.Car.Seats,
                Doors = vm.Car.Doors,
                LuggageVolume = vm.Car.LuggageVolume,
                EngineSize = vm.Car.EngineSize,
                FuelEconomy = vm.Car.FuelEconomy,
                ExteriorColor = vm.Car.ExteriorColor,
                InteriorColor = vm.Car.InteriorColor,
                CreatedAt = DateTime.UtcNow.AddHours(4),
                UserId = user.Id,
                Status = CarStatus.Pending,
                IsDeleted = false,
                CarImages = new List<CarImage>(),
                CarFeatures = new List<CarFeature>()
            };

            // Save main image
            string mainImageName = await MainImage.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");
            newCar.CarImages.Add(new CarImage { ImageUrl = mainImageName, IsMain = true });

            // Save other images
            if (OtherImages != null)
            {
                foreach (var img in OtherImages)
                {
                    if (img.ValidateType("image/") && img.ValidateSize(FileSize.MB, 5))
                    {
                        string imgName = await img.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");
                        newCar.CarImages.Add(new CarImage { ImageUrl = imgName, IsMain = false });
                    }
                }
            }

            if (vm.SelectedFeatureIds != null && vm.SelectedFeatureIds.Any())
            {
                foreach (var id in vm.SelectedFeatureIds.Distinct())
                {
                    newCar.CarFeatures.Add(new CarFeature { FeatureId = id });
                }
            }

            await _context.Cars.AddAsync(newCar);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your car listing has been submitted and is awaiting moderator approval.";
            return RedirectToAction(nameof(Add));
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
