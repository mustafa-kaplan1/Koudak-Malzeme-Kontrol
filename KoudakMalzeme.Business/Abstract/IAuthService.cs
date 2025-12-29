using KoudakMalzeme.Business.Types;
using System.Threading.Tasks;

namespace KoudakMalzeme.Business.Abstract
{
	public interface IAuthService
	{
		Task<ServiceResult<AuthResponseDto>> RegisterAsync(UserRegisterDto registerDto);
		Task<ServiceResult<AuthResponseDto>> LoginAsync(UserLoginDto loginDto);
	}
}