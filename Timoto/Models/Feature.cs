using System.ComponentModel.DataAnnotations;
using Timoto.Models.Base;

namespace Timoto.Models
{
    public class Feature : BaseEntity
    {
        [Required]
        [MinLength(3)]
        [MaxLength(20)]

        public string Name { get; set; }

        public ICollection<CarFeature>? CarFeatures { get; set; }
    }
}
