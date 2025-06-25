using System.ComponentModel.DataAnnotations;
using Timoto.Models.Base;

namespace Timoto.Models
{
    public class UserCard : BaseEntity
    {
        [Required]
        public string CardHolderName { get; set; }

        public string CardNumber { get; set; }
        [Required]
        public int ExpiryMonth { get; set; }
        [Required]

        public int ExpiryYear { get; set; }

        public string CVV { get; set; }
        public string Last4Digits => CardNumber?.Length >= 4 ? CardNumber.Substring(CardNumber.Length - 4) : "";
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public string StripePaymentMethodId { get; set; }

    }
}
