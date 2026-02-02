using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Shared.Entities;
using KoudakMalzeme.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoudakMalzeme.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class KullanicilarController : ControllerBase
	{
		private readonly IAuthService _authService;

		public KullanicilarController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpGet]
		public async Task<IActionResult> GetList()
		{
			var result = await _authService.TumKullanicilariGetirAsync();
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var result = await _authService.GetirByIdAsync(id);
			if (result.BasariliMi) return Ok(result);
			return NotFound(result);
		}

		[HttpPut("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Update(int id, [FromBody] Kullanici model)
		{
			if (id != model.Id)
				return BadRequest(new { message = "ID uyuşmuyor." });

			var result = await _authService.UpdateUserAsync(model);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(int id)
		{
			var result = await _authService.DeleteUserAsync(id);
			if (result.BasariliMi) return Ok(result);
			return BadRequest(result);
		}
	}
}
