using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;
using Timoto.Utilities.Enums;
using Timoto.Utilities.Extensions;
using Timoto.ViewModels;

namespace Timoto.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CarController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

            if (!vm.MainPhoto.ValidateSize(FileSize.MB, 2))
            {
                ModelState.AddModelError("MainPhoto", "Main image must be less than 2MB");
                return View(await LoadCreateOrUpdateVM(vm));
            }

            if (vm.AdditionalPhotos != null)
            {
                foreach (var photo in vm.AdditionalPhotos)
                {
                    if (!photo.ValidateType("image"))
                    {
                        ModelState.AddModelError("AdditionalPhotos", "Only image files are allowed");
                        return View(await LoadCreateOrUpdateVM(vm));
                    }
                    if (!photo.ValidateSize(FileSize.MB, 2))
                    {
                        ModelState.AddModelError("AdditionalPhotos", "Each image must be less than 2MB");
                        return View(await LoadCreateOrUpdateVM(vm));
                    }
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
                VehicleTypeId = car.VehicleTypeId
            };

            vm = await LoadCreateOrUpdateVM(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, UpdateCarVM vm)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                vm = await LoadCreateOrUpdateVM(vm);
                return View(vm);
            }

            var car = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.CarFeatures)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound();

            // Basic field updates
            car.Name = vm.Name;
            car.Year = vm.Year;
            car.DailyPrice = vm.DailyPrice;
            car.Description = vm.Description;
            car.Seats = vm.Seats;
            car.Doors = vm.Doors;
            car.EngineSize = vm.EngineSize;
            car.Mileage = vm.Mileage;
            car.ExteriorColor = vm.ExteriorColor;
            car.InteriorColor = vm.InteriorColor;
            car.Location = vm.Location;
            car.Latitude = vm.Latitude;
            car.Longitude = vm.Longitude;
            car.FuelEconomy = vm.FuelEconomy;

            car.FuelTypeId = vm.FuelTypeId;
            car.TransmissionTypeId = vm.TransmissionTypeId;
            car.DriveTypeId = vm.DriveTypeId;
            car.BodyTypeId = vm.BodyTypeId;
            car.VehicleTypeId = vm.VehicleTypeId;

            // main image (optional)
            if (vm.MainPhoto != null)
            {
                if (!vm.MainPhoto.ValidateType("image") || !vm.MainPhoto.ValidateSize(FileSize.MB, 2))
                {
                    ModelState.AddModelError("MainPhoto", "Main image must be a valid image under 2MB");
                    return View(await LoadCreateOrUpdateVM(vm));
                }

                string newMainFileName = await vm.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");

                var currentMain = car.CarImages.FirstOrDefault(i => i.IsMain);
                if (currentMain != null)
                {
                    currentMain.ImageUrl.DeleteFile(_env.WebRootPath, "assets", "images", "cars");
                    currentMain.ImageUrl = newMainFileName;
                }
                else
                {
                    car.CarImages.Add(new CarImage { ImageUrl = newMainFileName, IsMain = true });
                }
            }


            if (vm.ImageIds != null)
            {
                car.CarImages.RemoveAll(i => !i.IsMain && !vm.ImageIds.Contains(i.Id));
            }


            if (vm.AdditionalPhotos != null)
            {
                foreach (var photo in vm.AdditionalPhotos)
                {
                    if (!photo.ValidateType("image") || !photo.ValidateSize(FileSize.MB, 2))
                    {
                        ModelState.AddModelError("AdditionalPhotos", "All images must be valid and under 2MB");
                        return View(await LoadCreateOrUpdateVM(vm));
                    }

                    string fileName = await photo.CreateFileAsync(_env.WebRootPath, "assets", "images", "cars");
                    car.CarImages.Add(new CarImage { ImageUrl = fileName, IsMain = false });
                }
            }


            car.CarFeatures = vm.FeatureIds?
                .Select(fid => new CarFeature { CarId = car.Id, FeatureId = fid })
                .ToList();

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


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
                update.Features = await _context.Features.ToListAsync();
                return vm;
            }
            return vm;
        }



    }
}
