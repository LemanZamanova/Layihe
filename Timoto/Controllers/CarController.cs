using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Models;
using Timoto.ViewModels;

namespace Timoto.Controllers
{

    public class CarController : Controller
    {
        private readonly AppDbContext _context;

        public CarController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index(CarFilterVM filter)
        {
            var query = _context.Cars
                .Include(c => c.BodyType)
                 .Include(c => c.VehicleType)
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.DriveType)
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
            var vm = new CarFilterVM
            {
                Cars = query.ToList(),
                SelectedBodyTypeIds = filter.SelectedBodyTypeIds ?? new List<int>(),
                SelectedVehicleTypeIds = filter.SelectedVehicleTypeIds ?? new List<int>(),
                SelectedSeatCounts = filter.SelectedSeatCounts ?? new List<int>(),
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice
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


            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookingVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            DateTime startDate = DateTime.Parse($"{model.PickupDate} {model.PickupTime}");
            DateTime endDate = DateTime.Parse($"{model.CollectioDate} {model.CollectionTime}");


            bool isOverlap = _context.Bookings.Any(b =>
                b.CarId == model.CarId &&
                ((startDate >= b.StartDate && startDate < b.EndDate) ||
                 (endDate > b.StartDate && endDate <= b.EndDate) ||
                 (startDate <= b.StartDate && endDate >= b.EndDate)));

            if (isOverlap)
            {
                TempData["BookingError"] = "Bu tarixdə maşın artıq bron edilib.";
                return RedirectToAction("Detail", "Car", new { id = model.CarId });
            }


            if ((startDate - DateTime.Now).TotalDays > 30)
            {
                TempData["BookingError"] = "Çox uzaq tarix üçün bron edilə bilməz.";
                return RedirectToAction("Detail", "Car", new { id = model.CarId });
            }


            Booking booking = new Booking
            {
                CarId = model.CarId,

                StartDate = startDate,
                EndDate = endDate,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["BookingSuccess"] = "Maşın uğurla bron edildi.";
            return RedirectToAction("Detail", "Car", new { id = model.CarId });
        }



    }
}
