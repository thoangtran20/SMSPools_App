using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;

namespace SMSPools_App.Utility
{
	public class EmailSender : IEmailSender
	{
		public string SendGridSecret { get; set; }

		public EmailSender(IConfiguration config)
		{
			SendGridSecret = config.GetValue<string>("SendGrid:SecretKey");
		}
		public Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			var client = new SendGridClient(SendGridSecret);
			var from = new EmailAddress("thoangtran20@gmail.com", "SMSPools");
			var to = new EmailAddress(email);
			var plainTextContent = "Please view this email in an HTML-compatible client.";
			var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlMessage);
			return client.SendEmailAsync(msg);
		}
	}
}
