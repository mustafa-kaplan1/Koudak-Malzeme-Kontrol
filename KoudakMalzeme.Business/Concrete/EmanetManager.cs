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
			// 1. Mevcut Emanet Kaydını Bul
			var emanet = await _context.Emanetler
				.Include(e => e.EmanetDetaylari)
				.FirstOrDefaultAsync(e => e.Id == dto.EmanetId && e.UyeId == uyeId);

			if (emanet == null)
				return ServiceResult<Emanet>.Basarisiz("Emanet kaydı bulunamadı.");

			if (emanet.Durum != EmanetDurumu.TeslimEdildi && emanet.Durum != EmanetDurumu.KismenIadeEdildi)
				return ServiceResult<Emanet>.Basarisiz("Bu kayıt için zaten bekleyen bir işlem var veya kapalı.");

			// 2. Talep Edilen Miktarları İşle
			bool talepVar = false;
			foreach (var item in dto.IadeEdilecekler)
			{
				var detay = emanet.EmanetDetaylari.FirstOrDefault(d => d.MalzemeId == item.MalzemeId);
				if (detay != null)
				{
					if (item.Adet > detay.KalanAdet)
						return ServiceResult<Emanet>.Basarisiz($"İade etmek istediğiniz miktar, elinizdekinden fazla. (Malzeme ID: {item.MalzemeId})");

					if (item.Adet > 0)
					{
						detay.IadeTalepEdilenAdet = item.Adet; // Miktarı kaydet
						talepVar = true;
					}
				}
			}

			if (!talepVar)
				return ServiceResult<Emanet>.Basarisiz("İade edilecek geçerli bir miktar seçilmedi.");

			// 3. Durumu Güncelle (Yeni kayıt yok!)
			emanet.Durum = EmanetDurumu.IadeTalepEdildi;
			await _context.SaveChangesAsync();

			return ServiceResult<Emanet>.Basarili(emanet, "İade talebiniz alındı, onay bekleniyor.");
		}

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

			// SENARYO A: Malzeme Alma Talebi (Değişiklik Yok)
			if (emanet.Durum == EmanetDurumu.TalepEdildi)
			{
				// ... (Eski kodunuzdaki alma mantığı aynen kalacak) ...
				foreach (var detay in emanet.EmanetDetaylari)
				{
					if (detay.Malzeme.GuncelStok < detay.AlinanAdet)
						return ServiceResult<bool>.Basarisiz($"Stok yetersiz: {detay.Malzeme.Ad}");
					detay.Malzeme.GuncelStok -= detay.AlinanAdet;
				}
				emanet.Durum = EmanetDurumu.TeslimEdildi;
				emanet.VerenPersonelId = dto.PersonelId;
				// ...
			}
			// SENARYO B: Malzeme İade Talebi (YENİ MANTIK)
			else if (emanet.Durum == EmanetDurumu.IadeTalepEdildi)
			{
				foreach (var detay in emanet.EmanetDetaylari)
				{
					if (detay.IadeTalepEdilenAdet > 0)
					{
						// 1. Stoğu Artır
						detay.Malzeme.GuncelStok += detay.IadeTalepEdilenAdet;

						// 2. İade Edilen Sayısını Güncelle
						detay.IadeEdilenAdet += detay.IadeTalepEdilenAdet;

						// 3. Talep Sayacını Sıfırla
						detay.IadeTalepEdilenAdet = 0;
					}
				}

				// 4. Emanetin Yeni Durumunu Belirle
				if (emanet.EmanetDetaylari.All(d => d.TamamlandiMi))
				{
					emanet.Durum = EmanetDurumu.Tamamlandi;
					emanet.IadeTarihi = DateTime.Now;
					emanet.AlanPersonelId = dto.PersonelId;
				}
				else
				{
					if (emanet.EmanetDetaylari.Any(d => d.IadeEdilenAdet > 0))
					{
						emanet.Durum = EmanetDurumu.KismenIadeEdildi;
					}
					else
					{
						emanet.Durum = EmanetDurumu.TeslimEdildi;
					}
				}
			}
			else
			{
				return ServiceResult<bool>.Basarisiz("Bu kayıt onaylanabilir bir durumda değil.");
			}

			await _context.SaveChangesAsync();
			return ServiceResult<bool>.Basarili(true, "İşlem başarıyla onaylandı.");
		}

		// Talebi Reddet
		public async Task<ServiceResult<bool>> TalebiReddetAsync(EmanetRedDto dto)
		{
			var emanet = await _context.Emanetler.Include(e => e.EmanetDetaylari).FirstOrDefaultAsync(e => e.Id == dto.EmanetId);
			if (emanet == null) return ServiceResult<bool>.Basarisiz("Talep bulunamadı.");

			if (emanet.Durum == EmanetDurumu.TalepEdildi)
			{
				// Alma talebi reddedilirse iptal olur
				emanet.Durum = EmanetDurumu.Reddedildi;
				emanet.MalzemeciNotu = dto.RetNedeni;
			}
			else if (emanet.Durum == EmanetDurumu.IadeTalepEdildi)
			{
				// İade talebi reddedilirse, malzemeler hala kullanıcıdadır -> Tekrar 'TeslimEdildi' olur.
				emanet.Durum = EmanetDurumu.TeslimEdildi;
				emanet.MalzemeciNotu = dto.RetNedeni; // Red nedenini nota ekleyebiliriz

				// Talep edilen iade miktarlarını sıfırla ki eski haline dönsün
				foreach (var detay in emanet.EmanetDetaylari)
				{
					detay.IadeTalepEdilenAdet = 0;
				}
			}

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