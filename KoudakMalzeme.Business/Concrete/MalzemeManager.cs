using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.DataAccess;
using KoudakMalzeme.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Concrete
{
	public class MalzemeManager : IMalzemeService
	{
		private readonly AppDbContext _context;

		public MalzemeManager(AppDbContext context)
		{
			_context = context;
		}

		public async Task<ServiceResult<List<Malzeme>>> TumMalzemeleriGetirAsync()
		{
			// Include ile Emanet geçmişini, oradan Emanet'e, oradan da Üye'ye ulaşıyoruz.
			var liste = await _context.Malzemeler
				.Include(m => m.EmanetGecmisi)
					.ThenInclude(ed => ed.Emanet)
					.ThenInclude(e => e.Uye)
				.ToListAsync();

			return ServiceResult<List<Malzeme>>.Basarili(liste);
		}

		public async Task<ServiceResult<Malzeme>> GetirByIdAsync(int id)
		{
			var malzeme = await _context.Malzemeler.FindAsync(id);
			if (malzeme == null)
				return ServiceResult<Malzeme>.Basarisiz("Malzeme bulunamadı.");

			return ServiceResult<Malzeme>.Basarili(malzeme);
		}

		public async Task<ServiceResult<int>> EkleAsync(Malzeme malzeme)
		{
			// Yeni eklenen malzemenin güncel stoğu toplam stoğuna eşittir.
			malzeme.GuncelStok = malzeme.ToplamStok;

			_context.Malzemeler.Add(malzeme);
			await _context.SaveChangesAsync();

			return ServiceResult<int>.Basarili(malzeme.Id, "Malzeme başarıyla eklendi.");
		}

		public async Task<ServiceResult<bool>> GuncelleAsync(Malzeme malzeme)
		{
			var mevcut = await _context.Malzemeler.FindAsync(malzeme.Id);
			if (mevcut == null)
				return ServiceResult<bool>.Basarisiz("Güncellenecek malzeme bulunamadı.");

			// Sadece değişmesi gereken alanları güncelleyelim
			mevcut.Ad = malzeme.Ad;
			mevcut.Aciklama = malzeme.Aciklama;
			mevcut.GorselYolu = malzeme.GorselYolu;

			// Stok değişimi kritik olabilir, burada basitçe güncelliyoruz 
			// ama normalde stok hareketleriyle güncellenmeli.
			mevcut.ToplamStok = malzeme.ToplamStok;

			// Eğer toplam stok artırıldıysa güncel stoğu da artırabiliriz (opsiyonel mantık)
			// Şimdilik manuel bırakıyoruz.

			_context.Malzemeler.Update(mevcut);
			await _context.SaveChangesAsync();

			return ServiceResult<bool>.Basarili(true, "Güncelleme başarılı.");
		}

		public async Task<ServiceResult<bool>> SilAsync(int id)
		{
			var malzeme = await _context.Malzemeler.FindAsync(id);
			if (malzeme == null)
				return ServiceResult<bool>.Basarisiz("Silinecek malzeme bulunamadı.");

			// Emanette olan (GuncelStok < ToplamStok) malzeme silinmemeli!
			if (malzeme.GuncelStok < malzeme.ToplamStok)
				return ServiceResult<bool>.Basarisiz("Bu malzeme şu an emanette olduğu için silinemez!");

			_context.Malzemeler.Remove(malzeme);
			await _context.SaveChangesAsync();
			return ServiceResult<bool>.Basarili(true, "Malzeme silindi.");
		}

		public async Task<ServiceResult<bool>> StokKontrolEtAsync(int malzemeId, int istenenAdet)
		{
			var malzeme = await _context.Malzemeler.FindAsync(malzemeId);
			if (malzeme == null) return ServiceResult<bool>.Basarisiz("Malzeme yok");

			if (malzeme.GuncelStok >= istenenAdet)
				return ServiceResult<bool>.Basarili(true);
			else
				return ServiceResult<bool>.Basarisiz($"Yetersiz stok. Mevcut: {malzeme.GuncelStok}");
		}
	}
}