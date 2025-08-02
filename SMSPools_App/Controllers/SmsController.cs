using Microsoft.AspNetCore.Mvc;
using SMSPools_App.Services;

namespace SMSPools_App.Controllers
{
	public class SmsController : Controller
	{
		private readonly SmsAccountService _smsAccountService;

		public SmsController(IWebHostEnvironment env)
		{
			_smsAccountService = new SmsAccountService(env);
		}


		public IActionResult Index()
		{
			var accounts = _smsAccountService.GetAllAccounts();
			return View(accounts);
		}
			
		public IActionResult Rent(string id)
		{
			var account = _smsAccountService.GetAccountById(id);
			if (account == null)
			{
				return NotFound();
			}
			return View("Rent", account);
		}
	}
}
