using Timoto.Models.Base;

namespace Timoto.Models
{
    public class CarImage : BaseEntity
    {
        public string ImageUrl { get; set; }

        public int CarId { get; set; }
        public Car Car { get; set; }

        public bool IsMain { get; set; }
    }
}
