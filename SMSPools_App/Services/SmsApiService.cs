using SMSPools_App.Models;
using SMSPools_App.Services.Interfaces;
using System.Text.Json;

namespace SMSPools_App.Services
{
    public class SmsApiService : ISmsApiService
    {
        private readonly HttpClient _httpClient;

        public SmsApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.smspool.net/");
        }


        public async Task<SmsOrderResponse?> RentNumberAsync(string apiKey, string userToken)
        {
            var requestUrl = "https://api.smspool.net/purchase/sms";

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

            //using var httpClient = new HttpClient();
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
                    order.UserToken = userToken;
                }
				Console.WriteLine("RETURNING userToken: " + order?.UserToken);
				return order;
            }
            catch
            {
                var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json);
				var firstOrder = orders?.FirstOrDefault();
				if (firstOrder != null)
				{
					firstOrder.UserToken = userToken;
				}
				return firstOrder;
            }
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

        public async Task<List<SmsOrderResponse>?> GetAlRentNumbersAsync(string apiKey, string userToken)
        {
            var url = "https://api.smspool.net/request/orders_new";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "key", apiKey },
                { "country", "1" },
                { "service", "828" }
            });

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"[UserToken: {userToken}] RENTED NUMBERS JSON: {json}");
			Console.WriteLine("RENTED NUMBERS JSON: " + json);

            var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json);

            return orders;
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
