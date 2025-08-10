using System.Text.Json.Serialization;

namespace SMSPools_App.Models
{
    public class SmsOrderResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("order_id")] // rent number
        public string? OrderId { get; set; }

        [JsonPropertyName("order_code")] // get order
        public string? OrderCode { get; set; }

        [JsonPropertyName("phonenumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("cc")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("service")]
        public string? Service { get; set; }

        [JsonPropertyName("service_id")]
        public int ServiceId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("cost")]
        public string? Cost { get; set; }

        [JsonPropertyName("can_refund")]
        public bool CanRefund { get; set; }

        [JsonPropertyName("can_resend")]
        public bool CanResend { get; set; }

        [JsonPropertyName("expiry")]
        public long Expiry { get; set; }

        [JsonPropertyName("full_code")]
        public string? FullCode { get; set; }

        [JsonPropertyName("time_left")]
        public int TimeLeft { get; set; }

        public string? ErrorMessage { get; set; }
        public string? UserToken { get; set; }

        [JsonIgnore]
        public string? EffectiveOrderId => OrderId ?? OrderCode;

    }
}
