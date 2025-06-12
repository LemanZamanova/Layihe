using Timoto.Models.Base;

namespace Timoto.Models
{
    public class Feature : BaseEntity
    {
        public string Name { get; set; }

        public ICollection<CarFeature> CarFeatures { get; set; }
    }
}
