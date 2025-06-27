using System.ComponentModel.DataAnnotations;

namespace Timoto.ViewModels
{
    public class ContactVM
    {
        //public string RecaptchaToken { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }
    }
}
