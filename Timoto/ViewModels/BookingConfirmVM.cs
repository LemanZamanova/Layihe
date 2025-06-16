using Microsoft.AspNetCore.Mvc.Rendering;

namespace Timoto.ViewModels
{
    public class BookingConfirmVM
    {
        public BookingVM BookingVM { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string? SelectedCardId { get; set; }
        public List<SelectListItem>? UserCards { get; set; } = new();
    }
}
