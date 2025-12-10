using KoudakMalzeme.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace KoudakMalzeme.DataAccess
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}

		// Tablolarımız
		public DbSet<Kullanici> Kullanicilar { get; set; }
		public DbSet<Malzeme> Malzemeler { get; set; }
		public DbSet<Emanet> Emanetler { get; set; }
		public DbSet<EmanetDetay> EmanetDetaylari { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// --- İLİŞKİ AYARLARI (Fluent API) ---

			// 1. Emanet -> Alan Üye İlişkisi
			modelBuilder.Entity<Emanet>()
				.HasOne(x => x.Uye)
				.WithMany(u => u.AldigiEmanetler)
				.HasForeignKey(x => x.UyeId)
				.OnDelete(DeleteBehavior.Restrict); // Üye silinirse emanet kayıtları silinmesin, hata versin.

			// 2. Emanet -> Veren Personel İlişkisi
			// Bir kullanıcının birden fazla 'verdiği' emanet olabilir ama Kullanıcı entitysine
			// 'VerdigiEmanetler' diye liste eklemedik (gerek yoktu), o yüzden WithMany() boş.
			modelBuilder.Entity<Emanet>()
				.HasOne(x => x.VerenPersonel)
				.WithMany()
				.HasForeignKey(x => x.VerenPersonelId)
				.OnDelete(DeleteBehavior.Restrict);

			// 3. Emanet -> Teslim Alan Personel İlişkisi
			modelBuilder.Entity<Emanet>()
				.HasOne(x => x.AlanPersonel)
				.WithMany()
				.HasForeignKey(x => x.AlanPersonelId)
				.OnDelete(DeleteBehavior.Restrict);

			// 4. Emanet -> EmanetDetay İlişkisi
			// Bir emanet silinirse detayları da silinsin (Cascade)
			modelBuilder.Entity<EmanetDetay>()
				.HasOne(x => x.Emanet)
				.WithMany(e => e.EmanetDetaylari)
				.HasForeignKey(x => x.EmanetId)
				.OnDelete(DeleteBehavior.Cascade);

			base.OnModelCreating(modelBuilder);
		}
	}
}