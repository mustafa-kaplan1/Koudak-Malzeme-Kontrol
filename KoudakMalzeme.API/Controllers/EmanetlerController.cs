using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KoudakMalzeme.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class EmanetlerController : ControllerBase
	{
		private readonly IEmanetService _emanetService;

		public EmanetlerController(IEmanetService emanetService)
		{
			_emanetService = emanetService;
		}

		[HttpPost("talep")]
		public async Task<IActionResult> TalepOlustur([FromBody] EmanetTalepOlusturDto dto)
		{
			// Token'dan kullanıcı ID'sini al
			var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
			int userId = int.Parse(userIdClaim);

			var result = await _emanetService.TalepOlusturAsync(userId, dto);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		[HttpGet("talepler")]
		[Authorize(Roles = "Admin,Malzemeci")] // Sadece yetkililer
		public async Task<IActionResult> BekleyenTalepler()
		{
			var result = await _emanetService.BekleyenTalepleriGetirAsync();
			return Ok(result);
		}

		[HttpPut("onayla")]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> TalepOnayla([FromBody] EmanetOnayDto dto)
		{
			var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			dto.PersonelId = int.Parse(userIdClaim!); // İşlemi yapan yetkili

			var result = await _emanetService.TalebiOnaylaAsync(dto);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		[HttpPut("reddet")]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> TalepReddet([FromBody] EmanetRedDto dto)
		{
			var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			dto.PersonelId = int.Parse(userIdClaim!);

			var result = await _emanetService.TalebiReddetAsync(dto);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		[HttpPost("iade-al")]
		public async Task<IActionResult> IadeAl(EmanetIadeIstegiDto istek)
		{
			var sonuc = await _emanetService.IadeAlAsync(istek);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
		}

		[HttpGet("aktif")]
		public async Task<IActionResult> AktifEmanetler()
		{
			var sonuc = await _emanetService.AktifEmanetleriGetirAsync();
			return Ok(sonuc);
		}

		[HttpGet("gecmis")]
		public async Task<IActionResult> GecmisEmanetler()
		{
			var sonuc = await _emanetService.GecmisEmanetleriGetirAsync();
			return Ok(sonuc);
		}

		[HttpGet("uye/{uyeId}")]
		public async Task<IActionResult> UyeEmanetleri(int uyeId)
		{
			var sonuc = await _emanetService.UyeEmanetleriniGetirAsync(uyeId);
			return Ok(sonuc);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetirById(int id)
		{
			var sonuc = await _emanetService.GetirByIdAsync(id);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
		}
	}
}