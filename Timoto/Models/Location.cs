using Timoto.Areas.Admin.Models.Base;

namespace Timoto.Models
{
    public class Location : BaseEntity
    {
        public string Name { get; set; }


        public ICollection<Car>? Cars { get; set; }
    }
}
