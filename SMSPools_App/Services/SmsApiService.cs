using SMSPools_App.Models;
using SMSPools_App.Services.Interfaces;
using System.Text.Json;
using static System.Net.WebRequestMethods;

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


        public async Task<SmsOrderResponse?> RentNumberAsync(string apiKey)
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

			using var httpClient = new HttpClient();

			//var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
			//request.Headers.Add("User-Agent", "Mozilla/5.0");

			using var content = new FormUrlEncodedContent(parameters);

			var response = await httpClient.PostAsync(requestUrl, content);
			var json = await response.Content.ReadAsStringAsync();
			Console.WriteLine("JSON RESPONSE: " + json);

			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			try
			{
				var order = JsonSerializer.Deserialize<SmsOrderResponse>(json);
				return order;
			}
			catch
			{
				var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json);
				return orders?.FirstOrDefault();
			}
		}

		public async Task<string?> GetOtpAsync(string orderCode, string apiKey)
		{
			var url = "https://api.smspool.net/sms/check";

			var parameters = new Dictionary<string, string>
			{
				{ "orderid", orderCode },
				{ "key", apiKey }
			};

			Console.WriteLine($"[GetOtpAsync] orderCode: {orderCode}");

			using var httpClient = new HttpClient();
			using var content = new FormUrlEncodedContent(parameters);

			var response = await httpClient.PostAsync(url, content);
			var json = await response.Content.ReadAsStringAsync();

			Console.WriteLine("OTP RESPONSE: " + json);

			if (!response.IsSuccessStatusCode)
				Console.WriteLine("Status code: " + response.StatusCode);
				Console.WriteLine("Response body: " + json);
				return null;

			using var doc = JsonDocument.Parse(json);
			if (doc.RootElement.TryGetProperty("sms", out var smsElement))
			{
				var otp = smsElement.GetString();
				return !string.IsNullOrEmpty(otp) ? otp : null;
			}

			return null;
		}

		public async Task<bool> ResendCodeAsync(string orderCode, string apiKey)
        {
			var url = "https://api.smspool.net/sms/resend";

			var parameters = new Dictionary<string, string>
			{
				{ "orderid", orderCode },
				{ "key", apiKey }
			};


			using var httpClient = new HttpClient();
			using var content = new FormUrlEncodedContent(parameters);

			var response = await httpClient.PostAsync(url, content);
			var json = await response.Content.ReadAsStringAsync();

			Console.WriteLine("RESEND RESPONSE: " + json);

			return response.IsSuccessStatusCode && json.Contains("\"success\":1");

		}
	}
}
