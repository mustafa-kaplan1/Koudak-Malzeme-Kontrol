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

		// Malzeme Listesi (Herkes Görebilir)
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

		// Ekleme Sayfası (Sadece Admin ve Malzemeci)
		[HttpGet]
		[Authorize(Roles = "Admin,Malzemeci")]
		public IActionResult Ekle()
		{
			return View();
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Ekle(Malzeme malzeme)
		{
			// Formdan gelen veride ID 0 olur, sorun yok.
			// Stok mantığını business katmanı hallediyor (Guncel = Toplam).

			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.PostAsJsonAsync("api/malzemeler", malzeme);

			if (response.IsSuccessStatusCode)
			{
				TempData["Basari"] = "Malzeme başarıyla eklendi.";
				return RedirectToAction("Index");
			}
			else
			{
				// API'den dönen gerçek hatayı okuyup ekrana basalım
				var hataMesaji = await response.Content.ReadAsStringAsync();
				TempData["Hata"] = $"Ekleme başarısız: {hataMesaji}";
			}
			return View(malzeme);
		}

		// Düzenleme Sayfası
		[HttpGet]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Duzenle(int id)
		{
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync($"api/malzemeler/{id}");

			if (response.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<Malzeme>>(options);

				if (result != null && result.BasariliMi)
				{
					return View(result.Veri);
				}
			}

			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Duzenle(Malzeme malzeme)
		{
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.PutAsJsonAsync("api/malzemeler", malzeme);

			if (response.IsSuccessStatusCode)
			{
				TempData["Basari"] = "Malzeme güncellendi.";
				return RedirectToAction("Index");
			}

			TempData["Hata"] = "Güncelleme başarısız.";
			return View(malzeme);
		}

		// Silme İşlemi
		[HttpPost] // Güvenlik için GET yerine POST tercih edilir
		[Authorize(Roles = "Admin")] // Sadece Admin silebilir
		public async Task<IActionResult> Sil(int id)
		{
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.DeleteAsync($"api/malzemeler/{id}");

			if (response.IsSuccessStatusCode)
			{
				// Result'ı okuyup mesajı alabiliriz
				TempData["Basari"] = "Malzeme silindi.";
			}
			else
			{
				TempData["Hata"] = "Silinemedi. Malzeme kullanımda olabilir.";
			}

			return RedirectToAction("Index");
		}
	}
}