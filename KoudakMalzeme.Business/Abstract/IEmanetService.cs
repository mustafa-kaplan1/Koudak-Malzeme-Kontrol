using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Abstract
{
	public interface IEmanetService
	{
		Task<ServiceResult<Emanet>> TalepOlusturAsync(int uyeId, EmanetTalepOlusturDto dto);
		Task<ServiceResult<List<Emanet>>> BekleyenTalepleriGetirAsync();
		Task<ServiceResult<bool>> TalebiOnaylaAsync(EmanetOnayDto dto);
		Task<ServiceResult<bool>> TalebiReddetAsync(EmanetRedDto dto);
		Task<ServiceResult<int>> EmanetVerAsync(EmanetVermeIstegiDto istek);
		Task<ServiceResult<int>> IadeAlAsync(EmanetIadeIstegiDto istek);
		Task<ServiceResult<List<Emanet>>> AktifEmanetleriGetirAsync();
		Task<ServiceResult<List<Emanet>>> GecmisEmanetleriGetirAsync();
		Task<ServiceResult<Emanet>> GetirByIdAsync(int id);
		Task<ServiceResult<List<Emanet>>> UyeEmanetleriniGetirAsync(int uyeId);
	}
}