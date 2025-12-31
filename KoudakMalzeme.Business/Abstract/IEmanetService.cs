using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Abstract
{
	public interface IEmanetService
	{
		// --- TALEP VE ONAY SİSTEMİ (ANA AKIŞ) ---

		// Malzeme Alma Talebi Oluştur
		Task<ServiceResult<Emanet>> TalepOlusturAsync(int uyeId, EmanetTalepOlusturDto dto);

		// Malzeme İade Talebi Oluştur
		Task<ServiceResult<Emanet>> IadeTalepOlusturAsync(int uyeId, EmanetIadeTalepDto dto);

		// Bekleyen Tüm Talepleri (Alma ve İade) Getir
		Task<ServiceResult<List<Emanet>>> BekleyenTalepleriGetirAsync();

		// Talebi Onayla (Duruma göre stok düşer veya artar)
		Task<ServiceResult<bool>> TalebiOnaylaAsync(EmanetOnayDto dto);

		// Talebi Reddet
		Task<ServiceResult<bool>> TalebiReddetAsync(EmanetRedDto dto);


		// --- SORGULAMALAR ---

		// Tüm Geçmiş
		Task<ServiceResult<List<Emanet>>> GecmisEmanetleriGetirAsync();

		// Üyeye Özel Geçmiş
		Task<ServiceResult<List<Emanet>>> UyeEmanetleriniGetirAsync(int uyeId);

		// Aktif (Zimmetli) Kayıtlar
		Task<ServiceResult<List<Emanet>>> AktifEmanetleriGetirAsync();

		// Tekil Kayıt Getir
		Task<ServiceResult<Emanet>> GetirByIdAsync(int id);


		// --- MANUEL / YEDEK İŞLEMLER ---

		Task<ServiceResult<int>> EmanetVerAsync(EmanetVermeIstegiDto istek);
		Task<ServiceResult<int>> IadeAlAsync(EmanetIadeIstegiDto istek);
	}
}