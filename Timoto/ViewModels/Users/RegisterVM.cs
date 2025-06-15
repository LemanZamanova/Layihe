using System.ComponentModel.DataAnnotations;

namespace Timoto.ViewModels.Users
{
    public class RegisterVM
    {
        [MinLength(3)]
        [MaxLength(50)]
        public string Name { get; set; }
        public string Surname { get; set; }

        [MaxLength(150)]
        [DataType(DataType.EmailAddress)]


        public string Email { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }

        [Required]
        [RegularExpression(@"^\+994(50|51|55|70|77|10)\d{7}$", ErrorMessage = "Telefon nömrəsi düzgün formatda deyil. Məsələn: +99450xxxxxxx")]
        public string Phone { get; set; }
    }
}
