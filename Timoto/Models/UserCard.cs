using Timoto.Models.Base;

namespace Timoto.Models
{
    public class UserCard : BaseEntity
    {
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; }
    }
}
