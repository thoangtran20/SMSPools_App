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

			if (digitsOnly.Length < 3) return true;
			string prefix = digitsOnly.Substring(0, 3);
			return BlockedPrefixes.Blocked.Contains(prefix);
		}
	}
}
