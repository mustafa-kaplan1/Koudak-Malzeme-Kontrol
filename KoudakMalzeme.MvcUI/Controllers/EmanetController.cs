using KoudakMalzeme.MvcUI.Models;
using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using KoudakMalzeme.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace KoudakMalzeme.MvcUI.Controllers
{
	[Authorize]
	public class EmanetController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly JsonSerializerOptions _jsonOptions;

		public EmanetController(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
			_jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		}

		// --- YARDIMCI METOT ---
		private HttpClient CreateClient()
		{
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			if (!string.IsNullOrEmpty(token))
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}
			return client;
		}

		// 1. EMANET KAYITLARI / GEÇMİŞİ (LİSTELEME)
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var client = CreateClient();
			var isAdmin = User.IsInRole("Admin") || User.IsInRole("Malzemeci");

			// Admin ise tüm geçmişi, değilse sadece kendi geçmişini görsün
			// API tarafında 'gecmis' tüm kayıtları, 'uye/{id}' ise personelin kayıtlarını döner.
			string endpoint;

			if (isAdmin)
			{
				endpoint = "api/emanetler/gecmis";
			}
			else
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				endpoint = $"api/emanetler/uye/{userId}";
			}

			var response = await client.GetAsync(endpoint);

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Emanet>>>(_jsonOptions);
				return View(result?.Veri ?? new List<Emanet>());
			}

			TempData["Hata"] = "Emanet kayıtları yüklenirken bir hata oluştu.";
			return View(new List<Emanet>());
		}

		// 2. MALZEME TALEP ETME SAYFASI (KULLANICI)
		[HttpGet]
		public async Task<IActionResult> Al()
		{
			var client = CreateClient();
			var response = await client.GetAsync("api/malzemeler");

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Malzeme>>>(_jsonOptions);
				return View(result?.Veri ?? new List<Malzeme>());
			}

			TempData["Hata"] = "Malzemeler yüklenemedi.";
			return View(new List<Malzeme>());
		}

		// API: Talep Gönder
		[HttpPost]
		public async Task<IActionResult> TalepEt([FromBody] EmanetTalepOlusturDto dto)
		{
			var client = CreateClient();
			var response = await client.PostAsJsonAsync("api/emanetler/talep-et", dto);

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<Emanet>>(_jsonOptions);
				return Json(new { success = result?.BasariliMi, message = result?.Mesaj });
			}

			return Json(new { success = false, message = "Talep oluşturulurken sunucu hatası meydana geldi." });
		}

		// 3. BEKLEYEN TALEPLERİ YÖNETME (ADMİN/MALZEMECİ)
		[HttpGet]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Talepler()
		{
			var client = CreateClient();
			var response = await client.GetAsync("api/emanetler/bekleyen-talepler");

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Emanet>>>(_jsonOptions);
				return View(result?.Veri ?? new List<Emanet>());
			}

			TempData["Hata"] = "Talepler yüklenemedi.";
			return View(new List<Emanet>());
		}

		// API: Talebi Onayla
		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Onayla([FromBody] EmanetOnayDto dto)
		{
			var client = CreateClient();
			var response = await client.PostAsJsonAsync("api/emanetler/onayla", dto);

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<bool>>(_jsonOptions);
				return Json(new { success = result?.BasariliMi, message = result?.Mesaj });
			}

			return Json(new { success = false, message = "Onay işlemi başarısız." });
		}

		// API: Talebi Reddet
		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Reddet([FromBody] EmanetRedDto dto)
		{
			var client = CreateClient();
			var response = await client.PostAsJsonAsync("api/emanetler/reddet", dto);

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<bool>>(_jsonOptions);
				return Json(new { success = result?.BasariliMi, message = result?.Mesaj });
			}

			return Json(new { success = false, message = "Red işlemi başarısız." });
		}

		// 4. İADE ALMA İŞLEMİ (ADMİN/MALZEMECİ)
		[HttpGet]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> IadeAl(int id)
		{
			var client = CreateClient();
			var response = await client.GetAsync($"api/emanetler/{id}");

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<Emanet>>(_jsonOptions);
				if (result?.Veri != null)
				{
					var model = new EmanetIadeViewModel
					{
						EmanetId = result.Veri.Id,
						Emanet = result.Veri
					};
					return View(model);
				}
			}

			TempData["Hata"] = "Emanet kaydı bulunamadı.";
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> IadeAl(EmanetIadeViewModel model)
		{
			var client = CreateClient();
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			// ViewModel'den DTO'ya dönüştür
			var dto = new EmanetIadeIstegiDto
			{
				EmanetId = model.EmanetId,
				AlanPersonelId = int.Parse(userIdStr ?? "0"),
				IadeEdilenler = model.IadeAdetleri
					.Where(x => x.Value > 0) // Sadece 0'dan büyük girilenler
					.Select(x => new EmanetKalemDto { MalzemeId = x.Key, Adet = x.Value })
					.ToList()
			};

			var response = await client.PostAsJsonAsync("api/emanetler/iade-al", dto);

			if (response.IsSuccessStatusCode)
			{
				TempData["Basari"] = "İade başarıyla alındı.";
				return RedirectToAction("Index");
			}

			TempData["Hata"] = "İade işlemi sırasında hata oluştu.";
			return RedirectToAction("Index");
		}
	}
}
