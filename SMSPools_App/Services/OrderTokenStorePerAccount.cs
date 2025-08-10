using System.Text.Json;

namespace SMSPools_App.Services
{
    public class OrderTokenStorePerAccount
    {
        private readonly string _filePath;
        private Dictionary<string, string> _orderTokens = new Dictionary<string, string>();
        private readonly object _lock = new();

        public OrderTokenStorePerAccount(string apiKey, string userToken)
        {
            if (string.IsNullOrWhiteSpace(userToken))
                throw new ArgumentNullException(nameof(userToken), "userToken cannot be null or empty.");

            var baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "order_tokens", userToken); Directory.CreateDirectory(baseFolder);

            var fileName = $"order_tokens_{apiKey}.json";
            _filePath = Path.Combine(baseFolder, fileName);

            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    _orderTokens = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
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
        private void SaveToFile()
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_orderTokens, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }
        public void Remove(string orderId)
        {
            if (_orderTokens.Remove(orderId))
            {
                SaveToFile();
            }

        }

        public void Save(string orderId, string userToken)
        {
            _orderTokens[orderId] = userToken;
            SaveToFile();
        }

        public string? GetUserToken(string orderId)
        {
            Load();
            return _orderTokens.TryGetValue(orderId, out var token) ? token : null;
        }
    }
}
