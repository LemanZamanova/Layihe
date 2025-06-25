using System.ComponentModel.DataAnnotations;
using Timoto.Models;

namespace Timoto.ViewModels
{
    public class CreateCarVM
    {
        [Required]
        public string Name { get; set; }

        public int Seats { get; set; }
        public int Doors { get; set; }
        public int LuggageVolume { get; set; }
        public int EngineSize { get; set; }
        public int Year { get; set; }
        public int Mileage { get; set; }
        public float FuelEconomy { get; set; }

        public string ExteriorColor { get; set; }
        public string InteriorColor { get; set; }

        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int DailyPrice { get; set; }

        public string Description { get; set; }

        [Required]
        public IFormFile MainPhoto { get; set; }

        public List<IFormFile>? AdditionalPhotos { get; set; }

        [Range(1, int.MaxValue)]
        public int FuelTypeId { get; set; }

        [Range(1, int.MaxValue)]
        public int TransmissionTypeId { get; set; }

        [Range(1, int.MaxValue)]
        public int DriveTypeId { get; set; }

        [Range(1, int.MaxValue)]
        public int BodyTypeId { get; set; }

        [Range(1, int.MaxValue)]
        public int VehicleTypeId { get; set; }

        public List<int>? FeatureIds { get; set; }

        // Dropdownlar üçün
        public List<FuelType> FuelTypes { get; set; }
        public List<TransmissionType> TransmissionTypes { get; set; }
        public List<Models.DriveType> DriveTypes { get; set; }
        public List<BodyType> BodyTypes { get; set; }
        public List<VehicleType> VehicleTypes { get; set; }
        public List<Feature>? Features { get; set; }
    }
}
