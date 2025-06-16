using Timoto.Models.Base;

namespace Timoto.Models
{
    public class FavoriteCar : BaseEntity
    {
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public int CarId { get; set; }
        public Car Car { get; set; }
    }
}
