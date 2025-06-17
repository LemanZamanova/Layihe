using System.ComponentModel.DataAnnotations;
using Timoto.Models;

namespace Timoto.ViewModels
{
    public class ProfileVM : IBaseProfileVM
    {
        public string Name { get; set; }

        public string Surname { get; set; }


        public string Email { get; set; }
        [Required]
        [RegularExpression(@"^\+994(50|51|55|70|77|10)\d{7}$", ErrorMessage = "The phone number format is invalid. For example: +99450xxxxxxx")]
        public string Phone { get; set; }

        public List<Booking> Bookings { get; set; }
        public List<Notification> Notifications { get; set; }
        public List<Car> FavoriteCars { get; set; }
        public List<UserCard> Cards { get; set; }
    }
}
