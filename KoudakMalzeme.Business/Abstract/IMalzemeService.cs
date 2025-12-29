using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Abstract
{
	public interface IMalzemeService
	{
		Task<ServiceResult<List<Malzeme>>> TumMalzemeleriGetirAsync();
		Task<ServiceResult<Malzeme>> GetirByIdAsync(int id);
		Task<ServiceResult<int>> EkleAsync(Malzeme malzeme);
		Task<ServiceResult<bool>> GuncelleAsync(Malzeme malzeme);
		Task<ServiceResult<bool>> SilAsync(int id);

		// Stok kontrolü için özel metot
		Task<ServiceResult<bool>> StokKontrolEtAsync(int malzemeId, int istenenAdet);
	}
}