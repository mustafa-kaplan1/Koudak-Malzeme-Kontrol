using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Business.Types;
using Microsoft.AspNetCore.Mvc;

namespace KoudakMalzeme.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;

		public AuthController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register(UserRegisterDto request)
		{
			var result = await _authService.RegisterAsync(request);
			if (!result.BasariliMi)
			{
				return BadRequest(result);
			}
			return Ok(result);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login(UserLoginDto request)
		{
			var result = await _authService.LoginAsync(request);
			if (!result.BasariliMi)
			{
				return BadRequest(result);
			}
			return Ok(result);
		}
	}
}
