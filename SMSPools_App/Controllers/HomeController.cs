using Microsoft.AspNetCore.Mvc;
using SMSPools_App.Models;
using SMSPools_App.Services;
using SMSPools_App.Services.Interfaces;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SMSPools_App.Controllers
{
    public class HomeController : Controller
    {
		private readonly SmsAccountService _smsAccountService;
		private readonly ISmsApiService _smsApiService;


		public HomeController(IWebHostEnvironment env, ISmsApiService smsApiService)
		{
			_smsAccountService = new SmsAccountService(env);
			_smsApiService = smsApiService;
		}
		

		public IActionResult Index()
		{
			var accounts = _smsAccountService.GetAllAccounts();
			return View(accounts);
		}

		public async Task<IActionResult> Rent(string id)
		{
			var account = _smsAccountService.GetAccountById(id);
			if (account == null)
			{
				return NotFound();
			}

			var orders = await _smsApiService.GetAlRentNumbersAsync(account.ApiKey);

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
