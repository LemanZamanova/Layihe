using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Timoto.Models;
using Timoto.Services.Interface;
using Timoto.Utilities.Enums;
using Timoto.ViewModels;
using Timoto.ViewModels.Users;


namespace Timoto.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "This email is already in use.");
                return View(model);
            }

            var code = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("RegisterModel", JsonConvert.SerializeObject(model));
            HttpContext.Session.SetString("Code", code);
            HttpContext.Session.SetString("CodeCreatedAt", DateTime.UtcNow.ToString("O"));

            string body = $"<p>Hello {model.Name},</p><p>To complete your registration, please enter this verification code: <strong>{code}</strong></p>";
            await _emailService.SendEmailAsync(model.Email, "Timoto Email Verification Code", body);

            return RedirectToAction("VerifyCode");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            AppUser user = await _userManager.FindByNameAsync(model.UserNameOrEmail) ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Please verify your email before logging in.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Your account is locked due to multiple failed login attempts. Please try again later.");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> CreateRoles()
        {
            foreach (UserRole role in Enum.GetValues(typeof(UserRole)))
            {
                if (!await _roleManager.RoleExistsAsync(role.ToString()))
                    await _roleManager.CreateAsync(new IdentityRole(role.ToString()));
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult VerifyCode()
        {
            TempData.Keep();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(string inputCode)
        {
            string? sentCode = HttpContext.Session.GetString("Code");
            string? codeTimeStr = HttpContext.Session.GetString("CodeCreatedAt");
            string? registerJson = HttpContext.Session.GetString("RegisterModel");

            if (sentCode == null || codeTimeStr == null || registerJson == null)
            {
                TempData["ErrorMessage"] = "Session expired. Please register again.";
                return RedirectToAction("Register");
            }

            if (!DateTime.TryParse(codeTimeStr, out var codeCreatedAt))
            {
                TempData["ErrorMessage"] = "Code timestamp is invalid.";
                return RedirectToAction("Register");
            }

            var now = DateTime.UtcNow;
            if ((now - codeCreatedAt).TotalMinutes > 1)
            {
                TempData["ErrorMessage"] = "The verification code has expired. Please register again.";
                return RedirectToAction("Register");
            }

            if (inputCode != sentCode)
            {
                ModelState.AddModelError("", "Incorrect verification code.");
                return View();
            }

            var model = JsonConvert.DeserializeObject<RegisterVM>(registerJson);

            AppUser user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                Surname = model.Surname,
                Phone = model.Phone,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the user.";
                return RedirectToAction("Register");
            }

            await _userManager.AddToRoleAsync(user, UserRole.Member.ToString());
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["WelcomeMessage"] = $"Welcome, {user.Name}! Your registration was successful.";

            HttpContext.Session.Remove("RegisterModel");
            HttpContext.Session.Remove("Code");
            HttpContext.Session.Remove("CodeCreatedAt");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCode()
        {
            string? registerJson = HttpContext.Session.GetString("RegisterModel");

            if (string.IsNullOrWhiteSpace(registerJson))
                return RedirectToAction("Register");

            RegisterVM? model;

            try
            {
                model = JsonConvert.DeserializeObject<RegisterVM>(registerJson);
                if (model == null)
                    return RedirectToAction("Register");
            }
            catch
            {
                return RedirectToAction("Register");
            }

            var newCode = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("Code", newCode);
            HttpContext.Session.SetString("CodeCreatedAt", DateTime.UtcNow.ToString("O"));

            string body = $"<p>Hello {model.Name},</p><p>Your new verification code is: <strong>{newCode}</strong></p>";

            await _emailService.SendEmailAsync(model.Email, "Timoto - New Verification Code", body);

            TempData["InfoMessage"] = "A new verification code has been sent to your email address.";

            return RedirectToAction("VerifyCode");
        }

        [HttpGet]
        public IActionResult ForgotPassword(string? returnUrl = null)
        {
            return View(new ForgotPasswordVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {

            if (!ModelState.IsValid)
                return View(model);

            if (User.Identity.IsAuthenticated)
            {
                var loggedInUser = await _userManager.GetUserAsync(User);

                if (loggedInUser != null && model.Email.ToLower() != loggedInUser.Email.ToLower())
                {
                    ModelState.AddModelError("Email", "Please enter your registered email address.");
                    return View(model);
                }
            }


            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "No user found with this email address.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);


            var resetLink = Url.Action("ResetPassword", "Account", new
            {
                token = token,
                email = user.Email,
                returnUrl = model.ReturnUrl
            }, Request.Scheme);

            if (resetLink == null)
                return BadRequest("Reset link generation failed.");

            TempData["EmailSentTo"] = model.Email;
            TempData["ReturnUrl"] = model.ReturnUrl;


            string safeLink = HtmlEncoder.Default.Encode(resetLink);

            await _emailService.SendEmailAsync(user.Email, "Password Reset",
                $"Click <a href='{safeLink}'>here</a> to reset your password.");

            ViewBag.Success = "Reset link was sent to your email address.";
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction("Error", "Home");

            ViewBag.Error = TempData["Error"];

            return View(new ResetPasswordVM
            {
                Token = token,
                Email = email,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction("ForgotPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();


                if (errors.Count == 1 && errors[0].ToLower().Contains("invalid token"))
                {
                    TempData["Error"] = "Invalid token.";
                    return RedirectToAction("ResetPassword", new
                    {
                        token = model.Token,
                        email = model.Email,
                        returnUrl = model.ReturnUrl
                    });
                }


                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["Success"] = "Password successfully reset.";
            TempData["ReturnUrl"] = model.ReturnUrl;

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            ViewBag.ReturnUrl = TempData["ReturnUrl"];
            return View();
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

    }
}
