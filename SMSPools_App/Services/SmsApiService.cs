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
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.smspool.net/");
        }
        public async Task<SmsOrderResponse?> RentNumberAsync(string apiKey, string userToken)
        {
            const string requestUrl = "https://api.smspool.net/purchase/sms";
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

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(requestUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[Rent Attempt {attempt}] JSON RESPONSE: {json}");

                if (json.Contains("too many failed purchases"))
                    return new SmsOrderResponse { ErrorMessage = "Too many failed purchases. Try again in 6 hours." };

                if (json.Contains("Insufficient balance"))
                    return new SmsOrderResponse { ErrorMessage = "Insufficient balance. Please top up." };

                try
                {
                    var order = JsonSerializer.Deserialize<SmsOrderResponse>(json);
                    if (order != null && !string.IsNullOrEmpty(order.PhoneNumber))
                    {
                        bool isBlocked = PhoneNumberHelper.IsBlockedNumber(order.PhoneNumber);

                        if (isBlocked)
                        {
                            Console.WriteLine($"[REFUND] Blocked number detected: {order.PhoneNumber}");
                            if (!string.IsNullOrEmpty(order.OrderId))
                                await RefundOrderAsync(order.OrderId, apiKey);

                            await Task.Delay(700);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(order.OrderId))
                            tokenStore.Save(order.OrderId, userToken);

                        order.UserToken = userToken;
                        Console.WriteLine($"[SUCCESS] Got number {order.PhoneNumber} after {attempt} tries.");
                        return order;
                    }
                }
                catch
                {
                    try
                    {
                        var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json);
                        if (orders != null)
                        {
                            foreach (var ord in orders)
                            {
                                if (string.IsNullOrEmpty(ord.PhoneNumber))
                                {
                                    Console.WriteLine($"[SKIP] Empty phone number, OrderId={ord.OrderId}");
                                    continue;
                                }

                                if (PhoneNumberHelper.IsBlockedNumber(ord.PhoneNumber))
                                {
                                    Console.WriteLine($"[REFUND] Blocked number detected: {ord.PhoneNumber}");
                                    if (!string.IsNullOrEmpty(ord.OrderId))
                                        await RefundOrderAsync(ord.OrderId, apiKey);
                                    continue;
                                }

                                if (!string.IsNullOrEmpty(ord.OrderId))
                                    tokenStore.Save(ord.OrderId, userToken);

                                ord.UserToken = userToken;
                                Console.WriteLine($"[SUCCESS] Got number {ord.PhoneNumber} after {attempt} tries (list).");
                                return ord;
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"Parse error: {ex2.Message}");
                    }
                }
            }

            Console.WriteLine($"RentNumber failed after {maxAttempts} attempts.");
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

        public async Task<List<SmsOrderResponse>> GetAllRentNumbersAsync(string apiKey, string userToken)
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

            var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json) ?? new List<SmsOrderResponse>();

            orders = orders
                .Where(o => !string.IsNullOrWhiteSpace(o.EffectiveOrderId) && !string.IsNullOrWhiteSpace(o.PhoneNumber))
				.Where(o => !PhoneNumberHelper.IsBlockedNumber(o.PhoneNumber))
				.ToList();

            foreach (var order in orders)
            {
                var key = order.EffectiveOrderId;
                if (!string.IsNullOrEmpty(key))
                {
                    var token = tokenStore.GetUserToken(key);
                    if (!string.IsNullOrEmpty(token))
                        order.UserToken = token;
                }
            }
            return orders.Where(o => string.IsNullOrEmpty(o.UserToken) || o.UserToken == userToken).ToList();
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

            if (!response.IsSuccessStatusCode) return false;

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

        public async Task<bool> CancelAllOrdersAsync(string apiKey)
        {
            var url = "https://api.smspool.net/sms/cancel_all";

            var parameters = new Dictionary<string, string>
            {
                { "key", apiKey }
            };

            using var httpClient = new HttpClient();
            using var content = new FormUrlEncodedContent(parameters);

            var response = await httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("CANCEL ALL RESPONSE: " + json);

            if (!response.IsSuccessStatusCode) return false;

            try
            {
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    Console.WriteLine("CancelAll message: " + messageElement.ToString());
                }

                if (doc.RootElement.TryGetProperty("success", out var successElement))
                {
                    var success = successElement.ToString() == "1";
                    if (!success)
                    {
                        Console.WriteLine("CancelAll failed with message: " + messageElement.ToString());
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CancelAllOrdersAsync ERROR: " + ex.Message);
            }

            return false;
        }

        public async Task RefundBlockedNumbersAsync(string apiKey)
        {
            var allOrders = await GetAllOrdersAsync(apiKey);

            if (allOrders == null || allOrders.Count == 0)
            {
                Console.WriteLine("No order to handle.");
                return;
            }

            var blockedOrders = allOrders
                .Where(o =>
                    !string.IsNullOrEmpty(o.PhoneNumber) &&
                    PhoneNumberHelper.IsBlockedNumber(o.PhoneNumber))
                .ToList();

            Console.WriteLine($"Found {blockedOrders.Count} blocked numbers.");

            foreach (var blockedOrder in blockedOrders)
            {
                Console.WriteLine($"[Check] OrderId={blockedOrder.OrderId}, Phone={blockedOrder.PhoneNumber}, Blocked={PhoneNumberHelper.IsBlockedNumber(blockedOrder.PhoneNumber)}");

                if (string.IsNullOrEmpty(blockedOrder.OrderId))
                {
                    Console.WriteLine("OrderId is null or empty, skip refund.");
                    continue;
                }

                bool refundResult = await RefundOrderAsync(blockedOrder.OrderId, apiKey);

                if (refundResult)
                    Console.WriteLine($"Refund order {blockedOrder.OrderId} ({blockedOrder.PhoneNumber}) successfully");
                else
                    Console.WriteLine($"Refund failed for order {blockedOrder.OrderId} ({blockedOrder.PhoneNumber})");

                await Task.Delay(2000);
            }
        }

        public async Task<List<SmsOrderResponse>> GetAllOrdersAsync(string apiKey)
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
            Console.WriteLine("RENTED NUMBERS JSON: " + json);

            var orders = JsonSerializer.Deserialize<List<SmsOrderResponse>>(json);

            return orders;
        }
    }
}
