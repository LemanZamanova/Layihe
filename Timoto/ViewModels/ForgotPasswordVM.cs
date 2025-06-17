using System.ComponentModel.DataAnnotations;

namespace Timoto.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
