using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;

namespace Timoto.Controllers
{
    public class FilterBookingController : Controller
    {
        private readonly AppDbContext _context;

        public FilterBookingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(FilterBookingVM vm)
        {
            ViewBag.BodyTypes = await _context.BodyTypes.ToListAsync();
            ViewBag.Locations = await _context.Cars
                .Where(c => !string.IsNullOrEmpty(c.Location))
                .Select(c => c.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();


            bool filtersEmpty =
                string.IsNullOrWhiteSpace(vm.Location) &&
                string.IsNullOrWhiteSpace(vm.BodyType) &&
                string.IsNullOrWhiteSpace(vm.PickUpTime) &&
                string.IsNullOrWhiteSpace(vm.ReturnTime) &&
                vm.PickUpDate == null &&
                vm.ReturnDate == null;

            if (filtersEmpty)
            {
                vm.AvailableCars = new List<FilterBookingVM.AvailableCarItem>();
                return View(vm);
            }


            bool hasPickup = DateTime.TryParse($"{vm.PickUpDate:yyyy-MM-dd} {vm.PickUpTime}", out var pickUp);
            bool hasReturn = DateTime.TryParse($"{vm.ReturnDate:yyyy-MM-dd} {vm.ReturnTime}", out var returnDt);


            double userLat = 0;
            double userLon = 0;

            if (!string.IsNullOrWhiteSpace(vm.Location))
            {
                var locationCoords = await _context.Cars
                    .Where(c => c.Location.ToLower().Contains(vm.Location.ToLower()) && c.Latitude != 0 && c.Longitude != 0)
                    .GroupBy(c => c.Location.ToLower())
                    .Select(g => new
                    {
                        AvgLat = g.Average(c => c.Latitude),
                        AvgLon = g.Average(c => c.Longitude)
                    })
                    .FirstOrDefaultAsync();

                if (locationCoords != null)
                {
                    userLat = locationCoords.AvgLat;
                    userLon = locationCoords.AvgLon;
                }
            }

            var cars = await _context.Cars
                .Include(c => c.CarImages)
                .Include(c => c.Bookings)
                .Include(c => c.BodyType)
                .ToListAsync();

            vm.AvailableCars = cars
                .Where(c =>
                    (!hasPickup || !c.Bookings.Any(b => b.StartDate < returnDt && b.EndDate > pickUp)) &&
                    (string.IsNullOrWhiteSpace(vm.BodyType) || c.BodyType?.Name.ToLower().Contains(vm.BodyType.ToLower()) == true)
                )
                .Select(c => new FilterBookingVM.AvailableCarItem
                {
                    CarId = c.Id,
                    Name = c.Name,
                    DailyPrice = c.DailyPrice,
                    ImageUrl = c.CarImages.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "",
                    DistanceMeters = CalculateDistance(userLat, userLon, c.Latitude, c.Longitude),
                    LikeCount = c.LikeCount,
                    Seats = c.Seats,
                    Doors = c.Doors,
                    LuggageVolume = c.LuggageVolume,
                    BodyTypeName = c.BodyType?.Name,
                    IsActiveBooking = c.Bookings.Any(b => DateTime.Now >= b.StartDate && DateTime.Now <= b.EndDate && !b.IsDeleted),
                    ActiveBookingEndDate = c.Bookings
                        .Where(b => DateTime.Now >= b.StartDate && DateTime.Now <= b.EndDate && !b.IsDeleted)
                        .OrderBy(b => b.EndDate)
                        .FirstOrDefault()?.EndDate
                })
                .OrderBy(c => c.DistanceMeters)
                .ToList();

            return View(vm);
        }


        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double deg) => deg * (Math.PI / 180);
    }
}
