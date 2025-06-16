using Timoto.Models;

namespace Timoto.Services.Interface
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendBookingConfirmationAsync(string toEmail, Booking booking);
    }
}
