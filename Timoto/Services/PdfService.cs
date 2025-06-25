using iTextSharp.text;
using iTextSharp.text.pdf;
using Timoto.Models;
using Timoto.Services.Interface;

namespace Timoto.Services
{
    public class PdfService : IPdfService
    {
        public async Task<string> GenerateBookingPdfAsync(Booking booking)
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdf");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, $"Booking_{booking.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var doc = new Document(PageSize.A4);
                var writer = PdfWriter.GetInstance(doc, fs);

                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                doc.Add(new Paragraph("Car Booking Confirmation", titleFont));
                doc.Add(new Paragraph($"Name: {booking.Name}", normalFont));
                doc.Add(new Paragraph($"Email: {booking.Email}", normalFont));
                doc.Add(new Paragraph($"Phone: {booking.Phone}", normalFont));
                doc.Add(new Paragraph($"Start Date: {booking.StartDate:dd MMM yyyy HH:mm}", normalFont));
                doc.Add(new Paragraph($"End Date: {booking.EndDate:dd MMM yyyy HH:mm}", normalFont));
                doc.Add(new Paragraph($"Generated at: {DateTime.Now:dd MMM yyyy HH:mm}", normalFont));

                doc.Add(new Paragraph("Terms and Conditions", titleFont));
                doc.Add(new Paragraph("1. The vehicle must be returned to the same location.", normalFont));
                doc.Add(new Paragraph("2. The customer is responsible for any damage or loss.", normalFont));
                doc.Add(new Paragraph("3. After the reserved period ends, $10/hour will be charged.", normalFont));
                doc.Add(new Paragraph("4. Early return does not entitle you to a refund.", normalFont));
                doc.Add(new Paragraph("5. Fuel level must match the level at pickup.", normalFont));

                doc.Close();
            }

            return filePath;
        }
    }
}
