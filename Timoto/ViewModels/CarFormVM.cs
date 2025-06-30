using Microsoft.AspNetCore.Mvc.Rendering;
using Timoto.Models;

namespace Timoto.ViewModels
{
    public class CarFormVM
    {
        public Car Car { get; set; }

        // FK üçün dropdown listlər
        public IEnumerable<SelectListItem> FuelTypes { get; set; }
        public IEnumerable<SelectListItem> TransmissionTypes { get; set; }
        public IEnumerable<SelectListItem> DriveTypes { get; set; }
        public IEnumerable<SelectListItem> BodyTypes { get; set; }

        // Many-to-many üçün checkbox
        public List<int> SelectedFeatureIds { get; set; }
        public IEnumerable<Feature> AllFeatures { get; set; }
        public int? LocationId { get; set; }
        public IEnumerable<Location>? Locations { get; set; }
        public IEnumerable<SelectListItem> VehicleTypes { get; set; }
    }
}
