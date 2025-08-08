using Microsoft.EntityFrameworkCore;
using SMSPools_App.Models;

namespace SMSPools_App.Data
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
		{ 
		}

		public DbSet<UserTokenEntry> UserTokenEntries { get; set; }	
	}
}
