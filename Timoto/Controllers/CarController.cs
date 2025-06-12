using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
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
        public IActionResult Index()
        {
            var cars = _context.Cars
                .Include(c => c.BodyType)
                .Include(c => c.FuelType)
                .Include(c => c.TransmissionType)
                .Include(c => c.DriveType)
                .Include(c => c.CarFeatures)
                    .ThenInclude(cf => cf.Feature)
                .ToList();

            return View(cars);
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
    }
}
