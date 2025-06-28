using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.CodeAnalysis;
using Timoto.Models.Base;

namespace Timoto.Models
{
    public class Car : BaseEntity
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

        public int? LocationId { get; set; }
        public Location LocationRef { get; set; }
        public string? Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }


        public int DailyPrice { get; set; }
        public int LikeCount { get; set; }
        public string Description { get; set; }

        // Foreign keys
        public int FuelTypeId { get; set; }
        public FuelType FuelType { get; set; }

        public int TransmissionTypeId { get; set; }
        public TransmissionType TransmissionType { get; set; }

        public int DriveTypeId { get; set; }
        public DriveType DriveType { get; set; }

        public int BodyTypeId { get; set; }
        public BodyType BodyType { get; set; }


        public int VehicleTypeId { get; set; }
        public VehicleType VehicleType { get; set; }


        public List<CarImage> CarImages { get; set; }
        [NotMapped]
        public CarImage MainImage => CarImages?.FirstOrDefault(img => img.IsMain);

        //Many-to-Many: CarFeatures
        public ICollection<CarFeature> CarFeatures { get; set; }
        public ICollection<FavoriteCar> FavoritedBy { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
