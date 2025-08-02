using SMSPools_App.Models;

namespace SMSPools_App.Services.Interfaces
{
    public interface ISmsApiService
    {
        Task<SmsOrderResponse?> RentNumberAsync(string apiKey);
        Task<bool> ResendCodeAsync(string orderCode, string apiKey);
        Task<string?> GetOtpAsync(string orderCode, string apiKey);
    }
}
