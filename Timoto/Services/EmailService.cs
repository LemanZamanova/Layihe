using System.Net;
using System.Net.Mail;
using Timoto.Models;
using Timoto.Services.Interface;

namespace Timoto.Services
{
    public class EmailService : IEmailService
    {

        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var fromEmail = _config["EmailSettings:Email"];
            var password = _config["EmailSettings:Password"];

            SmtpClient smtpClient = new SmtpClient(_config["EmailSettings:Host"])
            {
                Port = int.Parse(_config["EmailSettings:Port"]),
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Timoto"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);


            await smtpClient.SendMailAsync(mailMessage);
        }
        public async Task SendBookingConfirmationAsync(string toEmail, Booking booking)
        {
            string subject = "Booking Confirmation - Timoto";
            string body = $@"
        <p>Hello {booking.Name},</p>
        <p>Your car booking has been confirmed:</p>
        <ul>
            <li>Car ID: {booking.CarId}</li>
            <li>Start: {booking.StartDate}</li>
            <li>End: {booking.EndDate}</li>
        </ul>
        <p>Thank you for choosing us!</p>";

            await SendEmailAsync(toEmail, subject, body);
        }
        public async Task SendEmailAsync(string toEmail, string subject, string message, object attachment)
        {
            var fromEmail = _config["EmailSettings:Email"];
            var password = _config["EmailSettings:Password"];

            SmtpClient smtpClient = new SmtpClient(_config["EmailSettings:Host"])
            {
                Port = int.Parse(_config["EmailSettings:Port"]),
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Timoto"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            // Əlavə varsa əlavə et
            if (attachment is string filePath && System.IO.File.Exists(filePath))
            {
                mailMessage.Attachments.Add(new Attachment(filePath));
            }

            await smtpClient.SendMailAsync(mailMessage);
        }

    }
}
