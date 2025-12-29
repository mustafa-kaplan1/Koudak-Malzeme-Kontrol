using KoudakMalzeme.MvcUI.Models;
using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KoudakMalzeme.MvcUI.Controllers
{
	[Authorize]
	public class EmanetController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;

		public EmanetController(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		// 1. Emanet Verme Sayfası (GET)
		[HttpGet]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Ver()
		{
			var model = new EmanetVerViewModel();
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Üyeleri Getir (Api'de tüm kullanıcıları getiren bir endpoint lazım veya filtreli)
			// Not: KullaniciController yazmadığımız için şimdilik admin yetkisiyle API'den çekeceğiz.
			// Eğer API'de 'api/kullanicilar' yoksa eklememiz gerekebilir ama şimdilik varsayalım.
			// (Shared katmanındaki UserDtos burada işe yarayacak)

			// Malzemeleri Getir
			var matResponse = await client.GetAsync("api/malzemeler");
			if (matResponse.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await matResponse.Content.ReadFromJsonAsync<ServiceResult<List<Malzeme>>>(options);
				if (result?.Veri != null) model.Malzemeler = result.Veri;
			}

			// NOT: Üye listesi için geçici bir çözüm yapıyoruz.
			// Gerçek projede 'api/kullanicilar' endpointi olmalı.
			// Şimdilik boş liste gönderiyorum, aşağıda View tarafında AJAX ile arama yapılabilir 
			// veya API'ye basit bir 'GetUsers' ekleyebiliriz. 
			// *Senin için API'ye dokunmadan View tarafında ID girişi ile çözeceğim.*

			return View(model);
		}

		// 2. Emanet İşlemini Kaydet (POST)
		[HttpPost]
		[Authorize(Roles = "Admin,Malzemeci")]
		public async Task<IActionResult> Ver([FromBody] EmanetVermeIstegiDto istek)
		{
			// Bu metot AJAX (JSON) ile çağrılacak
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Veren Personel ID'sini (Giriş yapan kullanıcı) ekle
			var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (!string.IsNullOrEmpty(userIdStr)) istek.VerenPersonelId = int.Parse(userIdStr);

			var response = await client.PostAsJsonAsync("api/emanetler/ver", istek);

			if (response.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<int>>(options);
				return Json(new { basarili = true, mesaj = result?.Mesaj });
			}

			return Json(new { basarili = false, mesaj = "İşlem sırasında hata oluştu." });
		}
	}
}
