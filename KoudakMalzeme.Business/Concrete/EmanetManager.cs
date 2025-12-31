using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.DataAccess;
using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using KoudakMalzeme.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace KoudakMalzeme.Business.Concrete
{
	public class EmanetManager : IEmanetService
	{
		private readonly AppDbContext _context;

		public EmanetManager(AppDbContext context)
		{
			_context = context;
		}

		// -----------------------------------------------------------------------
		// 1. TALEP OLUŞTURMA İŞLEMLERİ (KULLANICI)
		// -----------------------------------------------------------------------

		// Malzeme Alma Talebi
		public async Task<ServiceResult<Emanet>> TalepOlusturAsync(int uyeId, EmanetTalepOlusturDto dto)
		{
			if (dto.Malzemeler == null || !dto.Malzemeler.Any())
				return ServiceResult<Emanet>.Basarisiz("Sepet boş.");

			var yeniEmanet = new Emanet
			{
				UyeId = uyeId,
				Durum = EmanetDurumu.TalepEdildi,
				TeslimAlmaTarihi = DateTime.Now,
				EmanetDetaylari = new List<EmanetDetay>()
			};

			foreach (var item in dto.Malzemeler)
			{
				var malzeme = await _context.Malzemeler.FindAsync(item.MalzemeId);
				if (malzeme == null)
					return ServiceResult<Emanet>.Basarisiz($"Malzeme bulunamadı (ID: {item.MalzemeId})");

				// Talep anında stok kontrolü (Henüz düşmüyoruz, sadece var mı diye bakıyoruz)
				if (malzeme.GuncelStok < item.Adet)
					return ServiceResult<Emanet>.Basarisiz($"'{malzeme.Ad}' için yeterli stok yok. Mevcut: {malzeme.GuncelStok}");

				yeniEmanet.EmanetDetaylari.Add(new EmanetDetay
				{
					MalzemeId = item.MalzemeId,
					AlinanAdet = item.Adet,
					IadeEdilenAdet = 0
				});
			}

			_context.Emanetler.Add(yeniEmanet);
			await _context.SaveChangesAsync();

			return ServiceResult<Emanet>.Basarili(yeniEmanet, "Malzeme talebi oluşturuldu. Onay bekleniyor.");
		}

		// Malzeme İade Talebi (YENİ)
		public async Task<ServiceResult<Emanet>> IadeTalepOlusturAsync(int uyeId, EmanetIadeTalepDto dto)
		{
			// Kullanıcının elindeki aktif (iade edilmemiş) malzemeleri hesapla
			var aktifEmanetler = await _context.Emanetler
				.Include(e => e.EmanetDetaylari)
				.Where(e => e.UyeId == uyeId && e.Durum == EmanetDurumu.TeslimEdildi)
				.ToListAsync();

			var yeniIadeTalebi = new Emanet
			{
				UyeId = uyeId,
				Durum = EmanetDurumu.IadeTalepEdildi, // Önemli: İade isteği durumu
				TeslimAlmaTarihi = DateTime.Now,
				EmanetDetaylari = new List<EmanetDetay>()
			};

			foreach (var item in dto.IadeEdilecekler)
			{
				// Kullanıcıda bu malzemeden toplam kaç tane var?
				var kullanicidakiToplam = aktifEmanetler
					.SelectMany(e => e.EmanetDetaylari)
					.Where(d => d.MalzemeId == item.MalzemeId)
					.Sum(d => d.AlinanAdet - d.IadeEdilenAdet);

				if (kullanicidakiToplam < item.Adet)
					return ServiceResult<Emanet>.Basarisiz($"Üzerinizde iade edecek kadar malzeme yok. (ID: {item.MalzemeId})");

				yeniIadeTalebi.EmanetDetaylari.Add(new EmanetDetay
				{
					MalzemeId = item.MalzemeId,
					AlinanAdet = item.Adet, // İade edilecek miktar
					IadeEdilenAdet = 0
				});
			}

			_context.Emanetler.Add(yeniIadeTalebi);
			await _context.SaveChangesAsync();

			return ServiceResult<Emanet>.Basarili(yeniIadeTalebi, "İade talebi oluşturuldu. Yönetici onayı bekleniyor.");
		}

		// -----------------------------------------------------------------------
		// 2. YÖNETİCİ ONAY / RED İŞLEMLERİ
		// -----------------------------------------------------------------------

		// Bekleyen Tüm Talepleri Getir (Alma + İade)
		public async Task<ServiceResult<List<Emanet>>> BekleyenTalepleriGetirAsync()
		{
			var talepler = await _context.Emanetler
				.Include(e => e.Uye)
				.Include(e => e.EmanetDetaylari).ThenInclude(ed => ed.Malzeme)
				.Where(e => e.Durum == EmanetDurumu.TalepEdildi || e.Durum == EmanetDurumu.IadeTalepEdildi)
				.OrderByDescending(e => e.TeslimAlmaTarihi)
				.ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(talepler);
		}

		// Talebi Onayla (Alma ise Stok Düşer, İade ise Stok Artar)
		public async Task<ServiceResult<bool>> TalebiOnaylaAsync(EmanetOnayDto dto)
		{
			var emanet = await _context.Emanetler
				.Include(e => e.EmanetDetaylari).ThenInclude(ed => ed.Malzeme)
				.FirstOrDefaultAsync(e => e.Id == dto.EmanetId);

			if (emanet == null) return ServiceResult<bool>.Basarisiz("Talep bulunamadı.");

			// SENARYO A: Malzeme Alma Talebi
			if (emanet.Durum == EmanetDurumu.TalepEdildi)
			{
				foreach (var detay in emanet.EmanetDetaylari)
				{
					if (detay.Malzeme.GuncelStok < detay.AlinanAdet)
						return ServiceResult<bool>.Basarisiz($"'{detay.Malzeme.Ad}' stoğu tükenmiş. Onaylanamaz.");

					detay.Malzeme.GuncelStok -= detay.AlinanAdet; // Stoktan Düş
				}
				emanet.Durum = EmanetDurumu.TeslimEdildi; // Zimmetlendi
			}
			// SENARYO B: Malzeme İade Talebi
			else if (emanet.Durum == EmanetDurumu.IadeTalepEdildi)
			{
				// 1. Stoğu Geri Artır
				foreach (var detay in emanet.EmanetDetaylari)
				{
					detay.Malzeme.GuncelStok += detay.AlinanAdet;
				}

				// 2. Kullanıcının Üzerindeki Eski Zimmetlerden Düş (FIFO Yöntemi)
				var kullaniciEmanetleri = await _context.Emanetler
					.Include(e => e.EmanetDetaylari)
					.Where(e => e.UyeId == emanet.UyeId && e.Durum == EmanetDurumu.TeslimEdildi)
					.OrderBy(e => e.TeslimAlmaTarihi) // En eskiden başla
					.ToListAsync();

				foreach (var iadeDetay in emanet.EmanetDetaylari)
				{
					int dusulecekMiktar = iadeDetay.AlinanAdet;

					foreach (var eskiEmanet in kullaniciEmanetleri)
					{
						if (dusulecekMiktar <= 0) break;

						// Bu emanette bu malzemeden var mı ve henüz iade edilmemiş mi?
						var eskiDetay = eskiEmanet.EmanetDetaylari
							.FirstOrDefault(d => d.MalzemeId == iadeDetay.MalzemeId && (d.AlinanAdet - d.IadeEdilenAdet) > 0);

						if (eskiDetay != null)
						{
							int mevcutKalan = eskiDetay.AlinanAdet - eskiDetay.IadeEdilenAdet;
							int dusus = Math.Min(mevcutKalan, dusulecekMiktar);

							eskiDetay.IadeEdilenAdet += dusus;
							dusulecekMiktar -= dusus;

							// Eğer bu emanetteki her şey iade edildiyse durumu Tamamlandı yap
							if (eskiEmanet.EmanetDetaylari.All(d => d.AlinanAdet == d.IadeEdilenAdet))
							{
								eskiEmanet.Durum = EmanetDurumu.Tamamlandi;
								eskiEmanet.IadeTarihi = DateTime.Now;
								eskiEmanet.AlanPersonelId = dto.PersonelId;
							}
						}
					}
				}

				// İade talebi kaydını kapat (Bu bir zimmet kaydı değil, talep kaydıydı)
				emanet.Durum = EmanetDurumu.Tamamlandi;
				emanet.IadeTarihi = DateTime.Now;
				emanet.AlanPersonelId = dto.PersonelId;
			}
			else
			{
				return ServiceResult<bool>.Basarisiz("Bu kayıt onaylanabilir bir durumda değil.");
			}

			// Ortak Bilgiler
			emanet.VerenPersonelId = dto.PersonelId;
			if (!string.IsNullOrEmpty(dto.MalzemeciNotu))
				emanet.MalzemeciNotu = dto.MalzemeciNotu;

			if (dto.PlanlananIadeTarihi.HasValue && emanet.Durum == EmanetDurumu.TeslimEdildi)
				emanet.PlanlananIadeTarihi = dto.PlanlananIadeTarihi;

			await _context.SaveChangesAsync();
			return ServiceResult<bool>.Basarili(true, "İşlem başarıyla onaylandı.");
		}

		// Talebi Reddet
		public async Task<ServiceResult<bool>> TalebiReddetAsync(EmanetRedDto dto)
		{
			var emanet = await _context.Emanetler.FindAsync(dto.EmanetId);
			if (emanet == null) return ServiceResult<bool>.Basarisiz("Talep bulunamadı.");

			emanet.VerenPersonelId = dto.PersonelId;
			emanet.MalzemeciNotu = dto.RetNedeni;
			emanet.Durum = EmanetDurumu.IptalEdildi; // veya Reddedildi

			await _context.SaveChangesAsync();
			return ServiceResult<bool>.Basarili(true, "Talep reddedildi.");
		}

		// -----------------------------------------------------------------------
		// 3. RAPORLAMA VE LİSTELEME
		// -----------------------------------------------------------------------

		public async Task<ServiceResult<List<Emanet>>> GecmisEmanetleriGetirAsync()
		{
			var liste = await _context.Emanetler
				.Include(e => e.Uye)
				.Include(e => e.VerenPersonel)
				.Include(e => e.AlanPersonel)
				.Include(e => e.EmanetDetaylari).ThenInclude(d => d.Malzeme)
				.OrderByDescending(e => e.TeslimAlmaTarihi)
				.ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(liste);
		}

		public async Task<ServiceResult<List<Emanet>>> UyeEmanetleriniGetirAsync(int uyeId)
		{
			var liste = await _context.Emanetler
				.Include(e => e.Uye)
				.Include(e => e.EmanetDetaylari).ThenInclude(d => d.Malzeme)
				.Where(e => e.UyeId == uyeId)
				.OrderByDescending(e => e.TeslimAlmaTarihi)
				.ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(liste);
		}

		public async Task<ServiceResult<List<Emanet>>> AktifEmanetleriGetirAsync()
		{
			var liste = await _context.Emanetler
				.Include(e => e.Uye)
				.Include(e => e.EmanetDetaylari).ThenInclude(d => d.Malzeme)
				.Where(e => e.Durum == EmanetDurumu.TeslimEdildi)
				.OrderByDescending(e => e.TeslimAlmaTarihi)
				.ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(liste);
		}

		public async Task<ServiceResult<Emanet>> GetirByIdAsync(int id)
		{
			var emanet = await _context.Emanetler
				.Include(e => e.Uye)
				.Include(e => e.EmanetDetaylari).ThenInclude(d => d.Malzeme)
				.FirstOrDefaultAsync(e => e.Id == id);

			if (emanet == null) return ServiceResult<Emanet>.Basarisiz("Kayıt bulunamadı.");
			return ServiceResult<Emanet>.Basarili(emanet);
		}

		// -----------------------------------------------------------------------
		// 4. LEGACY / DİĞER METOTLAR
		// -----------------------------------------------------------------------

		// (Bu metodlar interface uyumluluğu için tutuluyor, ana akış talep üzerindendir)
		public async Task<ServiceResult<int>> EmanetVerAsync(EmanetVermeIstegiDto istek)
		{
			// Manuel emanet verme gerekirse burası doldurulabilir.
			// Şu an Talep -> Onay akışı kullanıldığı için pasif bırakıyoruz.
			return ServiceResult<int>.Basarisiz("Lütfen talep sistemini kullanın.");
		}

		public async Task<ServiceResult<int>> IadeAlAsync(EmanetIadeIstegiDto istek)
		{
			// Eski 'Manuel İade Al' sayfası için.
			// Yeni sistemde kullanıcı 'İade Talebi' oluşturuyor.
			// Ancak acil durumlar için admin manuel iade almak isterse bu çalışır.
			var emanet = await _context.Emanetler
			   .Include(e => e.EmanetDetaylari).ThenInclude(ed => ed.Malzeme)
			   .FirstOrDefaultAsync(e => e.Id == istek.EmanetId);

			if (emanet == null) return ServiceResult<int>.Basarisiz("Emanet bulunamadı.");

			foreach (var iadeKalem in istek.IadeEdilenler)
			{
				var detay = emanet.EmanetDetaylari.FirstOrDefault(d => d.MalzemeId == iadeKalem.MalzemeId);
				if (detay != null)
				{
					if (detay.IadeEdilenAdet + iadeKalem.Adet <= detay.AlinanAdet)
					{
						detay.IadeEdilenAdet += iadeKalem.Adet;
						detay.Malzeme.GuncelStok += iadeKalem.Adet;
					}
				}
			}

			emanet.AlanPersonelId = istek.AlanPersonelId;

			if (emanet.EmanetDetaylari.All(d => d.TamamlandiMi))
			{
				emanet.Durum = EmanetDurumu.Tamamlandi;
				emanet.IadeTarihi = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return ServiceResult<int>.Basarili(emanet.Id, "Manuel iade işlemi tamamlandı.");
		}
	}
}