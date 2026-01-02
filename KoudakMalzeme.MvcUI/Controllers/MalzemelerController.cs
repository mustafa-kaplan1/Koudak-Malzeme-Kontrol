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

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Yonetim()
		{
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync("api/malzemeler");

			if (response.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Malzeme>>>(options);
				return View(result?.Veri ?? new List<Malzeme>());
			}

			return View(new List<Malzeme>());
		}

		// ... (Mevcut kodların altına ekleyin) ...

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult Ekle()
		{
			return View(new Malzeme());
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> Ekle(Malzeme model)
		{
			// Stok başlangıçta eşittir
			model.GuncelStok = model.ToplamStok;

			if (!ModelState.IsValid)
				return View(model);

			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.PostAsJsonAsync("api/malzemeler", model);

			if (response.IsSuccessStatusCode)
			{
				TempData["Basarili"] = "Malzeme başarıyla eklendi.";
				return RedirectToAction("Yonetim");
			}

			TempData["Hata"] = "Malzeme eklenirken bir hata oluştu.";
			return View(model);
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IActionResult> Duzenle(int id)
		{
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync($"api/malzemeler/{id}");
			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<Malzeme>>();
				return View(result?.Veri);
			}

			return RedirectToAction("Yonetim");
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> Duzenle(Malzeme model)
		{
			// Modelde GecmisEmanetler gibi alanlar null gelebilir, bunları Entity'den temizlemek gerekebilir
			// Ancak API tarafı genellikle sadece gerekli alanları alır.

			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Güncel stok hesabı kritik olduğu için, genellikle backend'de ayrıca kontrol edilir.
			// Burada basitçe gönderiyoruz.
			var response = await client.PutAsJsonAsync($"api/malzemeler/{model.Id}", model);

			if (response.IsSuccessStatusCode)
			{
				TempData["Basarili"] = "Malzeme güncellendi.";
				return RedirectToAction("Yonetim");
			}

			TempData["Hata"] = "Güncelleme başarısız.";
			return View(model);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> TopluSil([FromBody] List<int> ids)
		{
			if (ids == null || !ids.Any())
				return Json(new { success = false, message = "Seçim yapılmadı." });

			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			int count = 0;
			foreach (var id in ids)
			{
				var response = await client.DeleteAsync($"api/malzemeler/{id}");
				if (response.IsSuccessStatusCode) count++;
			}

			if (count > 0)
				return Json(new { success = true, message = $"{count} malzeme silindi." });

			return Json(new { success = false, message = "Silinemedi." });
		}
	}
}