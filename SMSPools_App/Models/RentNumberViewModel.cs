namespace SMSPools_App.Models
{
    public class RentNumberViewModel
    {
        public SmsAccountConfig? Account { get; set; }
        public SmsOrderResponse? Order { get; set; }
        public List<SmsOrderResponse>? Orders { get; set; }

    }
}
