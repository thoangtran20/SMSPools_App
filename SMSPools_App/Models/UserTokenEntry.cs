using System.ComponentModel.DataAnnotations;

namespace SMSPools_App.Models
{
    public class UserTokenEntry
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email is invalid")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Login code is required")]
        public string? Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Expiration { get; set; }

        public bool IsUsed { get; set; }

        public string? UserToken { get; set; }
        public bool IsRegistered { get; set; }
    }
}
