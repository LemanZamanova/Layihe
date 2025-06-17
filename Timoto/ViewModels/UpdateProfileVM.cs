using System.ComponentModel.DataAnnotations;
using Timoto.Models;

namespace Timoto.ViewModels
{
    public class UpdateProfileVM : IBaseProfileVM
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MinLength(5)]
        [MaxLength(50)]
        public string Surname { get; set; }


        [Required]
        [MaxLength(150)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [Required]
        [RegularExpression(@"^\+994(50|51|55|70|77|10)\d{7}$", ErrorMessage = "The phone number format is invalid. For example: +99450xxxxxxx")]
        public string Phone { get; set; }


        [Required(ErrorMessage = "Current password is required to update your profile.")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        public IFormFile? ProfileImage { get; set; }
        public List<Notification>? Notifications { get; set; }


    }
}
