using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Timoto.Models;

namespace Timoto.ViewModels
{
    public class UpdateCarVM
    {
        public int Id { get; set; }

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

        public string PrimaryImage { get; set; }
        public IFormFile? MainPhoto { get; set; }
        public List<CarImage>? CarImages { get; set; }
        [BindProperty]
        public List<int>? ImageIds { get; set; } = new();
        public List<IFormFile>? AdditionalPhotos { get; set; }

        public int FuelTypeId { get; set; }
        public int TransmissionTypeId { get; set; }
        public int DriveTypeId { get; set; }
        public int BodyTypeId { get; set; }
        public int VehicleTypeId { get; set; }

        public List<int> FeatureIds { get; set; }

        // For dropdowns
        public List<FuelType> FuelTypes { get; set; }
        public List<TransmissionType> TransmissionTypes { get; set; }
        public List<Models.DriveType> DriveTypes { get; set; }
        public List<BodyType> BodyTypes { get; set; }
        public List<VehicleType> VehicleTypes { get; set; }
        public List<Feature> Features { get; set; }
    }
}
