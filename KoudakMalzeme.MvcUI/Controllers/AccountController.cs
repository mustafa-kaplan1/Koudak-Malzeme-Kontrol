using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.MvcUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace KoudakMalzeme.MvcUI.Controllers
{
	public class AccountController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;

		public AccountController(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		[HttpGet]
		public IActionResult Login()
		{
			if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			var client = _httpClientFactory.CreateClient("ApiClient");

			// API'ye göndermek için DTO oluştur
			var loginDto = new UserLoginDto { Email = model.Email, Password = model.Password };

			var response = await client.PostAsJsonAsync("api/auth/login", loginDto);

			if (response.IsSuccessStatusCode)
			{
				// API'den gelen veriyi oku
				var result = await response.Content.ReadFromJsonAsync<ServiceResult<AuthResponseDto>>();

				if (result != null && result.BasariliMi && result.Veri != null)
				{
					var veri = result.Veri;

					// --- COOKIE OLUŞTURMA (Oturum Açma) ---
					// Token içindeki ID'yi oku
					var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
					var jwt = handler.ReadJwtToken(veri.Token);
					var userId = jwt.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier)?.Value;

					var claims = new List<Claim>
					{
						new Claim("Token", veri.Token),
						new Claim(ClaimTypes.Name, veri.AdSoyad),
						new Claim(ClaimTypes.Role, veri.Rol),
						new Claim("IlkGirisYapildiMi", veri.IlkGirisYapildiMi.ToString()),
                        
                        // 2. ID'yi buraya ekliyoruz ki Layout ve diğer sayfalar okuyabilsin
                        new Claim(ClaimTypes.NameIdentifier, userId ?? "0")
					};

					var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
					var authProperties = new AuthenticationProperties
					{
						IsPersistent = true,
						ExpiresUtc = veri.Expiration
					};

					await HttpContext.SignInAsync(
						CookieAuthenticationDefaults.AuthenticationScheme,
						new ClaimsPrincipal(claimsIdentity),
						authProperties);

					// Yönlendirme Kontrolü
					if (!veri.IlkGirisYapildiMi)
					{
						return RedirectToAction("Kurulum");
					}

					return RedirectToAction("Index", "Home");
				}

				TempData["Hata"] = result?.Mesaj ?? "Giriş başarısız.";
			}
			else
			{
				TempData["Hata"] = "Sunucuya bağlanılamadı veya hatalı istek.";
			}

			return View(model);
		}

		[HttpGet]
		public IActionResult Kurulum()
		{
			// Bu sayfaya sadece giriş yapmış ama kurulum yapmamışlar erişebilir
			// Filtre zaten yönlendireceği için ekstra kontrole gerek yok ama güvenlik:
			if (!User.Identity!.IsAuthenticated) return RedirectToAction("Login");

			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Kurulum(IlkGirisViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			// Kullanıcının ID'sini Token'dan (Claim'den) bulmamız lazım.
			// API'deki AuthManager CreateToken metodunda "NameIdentifier" olarak ID koymuştuk.
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			// Eğer Claims'de bulamazsak (JWT decode edilmediyse), alternatif çözüm lazım.
			// Basitlik adına burada token'ı decode edelim:
			var token = User.FindFirst("Token")?.Value;
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jwt = handler.ReadJwtToken(token);
			var userId = jwt.Claims.First(c => c.Type == "nameid").Value;

			model.KullaniciId = int.Parse(userId);

			// API Çağrısı
			var client = _httpClientFactory.CreateClient("ApiClient");
			// Header'a Token ekle
			client.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var dto = new IlkGirisGuncellemeDto
			{
				KullaniciId = model.KullaniciId,
				Ad = model.Ad,
				Soyad = model.Soyad,
				Telefon = model.Telefon,
				YeniSifre = model.YeniSifre,
				YeniSifreTekrar = model.YeniSifreTekrar
			};

			var response = await client.PostAsJsonAsync("api/auth/ilk-giris-tamamla", dto);

			if (response.IsSuccessStatusCode)
			{
				// Başarılı! Çıkış yaptırıp tekrar giriş yaptırarak Claim'leri yenilemek en temizi.
				await HttpContext.SignOutAsync();
				TempData["Basari"] = "Kurulum tamamlandı! Lütfen yeni şifrenizle giriş yapın.";
				return RedirectToAction("Login");
			}

			TempData["Hata"] = "Bir hata oluştu.";
			return View(model);
		}

		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync();
			return RedirectToAction("Login");
		}

		public IActionResult ErisimEngellendi()
		{
			return View();
		}
	}
}