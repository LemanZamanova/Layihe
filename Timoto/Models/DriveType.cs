using Timoto.Models.Base;

namespace Timoto.Models
{
    public class DriveType : BaseEntity
    {
        public string Name { get; set; }

        public ICollection<Car> Cars { get; set; }
    }
}
