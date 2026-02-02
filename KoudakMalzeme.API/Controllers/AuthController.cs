using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
			if (!result.BasariliMi) return BadRequest(result);
			return Ok(result);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login(UserLoginDto request)
		{
			var result = await _authService.LoginAsync(request);
			if (!result.BasariliMi) return BadRequest(result);
			return Ok(result);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost("admin-uye-ekle")]
		public async Task<IActionResult> AdminUyeEkle(AdminUyeEkleDto request)
		{
			var result = await _authService.AdminUyeEkleAsync(request);
			if (!result.BasariliMi) return BadRequest(result);
			return Ok(result);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost("admin-guncelle-sifre")]
		public async Task<IActionResult> AdminGuncelleSifre(KoudakMalzeme.Shared.Dtos.AdminGuncelleSifreDto request)
		{
			var result = await _authService.AdminGuncelleSifreAsync(request);
			if (!result.BasariliMi) return BadRequest(result);
			return Ok(result);
		}

		[HttpPost("ilk-giris-tamamla")]
		public async Task<IActionResult> IlkGirisTamamla(IlkGirisGuncellemeDto request)
		{
			// Artık doğru ismi çağırıyoruz: IlkGirisGuncellemeAsync
			var result = await _authService.IlkGirisGuncellemeAsync(request);

			if (!result.BasariliMi) return BadRequest(result);
			return Ok(result);
		}
	}
}
