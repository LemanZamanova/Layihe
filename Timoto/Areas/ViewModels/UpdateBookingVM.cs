using System.ComponentModel.DataAnnotations;
using Timoto.Utilities.Enums;

namespace Timoto.Areas.ViewModels
{
    public class UpdateBookingVM
    {
        public BookingStatus Status { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(0, 999999)]
        public decimal TotalAmount { get; set; }

        [Range(0, 999999)]
        public decimal? LatePenaltyAmount { get; set; }
    }
}
