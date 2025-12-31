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

		// DÜZELTME 1: Adres "talep" -> "talep-et" yapıldı.
		[HttpPost("talep-et")]
		public async Task<IActionResult> TalepOlustur([FromBody] EmanetTalepOlusturDto dto)
		{
			var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
			int userId = int.Parse(userIdClaim);

			var result = await _emanetService.TalepOlusturAsync(userId, dto);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		// DÜZELTME 2: Adres "talepler" -> "bekleyen-talepler" yapıldı.
		[HttpGet("bekleyen-talepler")]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> BekleyenTalepler()
		{
			var result = await _emanetService.BekleyenTalepleriGetirAsync();
			return Ok(result);
		}

		// DÜZELTME 3: [HttpPut] -> [HttpPost] yapıldı (MVC Post gönderdiği için).
		[HttpPost("onayla")]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> TalepOnayla([FromBody] EmanetOnayDto dto)
		{
			var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			// null kontrolü eklendi
			dto.PersonelId = int.Parse(userIdClaim ?? "0");

			var result = await _emanetService.TalebiOnaylaAsync(dto);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		// DÜZELTME 4: [HttpPut] -> [HttpPost] yapıldı.
		[HttpPost("reddet")]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> TalepReddet([FromBody] EmanetRedDto dto)
		{
			var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			dto.PersonelId = int.Parse(userIdClaim ?? "0");

			var result = await _emanetService.TalebiReddetAsync(dto);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		[HttpPost("iade-talep")]
		public async Task<IActionResult> IadeTalepOlustur([FromBody] EmanetIadeTalepDto dto)
		{
			var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
			int userId = int.Parse(userIdClaim);

			var result = await _emanetService.IadeTalepOlusturAsync(userId, dto);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		[HttpPost("iade-al")]
		public async Task<IActionResult> IadeAl([FromBody] EmanetIadeIstegiDto istek)
		{
			var sonuc = await _emanetService.IadeAlAsync(istek);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
		}

		// Mevcut diğer GET metodları (Bunlarda sorun yoktu)
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