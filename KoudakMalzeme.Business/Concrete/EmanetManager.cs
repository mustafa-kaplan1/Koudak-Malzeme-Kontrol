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

		// 1. TALEP OLUŞTUR
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

				// Stok kontrolü (Talep anında stok yetiyor mu?)
				if (malzeme.GuncelStok < item.Adet)
					return ServiceResult<Emanet>.Basarisiz($"'{malzeme.Ad}' için yeterli stok yok. Mevcut: {malzeme.GuncelStok}");

				yeniEmanet.EmanetDetaylari.Add(new EmanetDetay
				{
					MalzemeId = item.MalzemeId,
					// HATA 1 ve 2 ÇÖZÜMÜ: Adet -> AlinanAdet, KalanAdet ataması kaldırıldı.
					AlinanAdet = item.Adet,
					IadeEdilenAdet = 0 // Başlangıçta hiç iade yok
				});
			}

			_context.Emanetler.Add(yeniEmanet);
			await _context.SaveChangesAsync();

			return ServiceResult<Emanet>.Basarili(yeniEmanet, "Talep başarıyla oluşturuldu. Onay bekleniyor.");
		}

		// 2. BEKLEYEN TALEPLERİ GETİR
		public async Task<ServiceResult<List<Emanet>>> BekleyenTalepleriGetirAsync()
		{
			var talepler = await _context.Emanetler
				.Include(e => e.Uye)
				.Include(e => e.EmanetDetaylari)
					.ThenInclude(ed => ed.Malzeme)
				.Where(e => e.Durum == EmanetDurumu.TalepEdildi)
				.OrderByDescending(e => e.TeslimAlmaTarihi)
				.ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(talepler);
		}

		// 3. TALEBİ ONAYLA (STOK DÜŞER)
		public async Task<ServiceResult<bool>> TalebiOnaylaAsync(EmanetOnayDto dto)
		{
			var emanet = await _context.Emanetler
				.Include(e => e.EmanetDetaylari)
					.ThenInclude(ed => ed.Malzeme)
				.FirstOrDefaultAsync(e => e.Id == dto.EmanetId);

			if (emanet == null) return ServiceResult<bool>.Basarisiz("Talep bulunamadı.");

			// Stok Kontrolü ve Düşümü
			foreach (var detay in emanet.EmanetDetaylari)
			{
				// HATA 1 ÇÖZÜMÜ: detay.Adet -> detay.AlinanAdet
				if (detay.Malzeme.GuncelStok < detay.AlinanAdet)
					return ServiceResult<bool>.Basarisiz($"'{detay.Malzeme.Ad}' stoğu tükenmiş. İşlem yapılamaz.");

				detay.Malzeme.GuncelStok -= detay.AlinanAdet;
			}

			emanet.VerenPersonelId = dto.PersonelId;
			emanet.TeslimAlmaTarihi = DateTime.Now;
			emanet.PlanlananIadeTarihi = dto.PlanlananIadeTarihi;
			emanet.MalzemeciNotu = dto.MalzemeciNotu;
			emanet.Durum = EmanetDurumu.TeslimEdildi;

			await _context.SaveChangesAsync();
			return ServiceResult<bool>.Basarili(true, "Talep onaylandı ve stoktan düşüldü.");
		}

		// 4. TALEBİ REDDET
		public async Task<ServiceResult<bool>> TalebiReddetAsync(EmanetRedDto dto)
		{
			var emanet = await _context.Emanetler.FindAsync(dto.EmanetId);
			if (emanet == null) return ServiceResult<bool>.Basarisiz("Talep bulunamadı.");

			emanet.VerenPersonelId = dto.PersonelId;
			emanet.MalzemeciNotu = dto.RetNedeni;

			// HATA 3 ÇÖZÜMÜ: Reddedildi -> IptalEdildi
			emanet.Durum = EmanetDurumu.IptalEdildi;

			await _context.SaveChangesAsync();
			return ServiceResult<bool>.Basarili(true, "Talep reddedildi.");
		}

		// --- EKSİK KALAN METOTLARINIZI TAMAMLAYALIM ---

		// Geçmiş (Tüm Emanetler)
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

		// Üye Özelinde Emanetler
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

		// ID ile Getir (İade Ekranı İçin)
		public async Task<ServiceResult<Emanet>> GetirByIdAsync(int id)
		{
			var emanet = await _context.Emanetler
				.Include(e => e.Uye)
				.Include(e => e.EmanetDetaylari).ThenInclude(d => d.Malzeme)
				.FirstOrDefaultAsync(e => e.Id == id);

			if (emanet == null) return ServiceResult<Emanet>.Basarisiz("Kayıt bulunamadı.");
			return ServiceResult<Emanet>.Basarili(emanet);
		}

		// İade Alma İşlemi
		public async Task<ServiceResult<int>> IadeAlAsync(EmanetIadeIstegiDto istek)
		{
			var emanet = await _context.Emanetler
			   .Include(e => e.EmanetDetaylari).ThenInclude(ed => ed.Malzeme)
			   .FirstOrDefaultAsync(e => e.Id == istek.EmanetId);

			if (emanet == null) return ServiceResult<int>.Basarisiz("Emanet bulunamadı.");

			foreach (var iadeKalem in istek.IadeEdilenler)
			{
				var detay = emanet.EmanetDetaylari.FirstOrDefault(d => d.MalzemeId == iadeKalem.MalzemeId);
				if (detay != null)
				{
					// İade miktarını artır
					if (detay.IadeEdilenAdet + iadeKalem.Adet <= detay.AlinanAdet)
					{
						detay.IadeEdilenAdet += iadeKalem.Adet;

						// Stoğa geri ekle
						detay.Malzeme.GuncelStok += iadeKalem.Adet;
					}
				}
			}

			emanet.AlanPersonelId = istek.AlanPersonelId;

			// Hepsi tamamlandı mı?
			if (emanet.EmanetDetaylari.All(d => d.TamamlandiMi))
			{
				emanet.Durum = EmanetDurumu.Tamamlandi;
				emanet.IadeTarihi = DateTime.Now;
			}
			else
			{
				emanet.Durum = EmanetDurumu.KismenIadeEdildi;
			}

			await _context.SaveChangesAsync();
			return ServiceResult<int>.Basarili(emanet.Id, "İade alındı.");
		}

		// Emanet Verme (Manuel/Direkt) - Interface'de varsa diye ekledim, TalepOlustur+Onayla akışını kullanıyoruz normalde.
		public async Task<ServiceResult<int>> EmanetVerAsync(EmanetVermeIstegiDto istek)
		{
			// Bu metot eski yapıdan kalmış olabilir, şu anki "Talep -> Onay" akışında buna gerek yok.
			// Ancak Interface zorunlu kılıyorsa boş dönebilir veya implemente edebilirsiniz.
			return ServiceResult<int>.Basarisiz("Lütfen talep sistemini kullanın.");
		}

		// Aktif Emanetler (Teslim Edilmiş ama Tamamlanmamış)
		public async Task<ServiceResult<List<Emanet>>> AktifEmanetleriGetirAsync()
		{
			var liste = await _context.Emanetler
			   .Include(e => e.Uye)
			   .Include(e => e.EmanetDetaylari).ThenInclude(d => d.Malzeme)
			   .Where(e => e.Durum == EmanetDurumu.TeslimEdildi || e.Durum == EmanetDurumu.KismenIadeEdildi || e.Durum == EmanetDurumu.Gecikmede)
			   .OrderByDescending(e => e.TeslimAlmaTarihi)
			   .ToListAsync();

			return ServiceResult<List<Emanet>>.Basarili(liste);
		}
	}
}