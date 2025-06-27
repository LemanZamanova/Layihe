using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Timoto.ViewModels;

public class ContactController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactVM vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }


        var mail = new MailMessage();
        mail.To.Add("zamanovaleman11@gmail.com");
        mail.From = new MailAddress(vm.Email);
        mail.Subject = vm.Subject;
        mail.Body = $"From: {vm.FullName} <{vm.Email}>\n\nMessage:\n{vm.Message}";

        mail.IsBodyHtml = false;

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new System.Net.NetworkCredential("zamanovaleman11@gmail.com", "kevbyynzhcwtxjwj")
        };

        try
        {
            await smtp.SendMailAsync(mail);
            TempData["Success"] = "Your message has been sent successfully!";
            return RedirectToAction("Index");
        }
        catch
        {
            ModelState.AddModelError("", "Sorry, we couldn’t send your message. Try again later.");
            return View(vm);
        }
    }
}
