namespace SMSPools_App.Services
{
    public class OrderTokenStore
    {
        private readonly Dictionary<string, string> _orderTokens = new Dictionary<string, string>();

        public void Set(string orderId, string userToken)
        {
            _orderTokens[orderId] = userToken;
        }

        public bool TryGet(string orderId, out string userToken)
        {
            return _orderTokens.TryGetValue(orderId, out userToken);
        }
    }
}
