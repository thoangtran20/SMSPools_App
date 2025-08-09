using Microsoft.AspNetCore.Mvc;
using SMSPools_App.Models;
using SMSPools_App.Services;
using SMSPools_App.Services.Interfaces;

namespace SMSPools_App.Controllers
{
    public class RentController : Controller
    {
        private readonly ISmsApiService _smsApiService;
        private readonly SmsAccountService _accountService;
        public static List<RentNumberViewModel> RentNumbers = new List<RentNumberViewModel>();
        private static Dictionary<string, List<SmsOrderResponse>> _userOrders = new();

        public RentController(ISmsApiService smsApiService, IWebHostEnvironment env)
        {
            _smsApiService = smsApiService;
            _accountService = new SmsAccountService(env);
        }

        public async Task<IActionResult> Rent(string id, string userToken)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
                return NotFound();

            var orders = await _smsApiService.GetAlRentNumbersAsync(account.ApiKey, userToken);

            var viewModel = new RentNumberViewModel
            {
                Account = account,
                Order = null,
                Orders = orders
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RentNumber(string id, string userToken)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }

            var order = await _smsApiService.RentNumberAsync(account.ApiKey, userToken);
            Console.WriteLine("RECEIVED userToken: " + userToken);

            if (order == null || !string.IsNullOrEmpty(order.ErrorMessage))
            {
                return Json(new { success = false, message = order?.ErrorMessage ?? "Failed to rent number" });
            }

            return Json(new
            {
                success = true,
                phoneNumber = order.PhoneNumber,
                orderId = order.OrderId,
                country = order.Country,
                service = order.Service,
                userToken = userToken
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetOtp(string id, string orderId)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }
            var otp = await _smsApiService.GetOtpAsync(orderId, account.ApiKey);
            if (otp == null)
            {
                return Json(new { success = false });
            }
            return Json(new { success = true, otp });
        }

        [HttpPost]
        public async Task<IActionResult> ResendCode(string id, string orderId)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }
            var success = await _smsApiService.ResendCodeAsync(orderId, account.ApiKey);
            if (!success)
                return Json(new { success = false });

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> GetAllRentedNumbers(string id, string userToken)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }
            var orders = await _smsApiService.GetAlRentNumbersAsync(account.ApiKey, userToken) ?? new List<SmsOrderResponse>();
            var userOrders = orders.Where(o => o.UserToken == userToken).ToList();

            int count = userOrders.Count;

            if (orders == null)
            {
                return Json(new { success = false, message = "Failed to retrieve orders" });
            }
            return Json(new { success = true, orders = userOrders });
        }

        [HttpPost]
        public async Task<IActionResult> GetAllOrders(string id)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }
            var orders = await _smsApiService.GetAllOrdersAsync(account.ApiKey) ?? new List<SmsOrderResponse>();

            if (orders == null)
            {
                return Json(new { success = false, message = "Failed to retrieve orders" });
            }
            return Json(new { success = true, orders = orders });
        }

        [HttpPost]
        public async Task<IActionResult> RefundOrder(string id, string orderId)
        {
            Console.WriteLine($"RefundOrder called with orderId={orderId}");

            if (string.IsNullOrEmpty(orderId))
            {
                return Json(new { status = false, message = "OrderId is required" });
            }

            var account = _accountService.GetAccountById(id);
            if (account == null)
            {
                return Json(new { success = false, message = "Account not found" });
            }
            bool success = await _smsApiService.RefundOrderAsync(orderId, account.ApiKey);

            if (success)
            {
                var itemToRemove = RentNumbers.FirstOrDefault(x => x.Order.OrderId == orderId);
                if (itemToRemove != null)
                {
                    RentNumbers.Remove(itemToRemove);
                }
                return Json(new { success = true, message = "The order has been cancelled, and you have been refunded 0.14 dollars." });
            }
            else
            {
                return Json(new { success = false, message = "Your order cannot be cancelled yet, please try again later." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelAllOrders(string id)
        {
            var account = _accountService.GetAccountById(id);

			var orders = await _smsApiService.GetAllOrdersAsync(account.ApiKey);

			if (!orders.All(o => o.TimeLeft < 500))
			{
				return Json(new { success = false, message = "Some orders have more than 5 minutes remaining. Cancel not allowed." });
			}

			var success = await _smsApiService.CancelAllOrdersAsync(account.ApiKey);

			return Json(new
            {
                success = success,
                message = success ? "All orders cancelled successfully." : "All orders have already been cancelled."
            });
        }

        public IActionResult Index(string id)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null) return NotFound();

            var viewModel = new RentNumberViewModel
            {
                Account = account,
                Order = null
            };

            return View("Rent", viewModel);
        }
    }
}
