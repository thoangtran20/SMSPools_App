using System.ComponentModel.DataAnnotations;

namespace SMSPools_App.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; }

        public string UserToken { get; set; }
        public string AccId { get; set; }
    }
}
