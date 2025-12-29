using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

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

		[HttpPost("ver")]
		public async Task<IActionResult> EmanetVer(EmanetVermeIstegiDto istek)
		{
			var sonuc = await _emanetService.EmanetVerAsync(istek);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
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