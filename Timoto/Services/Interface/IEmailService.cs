using Timoto.Models;

namespace Timoto.Services.Interface
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendBookingConfirmationAsync(string toEmail, Booking booking);
        Task SendEmailAsync(string email, string v1, string v2, object pdfAttachmentPath);
    }
}
