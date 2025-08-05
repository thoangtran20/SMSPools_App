using System.Text.Json;

namespace SMSPools_App.Services
{
    public class OrderTokenStore
    {
		private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "order_tokens.json");
		private static Dictionary<string, string> _orderTokens;
		private static readonly object _lock = new();

		static OrderTokenStore()
		{
			Load();
		}

		private static void Load()
		{
			if (File.Exists(FilePath))
			{
				try
				{
					var json = File.ReadAllText(FilePath);
					_orderTokens = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();				}
				catch
				{
					_orderTokens = new Dictionary<string, string>();
				}
			}
			else
			{
				_orderTokens = new Dictionary<string, string>();
			}
		}
		private static void SaveToFile()
		{
			try
			{
				var json = JsonSerializer.Serialize(_orderTokens, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(FilePath, json);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error saving order tokens: {ex.Message}");
			}
		}

		public static void Save(string orderId, string userToken)
		{
			_orderTokens[orderId] = userToken;
			SaveToFile();
		}

		public static string? GetUserToken(string orderId)
		{
			Load();
			return _orderTokens.TryGetValue(orderId, out var token) ? token : null;
		}
	}
}
