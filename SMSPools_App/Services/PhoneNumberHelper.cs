using SMSPools_App.Models;

namespace SMSPools_App.Services
{
    public class PhoneNumberHelper
    {
        public static bool IsBlockedNumber(string phoneNumber)
        {
            // Filter out only digits
            string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Remove country code if it is a US number
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                digitsOnly = digitsOnly.Substring(1);
            }

            if (digitsOnly.Length < 3)
            {
                Console.WriteLine($"[BlockCheck] {phoneNumber} => Too short");
                return true;
            }
            string prefix = digitsOnly.Substring(0, 3);
            bool isBlocked = BlockedPrefixes.Blocked.Contains(prefix);

            Console.WriteLine($"[BlockCheck] {phoneNumber} => Digits: {digitsOnly}, Prefix: {prefix}, Blocked: {isBlocked}");
            return isBlocked;
        }
    }
}
