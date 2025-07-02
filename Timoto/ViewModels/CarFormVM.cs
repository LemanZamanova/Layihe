using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Timoto.Models;

namespace Timoto.ViewModels
{
    public class CarFormVM
    {
        public Car Car { get; set; }


        [BindNever]
        public IEnumerable<SelectListItem> FuelTypes { get; set; }
        [BindNever]
        public IEnumerable<SelectListItem> TransmissionTypes { get; set; }
        [BindNever]
        public IEnumerable<SelectListItem> DriveTypes { get; set; }
        [BindNever]
        public IEnumerable<SelectListItem> BodyTypes { get; set; }
        [BindNever]


        public List<int> SelectedFeatureIds { get; set; }
        [BindNever]
        public IEnumerable<Feature> AllFeatures { get; set; }
        [BindRequired]
        public int? LocationId { get; set; }

        public IEnumerable<Location>? Locations { get; set; }
        [BindNever]
        public IEnumerable<SelectListItem> VehicleTypes { get; set; }
    }
}
