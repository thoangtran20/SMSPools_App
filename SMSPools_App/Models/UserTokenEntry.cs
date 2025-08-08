namespace SMSPools_App.Models
{
	public class UserTokenEntry
	{
		public int Id { get; set; }	
		public string? Email { get; set; }
		public string? Token { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime Expiration { get; set; }

		public bool IsUsed { get; set; }

		public string? UserToken { get; set; }
		public bool IsRegistered { get; set; }
	}
}
