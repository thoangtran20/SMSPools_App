using SMSPools_App.Models;
using SMSPools_App.Services.Interfaces;
using System.Text.Json;

namespace SMSPools_App.Services
{
    public class SmsApiService : ISmsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly static Dictionary<string, string> _orderUserTokens = new();

		private OrderTokenStorePerAccount GetTokenStore(string apiKey, string userToken)
		{
			return new OrderTokenStorePerAccount(apiKey, userToken);
		}


		public SmsApiService()
        {
            //_tokenStore = tokenStore;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.smspool.net/");
        }


        public async Task<SmsOrderResponse?> RentNumberAsync(string apiKey, string userToken)
        {
            var requestUrl = "https://api.smspool.net/purchase/sms";

			var tokenStore = GetTokenStore(apiKey, userToken);

			var parameters = new Dictionary<string, string>
            {
                { "key", apiKey },
                { "country", "1" },
                { "service", "828" },
                { "pricing_option", "1" },
                { "quantity", "1" },
                { "areacode", "" },
                { "exclude", "" },
                { "create_token", "0" },
                { "activation_type", "SMS" }
            };

            const int maxAttempts = 5;
            for (int i = 0; i < maxAttempts; i++)
            {
				using var content = new FormUrlEncodedContent(parameters);

				var response = await _httpClient.PostAsync(requestUrl, content);
				var json = await response.Content.ReadAsStringAsync();
				Console.WriteLine("JSON RESPONSE: " + json);

				if (!response.IsSuccessStatusCode)
				{
					return null;
				}
				try
				{
					var order = JsonSerializer.Deserialize<SmsOrderResponse>(json);
					if (order != null)
					{
                        if (PhoneNumberHelper.IsBlockedNumber(order.PhoneNumber))
                        {
							Console.WriteLine($"Blocked number detected: {order.PhoneNumber}. Retrying...");
							continue;
						}

						var key = !string.IsNullOrEmpty(order.OrderCode) ? order.OrderCode : order.OrderId;
						if (!string.IsNullOrEmpty(key))
						{
							tokenStore.Save(key, userToken);
							order.UserToken = userToken;
						}
						return order;
					}
				}
				catch
				{
					var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json);
					var firstOrder = orders?.FirstOrDefault();
					if (firstOrder != null)
					{
						if (PhoneNumberHelper.IsBlockedNumber(firstOrder.PhoneNumber))
						{
							Console.WriteLine($"Blocked number detected (from list): {firstOrder.PhoneNumber}. Retrying...");
							continue;
						}

						tokenStore.Save(firstOrder.OrderId, userToken);
						firstOrder.UserToken = userToken;
						return firstOrder;
					}
				}
			}    
            return null;
        }

        public async Task<string?> GetOtpAsync(string orderId, string apiKey)
        {
            var url = "https://api.smspool.net/sms/check";

            var parameters = new Dictionary<string, string>
            {
                { "orderid", orderId },
                { "key", apiKey }
            };

            Console.WriteLine($"[GetOtpAsync] orderId: {orderId}");

            using var httpClient = new HttpClient();
            using var content = new FormUrlEncodedContent(parameters);

            var response = await httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("OTP RESPONSE: " + json);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Status code: " + response.StatusCode);
                Console.WriteLine("Response body: " + json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("sms", out var smsElement))
            {
                var otp = smsElement.GetString();
                return !string.IsNullOrEmpty(otp) ? otp : null;
            }

            return null;
        }

        public async Task<bool> ResendCodeAsync(string orderId, string apiKey)
        {
            var url = "https://api.smspool.net/sms/resend";

            var parameters = new Dictionary<string, string>
            {
                { "orderid", orderId },
                { "key", apiKey }
            };


            using var httpClient = new HttpClient();
            using var content = new FormUrlEncodedContent(parameters);

            var response = await httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("RESEND RESPONSE: " + json);

            return response.IsSuccessStatusCode && json.Contains("\"success\":1");

        }

        public async Task<List<SmsOrderResponse>> GetAlRentNumbersAsync(string apiKey, string userToken)
        {
            var url = "https://api.smspool.net/request/orders_new";
			var tokenStore = GetTokenStore(apiKey, userToken);

			var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "key", apiKey },
                { "country", "1" },
                { "service", "828" }
            });

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(url, content);

             if (!response.IsSuccessStatusCode)
                return new List<SmsOrderResponse>();

			var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("RENTED NUMBERS JSON: " + json);

            var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json);
			if (orders != null)
			{
				foreach (var order in orders)
				{
					var key = !string.IsNullOrEmpty(order.OrderCode) ? order.OrderCode : order.OrderId;
					if (!string.IsNullOrEmpty(key))
					{
						var token = tokenStore.GetUserToken(key);
						if (!string.IsNullOrEmpty(token))
						{
							order.UserToken = token;
						}
					}
				}
			}
			return orders?.Where(o => o.UserToken == userToken).ToList() ?? new List<SmsOrderResponse>();
		}
        public async Task<bool> RefundOrderAsync(string orderId, string apiKey)
        {
            var url = "https://api.smspool.net/sms/cancel";

            var parameters = new Dictionary<string, string>
            {
                { "orderid", orderId },
                { "key", apiKey }
            };

            using var httpClient = new HttpClient();
            using var content = new FormUrlEncodedContent(parameters);

            var response = await httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("REFUND JSON RESPONSE: " + json);

            if (!response.IsSuccessStatusCode)
                return false;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    Console.WriteLine("Refund message: " + messageElement.ToString());
                }

                if (doc.RootElement.TryGetProperty("success", out var successElement))
                {
                    var success = successElement.ToString() == "1";
                    if (!success)
                    {
                        Console.WriteLine("Refund failed with message: " + messageElement.ToString());
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RefundOrderAsync ERROR: " + ex.Message);
            }

            return false;
        }

    }
}
