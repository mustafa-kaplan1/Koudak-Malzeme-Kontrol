using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Abstract
{
	public interface IAuthService
	{
		Task<ServiceResult<AuthResponseDto>> LoginAsync(UserLoginDto loginDto);
		Task<ServiceResult<Kullanici>> RegisterAsync(UserRegisterDto registerDto);

		// --- BU SATIRI SİLDİK: IlkGirisTamamlaAsync ---
		// Task<ServiceResult<bool>> IlkGirisTamamlaAsync(IlkGirisGuncellemeDto dto); 

		// Doğru metodun bu olduğunu varsayıyoruz (AuthManager'da bu var)
		Task<ServiceResult<bool>> IlkGirisGuncellemeAsync(IlkGirisGuncellemeDto dto);

		Task<ServiceResult<Kullanici>> AdminUyeEkleAsync(AdminUyeEkleDto dto);

		Task<ServiceResult<List<Kullanici>>> TumKullanicilariGetirAsync();
		Task<ServiceResult<Kullanici>> GetirByIdAsync(int id);
	}
}