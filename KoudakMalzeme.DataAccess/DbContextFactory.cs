using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KoudakMalzeme.DataAccess
{
	public class DbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
	{
		public AppDbContext CreateDbContext(string[] args)
		{
			var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

			// Buradaki Server ismini kendi MSSQL server isminle değiştirmelisin.
			// Genelde "Server=.;" veya "Server=(localdb)\\mssqllocaldb;" veya "Server=DESKTOP-XXXX;" olur.
			// Şimdilik nokta (.) koydum, localhost demektir.
			var connectionString = "Server=.;Database=KoudakMalzemeDb;Trusted_Connection=True;TrustServerCertificate=True;";

			optionsBuilder.UseSqlServer(connectionString);

			return new AppDbContext(optionsBuilder.Options);
		}
	}
}