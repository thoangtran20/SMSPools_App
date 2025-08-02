using Microsoft.AspNetCore.Mvc;
using SMSPools_App.Services;
using SMSPools_App.Services.Interfaces;

namespace SMSPools_App.Controllers
{
    public class RentController : Controller
    {
        private readonly ISmsApiService _smsApiService;
        private readonly SmsAccountService _accountService;

        public RentController(ISmsApiService smsApiService, IWebHostEnvironment env)
        {
            _smsApiService = smsApiService;
            _accountService = new SmsAccountService(env);
        }

        [HttpPost]
        public async Task<IActionResult> RentNumber(string id)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }

            var order = await _smsApiService.RentNumberAsync(account.ApiKey);
            if (order == null)
            {
                return Json(new { success = false, message = "Failed to rent number" });
            }
            return Json(new
            {
                success = true,
                phoneNumber = order.PhoneNumber,
                orderCode = order.OrderCode,
                code = order.Code,
                country = order.Country,
                service = order.Service,
                status = order.Status
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetOtp(string id, string orderCode)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }
			var otp = await _smsApiService.GetOtpAsync(orderCode, account.ApiKey);
			if (otp == null)
			{
				return Json(new { success = false });
			}
			return Json(new { success = true, otp });
        }

        [HttpPost]
        public async Task<IActionResult> ResendCode(string id, string orderCode)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }
            var success = await _smsApiService.ResendCodeAsync(orderCode, account.ApiKey);
			if (!success)
				return Json(new { success = false });

			return Json(new { success = true });
		}

        public IActionResult Index(string id)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null) return NotFound();
            return View(account);
        }
    }
}
