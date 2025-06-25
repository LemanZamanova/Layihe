using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Timoto.ViewModels
{
    public class BookingConfirmVM
    {
        public BookingVM BookingVM { get; set; }


        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int CarId { get; set; }
        [Required]
        public string PickupDate { get; set; }
        [Required]
        public string PickupTime { get; set; }
        [Required]
        public string CollectionDate { get; set; }
        [Required]
        public string CollectionTime { get; set; }

        public int SelectedCardId { get; set; }
        public List<SelectListItem>? UserCards { get; set; } = new();
        public string BookingSummary { get; set; }
        public decimal TotalAmount { get; set; }
        public string UserId { get; set; }
    }
}
