using SMSPools_App.Models;

namespace SMSPools_App.Services.Interfaces
{
    public interface ISmsApiService
    {
        Task<SmsOrderResponse?> RentNumberAsync(string apiKey);
        Task<bool> ResendCodeAsync(string orderId, string apiKey);
        Task<string?> GetOtpAsync(string orderId, string apiKey);
        Task<bool> RefundOrderAsync(string orderId, string apiKey);
        Task<List<SmsOrderResponse?>> GetAlRentNumbersAsync(string apiKey);
    }
}
