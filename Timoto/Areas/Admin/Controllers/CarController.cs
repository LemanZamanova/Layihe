using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;
using Timoto.Services.Interface;
using Timoto.Utilities.Enums;
using Timoto.Utilities.Extensions;
using Timoto.ViewModels;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]


    public class CarController : AdminBaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;


        public CarController(AppDbContext context, IWebHostEnvironment env, IEmailService emailService)
        {
            _context = context;
            _env = env;

            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.BodyType)
                .Include(c => c.VehicleType)
                .Include(c => c.DriveType)
                .Include(c => c.CarImages)
                .AsNoTracking()
                .ToListAsync();

            var model = cars.Select(c => new GetCarVM
            {
                Id = c.Id,
                Name = c.Name,
                DailyPrice = c.DailyPrice,
                MainImage = c.CarImages.FirstOrDefault(i => i.IsMain)?.ImageUrl,
                FuelTypeName = c.FuelType?.Name,
                TransmissionTypeName = c.TransmissionType?.Name,
                BodyTypeName = c.BodyType?.Name,
                VehicleTypeName = c.VehicleType?.Name,
                DriveTypeName = c.DriveType?.Name
            }).ToList();

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var vm = await LoadCreateOrUpdateVM(new CreateCarVM());
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCarVM vm)
        {
            ModelState.Remove(nameof(vm.FuelTypes));
            ModelState.Remove(nameof(vm.TransmissionTypes));
            ModelState.Remove(nameof(vm.DriveTypes));
            ModelState.Remove(nameof(vm.BodyTypes));
            ModelState.Remove(nameof(vm.VehicleTypes));
            ModelState.Remove(nameof(vm.Features));

            if (!ModelState.IsValid)
            {
                vm = await LoadCreateOrUpdateVM(vm);
                return View(vm);
            }
            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    foreach (var error in item.Value.Errors)
                    {
                        Console.WriteLine($"ModelState Error - {item.Key}: {error.ErrorMessage}");
                    }
                }

                return View(await LoadCreateOrUpdateVM(vm));
            }

            if (!vm.MainPhoto.ValidateType("image"))
            {
                ModelState.AddModelError("MainPhoto", "Only image files are allowed");
                return View(await LoadCreateOrUpdateVM(vm));
            }

            //if (!vm.MainPhoto.ValidateSize(FileSize.MB, 3))
            //{
            //    ModelState.AddModelError("MainPhoto", "Main image must be less than 3MB");
            //    return View(await LoadCreateOrUpdateVM(vm));
            //}

            if (vm.AdditionalPhotos != null)
            {
                foreach (var photo in vm.AdditionalPhotos)
                {
                    if (!photo.ValidateType("image"))
                    {
                        ModelState.AddModelError("AdditionalPhotos", "Only image files are allowed");
                        return View(await LoadCreateOrUpdateVM(vm));
                    }
                    //if (!photo.ValidateSize(FileSize.MB, 2))
                    //{
                    //    ModelState.AddModelError("AdditionalPhotos", "Each image must be less than 3MB");
                    //    return View(await LoadCreateOrUpdateVM(vm));
                    //}
                }
            }

            var car = new Car
            {
                Name = vm.Name,
                Seats = vm.Seats,
                Doors = vm.Doors,
                LuggageVolume = vm.LuggageVolume,
                EngineSize = vm.EngineSize,
                Year = vm.Year,
                Mileage = vm.Mileage,
                FuelEconomy = vm.FuelEconomy,
                ExteriorColor = vm.ExteriorColor,
                InteriorColor = vm.InteriorColor,
                LocationId = vm.LocationId,
                Location = vm.Location,
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                DailyPrice = vm.DailyPrice,
                Description = vm.Description,
                FuelTypeId = vm.FuelTypeId,
                TransmissionTypeId = vm.TransmissionTypeId,
                DriveTypeId = vm.DriveTypeId,
                BodyTypeId = vm.BodyTypeId,
                VehicleTypeId = vm.VehicleTypeId,
                Status = CarStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddHours(4),
                CarImages = new List<CarImage>(),
                CarFeatures = vm.FeatureIds?.Select(id => new CarFeature { FeatureId = id }).ToList()
            };

            string mainFileName = await vm.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");
            car.CarImages.Add(new CarImage { ImageUrl = mainFileName, IsMain = true });

            if (vm.AdditionalPhotos != null)
            {
                foreach (var photo in vm.AdditionalPhotos)
                {
                    string fileName = await photo.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");
                    car.CarImages.Add(new CarImage { ImageUrl = fileName, IsMain = false });
                }
            }

            await _context.Cars.AddAsync(car);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Update(int id)
        {
            var car = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.CarFeatures)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null) return NotFound();

            var vm = new UpdateCarVM
            {
                Id = car.Id,
                Name = car.Name,
                DailyPrice = car.DailyPrice,
                Year = car.Year,
                Description = car.Description,
                PrimaryImage = car.CarImages.FirstOrDefault(i => i.IsMain)?.ImageUrl,
                CarImages = car.CarImages.Where(i => !i.IsMain).ToList(),
                FeatureIds = car.CarFeatures.Select(cf => cf.FeatureId).ToList(),
                FuelTypeId = car.FuelTypeId,
                TransmissionTypeId = car.TransmissionTypeId,
                DriveTypeId = car.DriveTypeId,
                BodyTypeId = car.BodyTypeId,
                VehicleTypeId = car.VehicleTypeId,
                Seats = car.Seats,
                Doors = car.Doors,
                LuggageVolume = car.LuggageVolume,
                EngineSize = car.EngineSize,
                Mileage = car.Mileage,
                FuelEconomy = car.FuelEconomy,
                ExteriorColor = car.ExteriorColor,
                InteriorColor = car.InteriorColor,
                Location = car.Location,
                Latitude = car.Latitude,
                Longitude = car.Longitude,
            };

            vm = await LoadCreateOrUpdateVM(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Update(int id, UpdateCarVM vm)
        {
            ModelState.Remove("FuelTypes");
            ModelState.Remove("TransmissionTypes");
            ModelState.Remove("DriveTypes");
            ModelState.Remove("BodyTypes");
            ModelState.Remove("VehicleTypes");
            ModelState.Remove("Features");
            ModelState.Remove("ImageIds");

            vm = await LoadCreateOrUpdateVM(vm);

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var existed = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.CarFeatures)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existed == null) return NotFound();


            //if (vm.MainPhoto is not null &&
            //    (!vm.MainPhoto.ValidateType("image") || !vm.MainPhoto.ValidateSize(FileSize.MB, 2)))
            //{
            //    ModelState.AddModelError(nameof(vm.MainPhoto), "Main image must be a valid image under 2MB.");
            //    return View(vm);
            //}

            if (vm.AdditionalPhotos != null)
            {
                foreach (var photo in vm.AdditionalPhotos)
                {
                    //if (!photo.ValidateType("image") || !photo.ValidateSize(FileSize.MB, 2))
                    //{
                    //    ModelState.AddModelError(nameof(vm.AdditionalPhotos), $"File {photo.FileName} is invalid.");
                    //    return View(vm);
                    //}
                }
            }

            // Field update
            existed.Name = vm.Name;
            existed.DailyPrice = vm.DailyPrice;
            existed.Description = vm.Description;
            existed.Seats = vm.Seats;
            existed.Doors = vm.Doors;
            existed.EngineSize = vm.EngineSize;
            existed.Mileage = vm.Mileage;
            existed.FuelEconomy = vm.FuelEconomy;
            existed.ExteriorColor = vm.ExteriorColor;
            existed.InteriorColor = vm.InteriorColor;
            existed.LocationId = vm.LocationId;
            existed.Location = vm.Location;
            existed.Latitude = vm.Latitude;
            existed.Longitude = vm.Longitude;
            existed.Year = vm.Year;
            existed.FuelTypeId = vm.FuelTypeId;
            existed.TransmissionTypeId = vm.TransmissionTypeId;
            existed.DriveTypeId = vm.DriveTypeId;
            existed.BodyTypeId = vm.BodyTypeId;
            existed.VehicleTypeId = vm.VehicleTypeId;

            // Features
            existed.CarFeatures = vm.FeatureIds?.Distinct().Select(fid => new CarFeature
            {
                CarId = id,
                FeatureId = fid
            }).ToList();

            if (vm.MainPhoto != null && vm.MainPhoto.Length > 0)
            {
                var oldMain = existed.CarImages.FirstOrDefault(i => i.IsMain);
                if (oldMain != null)
                {
                    try
                    {
                        oldMain.ImageUrl.DeleteFile(_env.WebRootPath, "assets", "images", "cars");
                    }
                    catch (IOException ex)
                    {
                        // loglama və ya bildiriş üçün istifadə oluna bilər
                        Console.WriteLine("Main image delete error: " + ex.Message);
                    }

                    _context.CarImages.Remove(oldMain);
                }

                string newMain = await vm.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");
                existed.CarImages.Add(new CarImage { ImageUrl = newMain, IsMain = true, CreatedAt = DateTime.UtcNow.AddHours(4) });
            }

            if (vm.ImageIds == null) vm.ImageIds = new();
            var toDelete = existed.CarImages
                .Where(i => !i.IsMain && !vm.ImageIds.Contains(i.Id))
                .ToList();

            foreach (var img in toDelete)
            {
                img.ImageUrl.DeleteFile(_env.WebRootPath, "assets", "images", "cars");
            }

            _context.CarImages.RemoveRange(toDelete);


            if (vm.AdditionalPhotos != null)
            {
                foreach (var photo in vm.AdditionalPhotos)
                {
                    string fileName = await photo.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");
                    existed.CarImages.Add(new CarImage
                    {
                        ImageUrl = fileName,
                        IsMain = false,
                        CreatedAt = DateTime.UtcNow.AddHours(4)
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        [RoleAuthorize("Admin")]

        public async Task<IActionResult> Delete(int id)
        {
            var car = await _context.Cars.Include(c => c.CarImages).FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound();

            foreach (var img in car.CarImages)
            {
                img.ImageUrl.DeleteFile(_env.WebRootPath, "assets", "images", "cars");
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<T> LoadCreateOrUpdateVM<T>(T vm) where T : class
        {
            if (vm is CreateCarVM create)
            {
                create.FuelTypes = await _context.FuelTypes.ToListAsync();
                create.TransmissionTypes = await _context.TransmissionTypes.ToListAsync();
                create.DriveTypes = await _context.DriveTypes.ToListAsync();
                create.BodyTypes = await _context.BodyTypes.ToListAsync();
                create.VehicleTypes = await _context.VehicleTypes.ToListAsync();
                create.Locations = await _context.Locations.ToListAsync();
                create.Features = await _context.Features.ToListAsync();
                return vm;
            }
            else if (vm is UpdateCarVM update)
            {
                update.FuelTypes = await _context.FuelTypes.ToListAsync();
                update.TransmissionTypes = await _context.TransmissionTypes.ToListAsync();
                update.DriveTypes = await _context.DriveTypes.ToListAsync();
                update.BodyTypes = await _context.BodyTypes.ToListAsync();
                update.VehicleTypes = await _context.VehicleTypes.ToListAsync();
                update.Locations = await _context.Locations.ToListAsync();
                update.Features = await _context.Features.ToListAsync();
                return vm;
            }
            return vm;
        }


        public async Task<IActionResult> Detail(int id)
        {
            var car = await _context.Cars
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.DriveType)
                .Include(c => c.BodyType)
                .Include(c => c.VehicleType)
                .Include(c => c.CarImages)
                .Include(c => c.CarFeatures)
                    .ThenInclude(cf => cf.Feature)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null) return NotFound();

            var vm = new CarDetailVM
            {
                Name = car.Name,
                DailyPrice = car.DailyPrice,
                Year = car.Year,
                Description = car.Description,
                Seats = car.Seats,
                Doors = car.Doors,
                LuggageVolume = car.LuggageVolume,
                EngineSize = car.EngineSize,
                Mileage = car.Mileage,
                FuelEconomy = car.FuelEconomy,
                ExteriorColor = car.ExteriorColor,
                InteriorColor = car.InteriorColor,
                Location = car.Location,
                Latitude = car.Latitude.ToString("F6"),
                Longitude = car.Longitude.ToString("F6"),
                FuelTypeName = car.FuelType?.Name,
                TransmissionTypeName = car.TransmissionType?.Name,
                DriveTypeName = car.DriveType?.Name,
                BodyTypeName = car.BodyType?.Name,
                VehicleTypeName = car.VehicleType?.Name,
                MainImage = car.CarImages.FirstOrDefault()?.ImageUrl,
                AdditionalImages = car.CarImages.Skip(1).Select(i => i.ImageUrl).ToList(),
                Features = car.CarFeatures.Select(cf => cf.Feature.Name).ToList()
            };

            return View(vm);
        }
        public async Task<IActionResult> Pending()
        {
            var pendingCars = await _context.Cars
                .Include(c => c.User)
                .Where(c => c.Status == CarStatus.Pending && c.UserId != null)
                .ToListAsync();

            return View(pendingCars);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            car.Status = CarStatus.Approved;
            await _context.SaveChangesAsync();

            return RedirectToAction("Pending");
        }
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var car = await _context.Cars
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null) return NotFound();

            // Email göndər
            await _emailService.SendEmailAsync(car.User.Email, "Maşın Rədd Edildi",
                $"Təəssüf ki, maşınınız moderator tərəfindən rədd edildi. Səbəb: {reason}");

            // Bazadan sil
            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return RedirectToAction("Pending");
        }



    }

}

