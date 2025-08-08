using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMSPools_App.Data;
using SMSPools_App.Models;
using System.Security.Claims;

namespace SMSPools_App.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Register(string userToken)
        {
            if (string.IsNullOrEmpty(userToken))
            {
                userToken = Guid.NewGuid().ToString();
            }
            ViewData["UserToken"] = userToken;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string userToken)
        {
            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                ModelState.AddModelError("", "Email is invalid!!!");
                ViewData["UserToken"] = userToken;
                return View();
            }

            var token = new Random().Next(100000, 999999).ToString();

            var existingEntry = _context.UserTokenEntries.FirstOrDefault(u => u.UserToken == userToken);
            if (existingEntry == null)
            {
                var newEntry = new UserTokenEntry
                {
                    UserToken = userToken,
                    Email = email,
                    IsRegistered = false,
                    CreatedAt = DateTime.UtcNow,
                    Token = token,
                    Expiration = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false
                };
                _context.UserTokenEntries.Add(newEntry);
            }
            else
            {
                existingEntry.Email = email;
                existingEntry.CreatedAt = DateTime.UtcNow;
                existingEntry.Token = token;
                existingEntry.Expiration = DateTime.UtcNow.AddMinutes(10);
                existingEntry.IsUsed = false;
            }

            await _context.SaveChangesAsync();

            try
            {
                await _emailSender.SendEmailAsync(email, "Your login code", $"Your code is: {token}");
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Failed to send email: {ex.Message}";
            }

            ViewBag.Message = "Your login code has been sent to you. If you don't see it in your main inbox, please check your Spam or Junk folder.";
            return RedirectToAction("Login", new { userToken = userToken, email = email });
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        [HttpGet]
        public IActionResult Login(string userToken, string email, string accId)
        {
            if (string.IsNullOrEmpty(userToken))
            {
                userToken = Guid.NewGuid().ToString();
            }
            ViewData["UserToken"] = userToken;
            ViewData["Email"] = email;
            ViewData["AccId"] = accId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string token, string userToken, string accId)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "Email and login code are not empty");
                ViewData["Email"] = email;
                return View();
            }

            var tokenEntry = await _context.UserTokenEntries
                .Where(t => t.Email == email
                    && t.Token == token
                    && !t.IsUsed
                    && t.Expiration > DateTime.UtcNow
                    && t.UserToken == userToken)
                .FirstOrDefaultAsync();

            if (tokenEntry == null)
            {
                ModelState.AddModelError("", "Login code is not valid and expired");
                ViewData["Email"] = email;
                return View();
            }

            tokenEntry.IsUsed = true;
            tokenEntry.IsRegistered = true;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(accId))
            {
                return RedirectToAction("Rent", "Home", new { id = accId, userToken = userToken });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> ResendCode(string email, string userToken)
        {
            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                return Json(new { success = false, message = "Email is not valid" });
            }

            var token = new Random().Next(100000, 999999).ToString();

            var entry = _context.UserTokenEntries.FirstOrDefault(u => u.UserToken == userToken);
            if (entry != null)
            {
                entry.Token = token;
                entry.Email = email;
                entry.CreatedAt = DateTime.UtcNow;
                entry.Expiration = DateTime.UtcNow.AddMinutes(10);
                entry.IsUsed = false;
                entry.IsRegistered = true;
            }
            else
            {
                var newEntry = new UserTokenEntry
                {
                    UserToken = userToken,
                    Email = email,
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    Expiration = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false,
                    IsRegistered = true
                };
                _context.UserTokenEntries.Add(newEntry);
            }

            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(email, "Your login code", $"Your code is: {token}");

            return Json(new { success = true, message = "New code has been seen to your email" });
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
