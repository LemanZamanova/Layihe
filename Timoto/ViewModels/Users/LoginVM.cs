using System.ComponentModel.DataAnnotations;

namespace Timoto.ViewModels.Users
{
    public class LoginVM
    {
        [MaxLength(150)]
        public string UserNameOrEmail { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }


        public bool RememberMe { get; set; }
    }
}
