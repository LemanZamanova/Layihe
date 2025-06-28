public class FilterBookingVM
{
    public string? Location { get; set; }
    public string? BodyType { get; set; }

    public DateTime? PickUpDate { get; set; }
    public string? PickUpTime { get; set; }
    public int CarId { get; set; }

    public DateTime? ReturnDate { get; set; }
    public string? ReturnTime { get; set; }
    public List<int> FavoriteCarIds { get; set; } = new();

    public List<AvailableCarItem> AvailableCars { get; set; } = new();

    public class AvailableCarItem
    {

        public int CarId { get; set; }
        public string Name { get; set; }
        public int DailyPrice { get; set; }
        public string ImageUrl { get; set; }
        public double DistanceMeters { get; set; }
        public string DistanceDisplay =>
     DistanceMeters >= 1000 ? $"{(DistanceMeters / 1000):0.0} km"
     : DistanceMeters < 100 ? "100 m"
     : $"{(int)DistanceMeters} m";


        public bool IsFavorited { get; set; }

        public int LikeCount { get; set; }
        public int Seats { get; set; }
        public int Doors { get; set; }
        public int LuggageVolume { get; set; }
        public string BodyTypeName { get; set; }
        public bool IsActiveBooking { get; set; }
        public DateTime? ActiveBookingEndDate { get; set; }
    }

}
