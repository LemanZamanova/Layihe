using Timoto.Models;

namespace Timoto.ViewModels
{
    public class FavoriteCarGroupVM
    {
        public Car Car { get; set; }
        public int TotalLikes { get; set; }
        public List<AppUser> Users { get; set; }
    }
}
