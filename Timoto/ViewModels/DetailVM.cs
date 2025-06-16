using Timoto.Models;

namespace Timoto.ViewModels
{
    public class DetailVM
    {
        public Car Cars { get; set; }
        public BookingVM BookingVM { get; set; } = new BookingVM();
    }
}
