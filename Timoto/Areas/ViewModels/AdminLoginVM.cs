using System.ComponentModel.DataAnnotations;

namespace Timoto.Areas.ViewModels
{
    public class AdminLoginVM
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
