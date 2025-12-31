using KoudakMalzeme.MvcUI.Models;
using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace KoudakMalzeme.MvcUI.Controllers
{
	[Authorize]
	public class KullaniciController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;

		public KullaniciController(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		// Üye Listesi
		public async Task<IActionResult> Index()
		{
			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync("api/kullanicilar");

			if (response.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<Kullanici>>>(options);
				return View(result?.Veri ?? new List<Kullanici>());
			}

			return View(new List<Kullanici>());
		}

		public async Task<IActionResult> Profil(int? id)
		{
			// 1. ID Kontrolü: Eğer ID gelmediyse, giriş yapan kullanıcının ID'sini al
			if (!id.HasValue)
			{
				var currentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (int.TryParse(currentIdClaim, out int parsedId))
				{
					id = parsedId;
				}
				else
				{
					return RedirectToAction("Index", "Home");
				}
			}

			var client = _httpClientFactory.CreateClient("ApiClient");
			var token = User.FindFirst("Token")?.Value;
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var model = new KullaniciProfilViewModel();

			// 2. Kullanıcı Bilgisi Getir
			var userResponse = await client.GetAsync($"api/kullanicilar/{id}");
			if (userResponse.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await userResponse.Content.ReadFromJsonAsync<ServiceResult<Kullanici>>(options);

				// EĞER KULLANICI BULUNAMADIYSA ÇÖKMEMESİ İÇİN KONTROL
				if (result?.Veri != null)
				{
					model.Kullanici = result.Veri;
				}
				else
				{
					TempData["Hata"] = "Kullanıcı bulunamadı.";
					return RedirectToAction("Index", "Home");
				}
			}
			else
			{
				TempData["Hata"] = "Kullanıcı bilgileri alınamadı.";
				return RedirectToAction("Index", "Home");
			}

			// 3. Emanet Bilgileri
			var emanetResponse = await client.GetAsync($"api/emanetler/uye/{id}");
			if (emanetResponse.IsSuccessStatusCode)
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var result = await emanetResponse.Content.ReadFromJsonAsync<ServiceResult<List<Emanet>>>(options);
				if (result?.Veri != null)
				{
					model.Emanetler = result.Veri;

					model.AktifZimmetListesi = model.Emanetler
						.Where(e => e.Durum != Shared.Enums.EmanetDurumu.Tamamlandi &&
									e.Durum != Shared.Enums.EmanetDurumu.IptalEdildi)
						.SelectMany(e => e.EmanetDetaylari.Select(d => new AktifZimmetOzetViewModel
						{
							MalzemeAdi = d.Malzeme?.Ad ?? "Bilinmeyen",
							Adet = d.KalanAdet,
							VerilisTarihi = e.TeslimAlmaTarihi,
							EmanetId = e.Id
						}))
						.Where(x => x.Adet > 0)
						.OrderBy(x => x.VerilisTarihi)
						.ToList();
				}
			}

			// 4. Kendi profili mi?
			var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (currentUserId != null && int.Parse(currentUserId) == id)
			{
				model.KendiProfiliMi = true;
			}

			return View(model);
		}
	}
}
