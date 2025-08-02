using Microsoft.AspNetCore.Mvc;
using SMSPools_App.Models;
using SMSPools_App.Services;
using System.Diagnostics;

namespace SMSPools_App.Controllers
{
    public class HomeController : Controller
    {
		private readonly SmsAccountService _smsAccountService;

		public HomeController(IWebHostEnvironment env)
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
