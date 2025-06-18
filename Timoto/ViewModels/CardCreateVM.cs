using System.ComponentModel.DataAnnotations;

namespace Timoto.ViewModels
{
    public class CardCreateVM
    {
        [Required]
        [Display(Name = "Card Holder Name")]
        public string HolderName { get; set; }

        [Required]
        [CreditCard]
        [Display(Name = "Card Number")]
        [MaxLength(16)]
        public string CardNumber { get; set; }

        [Required]
        [Range(1, 12)]
        [Display(Name = "Expiry Month")]
        public int ExpiryMonth { get; set; }

        [Required]
        [Range(2024, 2100)]
        [Display(Name = "Expiry Year")]
        public int ExpiryYear { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 3)]
        public string CVV { get; set; }
    }
}
