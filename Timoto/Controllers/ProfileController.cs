using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Timoto.DAL;
using Timoto.Helper;
using Timoto.Models;
using Timoto.Services.Interface;

using Timoto.ViewModels;
namespace Timoto.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly SignInManager<AppUser> _signInManager;

        public ProfileController(AppDbContext context, UserManager<AppUser> userManager, IEmailService emailService, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var vm = new ProfileVM
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Phone = user.Phone,

                Bookings = await _context.Bookings
                    .Where(b => b.UserId == user.Id && !b.IsDeleted)
                    .Include(b => b.Car)
                    .ToListAsync(),

                FavoriteCars = await _context.FavoriteCars
                    .Where(f => f.UserId == user.Id)
                    .Include(f => f.Car)
                        .ThenInclude(c => c.CarImages)
                    .Select(f => f.Car)
                    .ToListAsync(),

                Cards = await _context.UserCards
                    .Where(c => c.UserId == user.Id && !c.IsDeleted)
                    .ToListAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> MyProfile(bool edit = false)
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var vm = new UpdateProfileVM
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Phone = user.Phone,
                Notifications = _context.Notifications
                 .Where(n => n.AppUserId == user.Id)
                  .OrderByDescending(n => n.CreatedAt)
                  .ToList()
            };
            ViewBag.ProfileImage = string.IsNullOrEmpty(user.ProfileImageUrl) ? "/assets/images/profile/default.jpg" : user.ProfileImageUrl;
            ViewBag.EditMode = edit;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(UpdateProfileVM model)
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid)
            {
                ViewBag.ProfileImage = string.IsNullOrEmpty(user.ProfileImageUrl) ? "/assets/images/profile/default.jpg" : user.ProfileImageUrl;
                var fallbackModel = new UpdateProfileVM
                {
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    Phone = user.Phone,
                    Notifications = _context.Notifications
                     .Where(n => n.AppUserId == user.Id)
                       .OrderByDescending(n => n.CreatedAt)
                      .ToList()
                };

                ModelState.OverrideFieldValuesWithModel(fallbackModel);

                return View(fallbackModel);
            }



            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Please enter your current password to update your profile.");
                ViewBag.ProfileImage = string.IsNullOrEmpty(user.ProfileImageUrl) ? "/assets/images/profile/default.jpg" : user.ProfileImageUrl;

                model.Notifications = _context.Notifications
                    .Where(n => n.AppUserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();

                return View(model);
            }


            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!isPasswordValid)
            {
                ModelState.AddModelError("CurrentPassword", "Incorrect password. Did you forget it?");
                ViewBag.ProfileImage = string.IsNullOrEmpty(user.ProfileImageUrl) ? "/assets/images/profile/default.jpg" : user.ProfileImageUrl;

                model.Notifications = _context.Notifications
                    .Where(n => n.AppUserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();
                return View(model);
            }




            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }
            }

            if (user.Email != model.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "This email address is already associated with another account.");
                    ViewBag.ProfileImage = string.IsNullOrEmpty(user.ProfileImageUrl) ? "/assets/images/profile/default.jpg" : user.ProfileImageUrl;

                    model.Notifications = _context.Notifications
                        .Where(n => n.AppUserId == user.Id)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();

                    return View(model);
                }


                var verificationCode = new Random().Next(100000, 999999).ToString();

                HttpContext.Session.SetString("VerifyEmailUserId", user.Id);
                HttpContext.Session.SetString("VerifyEmailNewAddress", model.Email);
                HttpContext.Session.SetString("EmailVerificationCode", verificationCode);
                HttpContext.Session.SetString("EmailVerificationSentTime", DateTime.UtcNow.ToString("O"));


                HttpContext.Session.SetString("Update_Name", model.Name);
                HttpContext.Session.SetString("Update_Surname", model.Surname);
                HttpContext.Session.SetString("Update_Phone", model.Phone);

                string body = $"<p>To confirm your new email, enter the following verification code:</p><h2>{verificationCode}</h2>";
                await _emailService.SendEmailAsync(model.Email, "Email Change Verification Code", body);

                return RedirectToAction("ChangeEmailVerify");
            }

            //update 
            user.Name = model.Name;
            user.Surname = model.Surname;
            user.Phone = model.Phone;

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            var notification = new Notification
            {
                AppUserId = user.Id,
                Message = "You updated your profile successfully.",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            ViewBag.SuccessMessage = "Your profile has been successfully updated!";
            ViewBag.ProfileImage = string.IsNullOrEmpty(user.ProfileImageUrl)
                   ? "/assets/images/profile/default.jpg"
                   : user.ProfileImageUrl;

            model.Notifications = _context.Notifications
             .Where(n => n.AppUserId == user.Id)
             .OrderByDescending(n => n.CreatedAt)
              .ToList();
            return View(model);
        }

        public IActionResult ChangeEmailVerify()
        {
            return View("ChangeEmail");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmailVerify(string inputCode)
        {
            var userId = HttpContext.Session.GetString("VerifyEmailUserId");
            var newEmail = HttpContext.Session.GetString("VerifyEmailNewAddress");
            var sentCode = HttpContext.Session.GetString("EmailVerificationCode");
            var codeSentTimeStr = HttpContext.Session.GetString("EmailVerificationSentTime");

            if (userId == null || newEmail == null || sentCode == null || codeSentTimeStr == null)
            {
                TempData["ErrorMessage"] = "Session expired.";
                return RedirectToAction("MyProfile");
            }

            if (!DateTime.TryParse(codeSentTimeStr, out var codeSentTime) || DateTime.UtcNow > codeSentTime.AddMinutes(1))
            {
                TempData["ErrorMessage"] = "Verification code expired.";
                return RedirectToAction("MyProfile");
            }

            if (inputCode != sentCode)
            {
                ModelState.AddModelError("", "Invalid verification code.");
                return View("ChangeEmail");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            user.Email = newEmail;
            user.UserName = newEmail;
            user.EmailConfirmed = true;

            user.Name = HttpContext.Session.GetString("Update_Name") ?? user.Name;
            user.Surname = HttpContext.Session.GetString("Update_Surname") ?? user.Surname;
            user.Phone = HttpContext.Session.GetString("Update_Phone") ?? user.Phone;

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            HttpContext.Session.Remove("VerifyEmailUserId");
            HttpContext.Session.Remove("VerifyEmailNewAddress");
            HttpContext.Session.Remove("EmailVerificationCode");
            HttpContext.Session.Remove("EmailVerificationSentTime");
            HttpContext.Session.Remove("Update_Name");
            HttpContext.Session.Remove("Update_Surname");
            HttpContext.Session.Remove("Update_Phone");

            TempData["Success"] = "Email updated successfully!";
            return RedirectToAction("MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendChangeEmailCode()
        {
            var email = HttpContext.Session.GetString("VerifyEmailNewAddress");
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Session expired.";
                return RedirectToAction("MyProfile");
            }

            var code = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("EmailVerificationCode", code);
            HttpContext.Session.SetString("EmailVerificationSentTime", DateTime.UtcNow.ToString("O"));

            string body = $"<p>Your new verification code is:</p><h2>{code}</h2>";
            await _emailService.SendEmailAsync(email, "Verification Code", body);

            TempData["InfoMessage"] = "A new code has been sent to your email.";
            return RedirectToAction("ChangeEmailVerify");
        }
        public async Task<IActionResult> ChangeEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || profileImage == null || profileImage.Length == 0)
                return RedirectToAction("MyProfile");


            string folderPath = Path.Combine("wwwroot", "uploads", "profiles");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{Guid.NewGuid()}_{profileImage.FileName}";
            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await profileImage.CopyToAsync(stream);
            }


            user.ProfileImageUrl = $"/uploads/profiles/{fileName}";
            await _userManager.UpdateAsync(user);

            return Redirect(Request.Headers["Referer"].ToString());

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                user.ProfileImageUrl = null;
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
            }

            return Redirect(Request.Headers["Referer"].ToString());

        }


        public async Task<IActionResult> Orders()
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var vm = new ProfileVM
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Phone = user.Phone,

                Bookings = await _context.Bookings
                    .Where(b => b.UserId == user.Id && !b.IsDeleted)
                    .Include(b => b.Car)
                    .ToListAsync(),

                Notifications = await _context.Notifications
                    .Where(n => n.AppUserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Favorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");



            var favoriteCars = await _context.FavoriteCars
                .Where(fc => fc.UserId == user.Id)
                .Include(fc => fc.Car)
                    .ThenInclude(c => c.CarImages)
                .Select(fc => fc.Car)
                .ToListAsync();

            var vm = new ProfileVM
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                FavoriteCars = favoriteCars
            };

            return View(vm);
        }
        public async Task<IActionResult> Cards()
        {

            var user = await _userManager.GetUserAsync(User);
            var cards = await _context.UserCards
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            var vm = new ProfileVM
            {
                Cards = cards,
                NewCard = new CardCreateVM()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCard(CardCreateVM newCard)
        {


            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");


            if (!ModelState.IsValid)
            {
                var cards = await _context.UserCards
                    .Where(c => c.UserId == currentUser.Id)
                    .ToListAsync();

                var vm = new ProfileVM
                {
                    Cards = cards,
                    NewCard = newCard
                };

                return View("Cards", vm);
            }

            var exists = await _context.UserCards
                .AnyAsync(c => c.UserId == currentUser.Id && c.CardNumber == newCard.CardNumber);

            if (exists)
            {
                ModelState.AddModelError("NewCard.CardNumber", "This card is already added.");

                var cards = await _context.UserCards
                    .Where(c => c.UserId == currentUser.Id)
                    .ToListAsync();

                var vm = new ProfileVM
                {
                    Cards = cards,
                    NewCard = newCard
                };

                return View("Cards", vm);
            }


            var card = new UserCard
            {
                CardHolderName = newCard.HolderName,
                CardNumber = newCard.CardNumber,
                ExpiryMonth = newCard.ExpiryMonth,
                ExpiryYear = newCard.ExpiryYear,
                CVV = newCard.CVV,
                UserId = currentUser.Id
            };

            _context.UserCards.Add(card);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Card successfully added!";
            return RedirectToAction("Cards");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var card = await _context.UserCards.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);

            if (card == null || card.UserId != user.Id)
            {
                return NotFound();
            }

            _context.UserCards.Remove(card);
            await _context.SaveChangesAsync();

            return RedirectToAction("Cards");
        }
        #region SetImage
        //private async Task SetProfileImageAsync()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user?.ProfileImageUrl != null)
        //    {
        //        // Əgər artıq "/uploads" ilə başlayırsa təkrar əlavə etmə
        //        ViewBag.ProfileImage = user.ProfileImageUrl.StartsWith("/uploads")
        //            ? user.ProfileImageUrl
        //            : "/uploads/profiles/" + user.ProfileImageUrl;
        //    }
        //    else
        //    {
        //        ViewBag.ProfileImage = "/assets/images/profile/default.jpg";
        //    }
        //}

        #endregion
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = _userManager.GetUserAsync(User).Result;

            if (user?.ProfileImageUrl != null)
            {
                ViewBag.ProfileImage = user.ProfileImageUrl.StartsWith("/uploads")
                    ? user.ProfileImageUrl
                    : "/uploads/profiles/" + user.ProfileImageUrl;
            }
            else
            {
                ViewBag.ProfileImage = "/assets/images/profile/default.jpg";
            }

            base.OnActionExecuting(context);
        }


    }
}