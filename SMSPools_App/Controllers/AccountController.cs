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
        private readonly IEmailSender _emailSender;

        public AccountController(IEmailSender emailSender)
        {
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
				ModelState.AddModelError("", "Invalid user token");
				return View();
			}
			ViewData["UserToken"] = userToken;
			ViewData["AccId"] = accId ?? "";
			return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
