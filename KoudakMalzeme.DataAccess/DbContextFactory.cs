using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace KoudakMalzeme.DataAccess
{
	public class DbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
	{
		public AppDbContext CreateDbContext(string[] args)
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "../KoudakMalzeme.API/appsettings.json"), optional: true)
				.Build();

			var builder = new DbContextOptionsBuilder<AppDbContext>();

			var connectionString = configuration.GetConnectionString("DefaultConnection");

			if (string.IsNullOrEmpty(connectionString))
			{
				throw new InvalidOperationException("Bağlantı adresi (Connection String) bulunamadı! Lütfen 'KoudakMalzeme.API/appsettings.json' dosyasını ve içindeki 'DefaultConnection' alanını kontrol edin.");
			}

			builder.UseSqlServer(connectionString);

			return new AppDbContext(builder.Options);
		}
	}
}