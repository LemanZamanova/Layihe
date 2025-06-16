using System.ComponentModel.DataAnnotations;

namespace Timoto.ViewModels
{
    public class BookingVM
    {
        public int CarId { get; set; }
        [Required]
        public string PickupDate { get; set; }
        [Required]
        public string PickupTime { get; set; }
        [Required]
        public string CollectionDate { get; set; }
        [Required]
        public string CollectionTime { get; set; }



    }
}
