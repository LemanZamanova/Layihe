using Timoto.Models.Base;

namespace Timoto.Models
{
    public class Booking : BaseEntity
    {
        public int CarId { get; set; }
        public Car Car { get; set; }

        public string? UserId { get; set; }
        public AppUser? User { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string StripePaymentIntentId { get; set; }
        public string PaymentStatus { get; set; }

    }
}
