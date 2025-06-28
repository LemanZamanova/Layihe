using System.ComponentModel.DataAnnotations;

using Timoto.Models;

namespace Timoto.ViewModels
{
    public class CreateCarVM
    {
        [Required(ErrorMessage = "Name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        public string Name { get; set; }

        [Range(1, 20, ErrorMessage = "Seats must be between 1 and 20")]
        public int Seats { get; set; }

        [Range(1, 10, ErrorMessage = "Doors must be between 1 and 10")]
        public int Doors { get; set; }


        public int LuggageVolume { get; set; }


        public int EngineSize { get; set; }

        [Range(1990, 2025, ErrorMessage = "Year must be between 1990 and 2025")]
        public int Year { get; set; }


        public int Mileage { get; set; }

        [Range(1, 100, ErrorMessage = "Fuel Economy must be a positive number")]
        public float FuelEconomy { get; set; }

        [MaxLength(50, ErrorMessage = "Exterior color cannot exceed 50 characters")]
        public string ExteriorColor { get; set; }

        [MaxLength(50, ErrorMessage = "Interior color cannot exceed 50 characters")]
        public string InteriorColor { get; set; }

        [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        public string Location { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [Range(1, 1000000, ErrorMessage = "Daily price must be a positive number")]
        public int DailyPrice { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Main image is required")]
        public IFormFile MainPhoto { get; set; }

        public List<IFormFile>? AdditionalPhotos { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a fuel type")]
        public int FuelTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a transmission type")]
        public int TransmissionTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a drive type")]
        public int DriveTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a body type")]
        public int BodyTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a vehicle type")]
        public int VehicleTypeId { get; set; }

        public List<int>? FeatureIds { get; set; }

        // Dropdownlar üçün
        public List<FuelType> FuelTypes { get; set; }
        public List<TransmissionType> TransmissionTypes { get; set; }
        public List<Models.DriveType> DriveTypes { get; set; }
        public List<BodyType> BodyTypes { get; set; }
        public List<VehicleType> VehicleTypes { get; set; }
        public List<Feature>? Features { get; set; }
        public int? LocationId { get; set; }
        public IEnumerable<Location>? Locations { get; set; }
    }
}
