using Timoto.Models.Base;

namespace Timoto.Models
{
    public class Notification : BaseEntity
    {
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
