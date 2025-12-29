using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace KoudakMalzeme.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class MalzemelerController : ControllerBase
	{
		private readonly IMalzemeService _malzemeService;

		public MalzemelerController(IMalzemeService malzemeService)
		{
			_malzemeService = malzemeService;
		}

		[HttpGet]
		public async Task<IActionResult> TumunuGetir()
		{
			var sonuc = await _malzemeService.TumMalzemeleriGetirAsync();
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetirById(int id)
		{
			var sonuc = await _malzemeService.GetirByIdAsync(id);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return NotFound(sonuc);
		}

		[HttpPost]
		public async Task<IActionResult> Ekle(Malzeme malzeme)
		{
			var sonuc = await _malzemeService.EkleAsync(malzeme);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
		}

		[HttpPut]
		public async Task<IActionResult> Guncelle(Malzeme malzeme)
		{
			var sonuc = await _malzemeService.GuncelleAsync(malzeme);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Sil(int id)
		{
			var sonuc = await _malzemeService.SilAsync(id);
			if (sonuc.BasariliMi) return Ok(sonuc);
			return BadRequest(sonuc);
		}
	}
}