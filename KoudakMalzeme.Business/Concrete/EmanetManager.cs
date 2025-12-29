using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Business.Types;
using KoudakMalzeme.DataAccess;
using KoudakMalzeme.Shared.Entities;
using KoudakMalzeme.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Concrete
{
	public class EmanetManager : IEmanetService
	{
		private readonly AppDbContext _context;

		public EmanetManager(AppDbContext context)
		{
			_context = context;
		}

		public async Task<ServiceResult<int>> EmanetVerAsync(EmanetVermeIstegi istek)
		{
			// Transaction (İşlem bütünlüğü): Bir hata olursa her şeyi geri al.
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// 1. Yeni Emanet Kaydı Oluştur
				var emanet = new Emanet
				{
					UyeId = istek.UyeId,
					VerenPersonelId = istek.VerenPersonelId,
					TeslimAlmaTarihi = DateTime.Now,
					Durum = EmanetDurumu.TeslimEdildi,
					MalzemeciNotu = istek.Not
				};

				// 2. Kalemleri Ekle ve Stok Düş
				foreach (var kalem in istek.Kalemler)
				{
					var malzeme = await _context.Malzemeler.FindAsync(kalem.MalzemeId);
					if (malzeme == null)
						throw new Exception($"Malzeme ID {kalem.MalzemeId} bulunamadı.");

					// Stok Kontrolü
					if (malzeme.GuncelStok < kalem.Adet)
						throw new Exception($"{malzeme.Ad} için yeterli stok yok! (Mevcut: {malzeme.GuncelStok}, İstenen: {kalem.Adet})");

					// Stoktan Düş
					malzeme.GuncelStok -= kalem.Adet;

					// Detay Ekle
					emanet.EmanetDetaylari.Add(new EmanetDetay
					{
						MalzemeId = kalem.MalzemeId,
						AlinanAdet = kalem.Adet,
						IadeEdilenAdet = 0
					});
				}

				_context.Emanetler.Add(emanet);
				await _context.SaveChangesAsync();

				// Her şey yolundaysa onayla
				await transaction.CommitAsync();

				return ServiceResult<int>.Basarili(emanet.Id, "Emanet verme işlemi başarılı.");
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return ServiceResult<int>.Basarisiz(ex.Message);
			}
		}

		public async Task<ServiceResult<bool>> IadeAlAsync(EmanetIadeIstegi istek)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// Emaneti ve detaylarını getir
				var emanet = await _context.Emanetler
					.Include(x => x.EmanetDetaylari)
					.ThenInclude(d => d.Malzeme) // Malzeme stoğunu artıracağız
					.FirstOrDefaultAsync(x => x.Id == istek.EmanetId);

				if (emanet == null)
					return ServiceResult<bool>.Basarisiz("Emanet kaydı bulunamadı.");

				// İade işlemleri
				foreach (var iadeKalem in istek.IadeEdilenler)
				{
					var detay = emanet.EmanetDetaylari.FirstOrDefault(d => d.MalzemeId == iadeKalem.MalzemeId);

					if (detay == null) continue; // Bu emanette böyle bir malzeme yoksa geç

					// Fazla iade girilmesini engelle
					if (iadeKalem.Adet > detay.KalanAdet)
						throw new Exception($"{detay.Malzeme.Ad} için iade edilen miktar, alınandan fazla olamaz!");

					// Detay Güncelle
					detay.IadeEdilenAdet += iadeKalem.Adet;

					// Stok İade Al (Geri rafa koy)
					detay.Malzeme.GuncelStok += iadeKalem.Adet;
				}

				// Genel Durumu Kontrol Et
				// Tüm detaylardaki malzemeler tamamen döndü mü?
				bool hepsiDondu = emanet.EmanetDetaylari.All(d => d.TamamlandiMi);

				emanet.AlanPersonelId = istek.AlanPersonelId;

				if (hepsiDondu)
				{
					emanet.Durum = EmanetDurumu.Tamamlandi;
					emanet.IadeTarihi = DateTime.Now;
				}
				else
				{
					emanet.Durum = EmanetDurumu.KismenIadeEdildi;
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				return ServiceResult<bool>.Basarili(true, hepsiDondu ? "Tüm malzemeler teslim alındı." : "Kısmi iade alındı.");
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return ServiceResult<bool>.Basarisiz(ex.Message);
			}
		}

		public async Task<ServiceResult<List<Emanet>>> AktifEmanetleriGetirAsync()
		{
			// Henüz tamamlanmamış emanetler (TeslimEdildi veya KismenIadeEdildi)
			var liste = await _context.Emanetler
				.Include(x => x.Uye) // Üye ismini görmek için
				.Include(x => x.EmanetDetaylari)
				.ThenInclude(d => d.Malzeme)
				.Where(x => x.Durum == EmanetDurumu.TeslimEdildi || x.Durum == EmanetDurumu.KismenIadeEdildi || x.Durum == EmanetDurumu.Gecikmede)
				.OrderByDescending(x => x.TeslimAlmaTarihi)
				.ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(liste);
		}

		public async Task<ServiceResult<List<Emanet>>> GecmisEmanetleriGetirAsync()
		{
			var liste = await _context.Emanetler
				.Include(x => x.Uye)
				.Include(x => x.AlanPersonel) // Kim teslim aldı?
				.Where(x => x.Durum == EmanetDurumu.Tamamlandi || x.Durum == EmanetDurumu.IptalEdildi)
				.OrderByDescending(x => x.IadeTarihi)
				.ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(liste);
		}

		public async Task<ServiceResult<Emanet>> GetirByIdAsync(int id)
		{
			var kayit = await _context.Emanetler
				.Include(x => x.Uye)
				.Include(x => x.VerenPersonel)
				.Include(x => x.AlanPersonel)
				.Include(x => x.EmanetDetaylari)
				.ThenInclude(d => d.Malzeme)
				.FirstOrDefaultAsync(x => x.Id == id);

			if (kayit == null) return ServiceResult<Emanet>.Basarisiz("Bulunamadı");

			return ServiceResult<Emanet>.Basarili(kayit);
		}

		public async Task<ServiceResult<List<Emanet>>> UyeEmanetleriniGetirAsync(int uyeId)
		{
			var liste = await _context.Emanetler
			   .Include(x => x.EmanetDetaylari)
			   .ThenInclude(d => d.Malzeme)
			   .Where(x => x.UyeId == uyeId)
			   .OrderByDescending(x => x.TeslimAlmaTarihi)
			   .ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(liste);
		}
	}
}