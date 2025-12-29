using KoudakMalzeme.Business.Types;
using KoudakMalzeme.Shared.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Abstract
{
	public interface IEmanetService
	{
		// Emanet Verme İşlemi
		Task<ServiceResult<int>> EmanetVerAsync(EmanetVermeIstegi istek);

		// İade Alma İşlemi
		Task<ServiceResult<bool>> IadeAlAsync(EmanetIadeIstegi istek);

		// Sorgulamalar
		Task<ServiceResult<List<Emanet>>> AktifEmanetleriGetirAsync();
		Task<ServiceResult<List<Emanet>>> GecmisEmanetleriGetirAsync();
		Task<ServiceResult<Emanet>> GetirByIdAsync(int id);

		// Bir üyenin üzerindeki malzemeler
		Task<ServiceResult<List<Emanet>>> UyeEmanetleriniGetirAsync(int uyeId);
	}
}