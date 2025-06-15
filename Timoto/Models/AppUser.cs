using Microsoft.AspNetCore.Identity;

namespace Timoto.Models
{
    public class AppUser : IdentityUser
    {
        public string Name { get; set; }
        public string Surname { get; set; }

        public string Phone { get; set; }
        public string? EmailConfirmationCode { get; set; }
        public DateTime? CodeExpireAt { get; set; }
    }
}
