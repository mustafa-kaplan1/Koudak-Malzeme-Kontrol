using KoudakMalzeme.Business.Types;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Abstract
{
	public interface IAuthService
	{
		Task<ServiceResult<AuthResponseDto>> LoginAsync(UserLoginDto loginDto);
		Task<ServiceResult<string>> AdminUyeEkleAsync(AdminUyeEkleDto dto);
		Task<ServiceResult<bool>> IlkGirisTamamlaAsync(IlkGirisGuncellemeDto dto);
	}
}