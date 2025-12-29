using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Business.Types;
using Microsoft.AspNetCore.Authorization; // Authorize için
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

		// Sadece Adminler yeni üye ekleyebilir
		[HttpPost("admin-uye-ekle")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AdminUyeEkle(AdminUyeEkleDto request)
		{
			var result = await _authService.AdminUyeEkleAsync(request);
			if (!result.BasariliMi)
			{
				return BadRequest(result);
			}
			return Ok(result);
		}

		// Kullanıcı ilk girişinde şifresini ve bilgilerini günceller
		[HttpPost("ilk-giris-tamamla")]
		[Authorize] // Giriş yapmış olması lazım (Token sahibi olmalı)
		public async Task<IActionResult> IlkGirisTamamla(IlkGirisGuncellemeDto request)
		{
			var result = await _authService.IlkGirisTamamlaAsync(request);
			if (!result.BasariliMi)
			{
				return BadRequest(result);
			}
			return Ok(result);
		}
	}
}
