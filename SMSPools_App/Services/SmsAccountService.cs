using SMSPools_App.Models;
using System.Text.Json;

namespace SMSPools_App.Services
{
	public class SmsAccountService
	{
		private readonly IWebHostEnvironment _env;
		private readonly string _jsonFilePath;

		public SmsAccountService(IWebHostEnvironment env)
		{
			_env = env;
			_jsonFilePath = Path.Combine(_env.ContentRootPath, "Data", "accounts.json");
		}


		public List<SmsAccountConfig> GetAllAccounts()
		{
			var path = _jsonFilePath;
			Console.WriteLine("JSON path: " + path);

			if (!File.Exists(_jsonFilePath))
				return new List<SmsAccountConfig>();

			var json = File.ReadAllText(_jsonFilePath);
			return JsonSerializer.Deserialize<List<SmsAccountConfig>>(json) ?? new List<SmsAccountConfig>();
		}

		public SmsAccountConfig? GetAccountById(string id)
		{
			return GetAllAccounts().FirstOrDefault(a => a.Id == id);
		}
	}
}
