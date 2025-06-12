using Timoto.Models.Base;

namespace Timoto.Models
{
    public class FuelType : BaseEntity
    {
        public string Name { get; set; }

        public ICollection<Car> Cars { get; set; }
    }
}
