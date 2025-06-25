using Timoto.Models;

namespace Timoto.Services.Interface
{
    public interface IPdfService
    {
        Task<string> GenerateBookingPdfAsync(Booking booking);
    }
}
