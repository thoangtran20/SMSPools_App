using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSPools_App.Data;
using SMSPools_App.Models;
using SMSPools_App.Services;
using SMSPools_App.Services.Interfaces;

namespace SMSPools_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly SmsAccountService _smsAccountService;
        private readonly ISmsApiService _smsApiService;
        private readonly ApplicationDbContext _context;

        public HomeController(IWebHostEnvironment env, ISmsApiService smsApiService, ApplicationDbContext context)
        {
            _smsAccountService = new SmsAccountService(env);
            _smsApiService = smsApiService;
            _context = context;
        }

        [HttpGet]
        public IActionResult CheckUserTokenRegistered(string userToken)
        {
            if (string.IsNullOrEmpty(userToken))
                return Json(new { registered = false });

            var entry = _context.UserTokenEntries
                .FirstOrDefault(u => u.UserToken == userToken && u.IsRegistered);

            bool registered = entry != null;

            bool isAuthenticated = User.Identity.IsAuthenticated;

            return Json(new { registered, isAuthenticated });
        }

        public IActionResult Index()
        {
            var accounts = _smsAccountService.GetAllAccounts();
            return View(accounts);
        }

        [Authorize]
        public async Task<IActionResult> Rent(string id, string userToken)
        {
            var account = _smsAccountService.GetAccountById(id);
            if (account == null)
            {
                return NotFound();
            }

            var orders = await _smsApiService.GetAlRentNumbersAsync(account.ApiKey, userToken);

            var viewModel = new RentNumberViewModel
            {
                Account = account,
                Order = null,
                Orders = orders
            };

            return View("Rent", viewModel);
        }
    }
}
