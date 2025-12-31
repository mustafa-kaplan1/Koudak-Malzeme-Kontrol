using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Headers;

namespace KoudakMalzeme.MvcUI.Controllers
{
	[Authorize] // Sadece giriş yapanlar görebilir
	public class MalzemelerController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;

		public MalzemelerController(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var client = _httpClientFactory.CreateClient("ApiClient");

			// Token'ı ekle
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync("api/malzemeler");

			if (response.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Malzeme>>>(options);

				if (result != null && result.BasariliMi)
				{
					return View(result.Veri);
				}
			}

			TempData["Hata"] = "Malzemeler yüklenirken bir hata oluştu.";
			return View(new List<Malzeme>());
		}
	}
}