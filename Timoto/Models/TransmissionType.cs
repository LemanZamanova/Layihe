using Timoto.Models.Base;

namespace Timoto.Models
{
    public class TransmissionType : BaseEntity
    {
        public string Name { get; set; }

        public ICollection<Car> Cars { get; set; }
    }
}
