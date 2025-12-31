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

		// 1. EMANET KAYITLARI (LİSTELEME)
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			try
			{
				var client = CreateClient();
				var isAdmin = User.IsInRole("Admin") || User.IsInRole("Malzemeci");
				string endpoint = isAdmin ? "api/emanetler/gecmis" : $"api/emanetler/uye/{User.FindFirst(ClaimTypes.NameIdentifier)?.Value}";

				var response = await client.GetAsync(endpoint);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Emanet>>>(_jsonOptions);
					return View(result?.Veri ?? new List<Emanet>());
				}
			}
			catch (Exception)
			{
				// API kapalıysa hata vermesin, boş liste dönsün
			}

			TempData["Hata"] = "Veriler yüklenemedi. API bağlantısını kontrol edin.";
			return View(new List<Emanet>());
		}

		// 2. MALZEME TALEP ETME (KULLANICI) - "Al" Sayfası
		[HttpGet]
		public async Task<IActionResult> Al()
		{
			try
			{
				var client = CreateClient();
				var response = await client.GetAsync("api/malzemeler");

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Malzeme>>>(_jsonOptions);
					return View(result?.Veri ?? new List<Malzeme>());
				}
				TempData["Hata"] = "Malzemeler API'den çekilemedi.";
			}
			catch (Exception ex)
			{
				TempData["Hata"] = "Sunucuya bağlanılamadı: " + ex.Message;
			}

			return View(new List<Malzeme>());
		}

		// API: Talep Gönder
		[HttpPost]
		public async Task<IActionResult> TalepEt([FromBody] EmanetTalepOlusturDto dto)
		{
			try
			{
				var client = CreateClient();
				var response = await client.PostAsJsonAsync("api/emanetler/talep-et", dto);

				// API'den gelen yanıtı (Başarılı da olsa Hatalı da olsa) okumaya çalışıyoruz
				// Çünkü EmanetManager başarısız durumda da ServiceResult içinde mesaj dönüyor.
				ServiceResult<Emanet>? result = null;
				try
				{
					result = await response.Content.ReadFromJsonAsync<ServiceResult<Emanet>>(_jsonOptions);
				}
				catch
				{
					// Eğer JSON okuyamazsa (örneğin 401 Unauthorized HTML dönerse) null kalır.
				}

				if (response.IsSuccessStatusCode && result != null && result.BasariliMi)
				{
					return Json(new { success = true, message = result.Mesaj });
				}
				else
				{
					// Hata mesajını API'den al, yoksa genel hata göster
					string hataMesaji = result?.Mesaj ?? $"İşlem başarısız. (Sunucu Yanıtı: {response.StatusCode})";

					if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
						hataMesaji = "Oturum süreniz dolmuş, lütfen tekrar giriş yapın.";

					return Json(new { success = false, message = hataMesaji });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bağlantı hatası: " + ex.Message });
			}
		}

		[HttpGet]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Ver()
		{
			var model = new EmanetVerViewModel();
			try
			{
				var client = CreateClient();

				// Malzemeleri Çek
				var matResponse = await client.GetAsync("api/malzemeler");
				if (matResponse.IsSuccessStatusCode)
				{
					var result = await matResponse.Content.ReadFromJsonAsync<ServiceResult<List<Malzeme>>>(_jsonOptions);
					if (result?.Veri != null) model.Malzemeler = result.Veri;
				}
			}
			catch
			{
				TempData["Hata"] = "API bağlantısı sağlanamadı.";
			}
			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Ver([FromBody] EmanetVermeIstegiDto istek)
		{
			try
			{
				var client = CreateClient();
				// Veren Personel ID'sini (Giriş yapan kullanıcı) ekle
				var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (!string.IsNullOrEmpty(userIdStr)) istek.VerenPersonelId = int.Parse(userIdStr);

				// API'de eski 'ver' endpoint'i duruyor mu? Yoksa 'onayla' mı kullanılmalı?
				// Manager koduna göre 'EmanetVerAsync' metodunuz varsa 'api/emanetler/ver' çalışır.
				var response = await client.PostAsJsonAsync("api/emanetler/ver", istek);

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<ServiceResult<int>>(_jsonOptions);
					return Json(new { basarili = result?.BasariliMi, mesaj = result?.Mesaj });
				}
			}
			catch
			{
				return Json(new { basarili = false, mesaj = "Sunucu hatası." });
			}

			return Json(new { basarili = false, mesaj = "İşlem başarısız." });
		}

		// 4. BEKLEYEN TALEPLERİ YÖNETME
		[HttpGet]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Talepler()
		{
			try
			{
				var client = CreateClient();
				var response = await client.GetAsync("api/emanetler/bekleyen-talepler");

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Emanet>>>(_jsonOptions);
					return View(result?.Veri ?? new List<Emanet>());
				}
			}
			catch { }
			return View(new List<Emanet>());
		}

		// API: Onayla
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
			return Json(new { success = false, message = "Hata oluştu." });
		}

		// API: Reddet
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
			return Json(new { success = false, message = "Hata oluştu." });
		}

		[HttpGet]
		public async Task<IActionResult> IadeVer()
		{
			var client = CreateClient();
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			// Kullanıcının kendi emanetlerini çekiyoruz
			var response = await client.GetAsync($"api/emanetler/uye/{userId}");

			// Sadece aktif (TeslimEdildi) olanları ve miktarı 0'dan büyük olanları filtreleyip View'a göndereceğiz
			// API tüm geçmişi döner, filtrelemeyi burada veya backend'de yapabiliriz.
			// Kolaylık olsun diye tüm listeyi View'a gönderip orada filtreleyeceğiz veya ViewModel kullanacağız.

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Emanet>>>(_jsonOptions);
				var tumEmanetler = result?.Veri ?? new List<Emanet>();

				// View tarafında daha rahat işlem yapmak için sadece detayları düzleştirip gönderebiliriz
				// Ancak basitlik adına direkt listeyi gönderelim.
				return View(tumEmanetler);
			}

			return View(new List<Emanet>());
		}

		// 2. İade Talebi Gönder (Post)
		[HttpPost]
		public async Task<IActionResult> IadeTalepEt([FromBody] EmanetIadeTalepDto dto)
		{
			var client = CreateClient();
			var response = await client.PostAsJsonAsync("api/emanetler/iade-talep", dto);

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<Emanet>>(_jsonOptions);
				return Json(new { success = result?.BasariliMi, message = result?.Mesaj });
			}

			// Hata mesajını okumaya çalış
			try
			{
				var errResult = await response.Content.ReadFromJsonAsync<ServiceResult<object>>(_jsonOptions);
				return Json(new { success = false, message = errResult?.Mesaj ?? "İşlem başarısız." });
			}
			catch
			{
				return Json(new { success = false, message = "Sunucu hatası." });
			}
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> IadeAl(EmanetIadeViewModel model)
		{
			var client = CreateClient();
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			var dto = new EmanetIadeIstegiDto
			{
				EmanetId = model.EmanetId,
				AlanPersonelId = int.Parse(userIdStr ?? "0"),
				IadeEdilenler = model.IadeAdetleri
					.Where(x => x.Value > 0)
					.Select(x => new EmanetKalemDto { MalzemeId = x.Key, Adet = x.Value })
					.ToList()
			};

			var response = await client.PostAsJsonAsync("api/emanetler/iade-al", dto);
			if (response.IsSuccessStatusCode)
			{
				TempData["Basari"] = "İade alındı.";
				return RedirectToAction("Index");
			}
			TempData["Hata"] = "İade başarısız.";
			return RedirectToAction("Index");
		}
	}
}
