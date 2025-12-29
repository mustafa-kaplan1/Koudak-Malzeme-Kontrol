using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace KoudakMalzeme.MvcUI.Filters
{
	public class CheckFirstLoginAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			var user = context.HttpContext.User;

			// 1. Kullanıcı giriş yapmış mı?
			if (user.Identity != null && user.Identity.IsAuthenticated)
			{
				// 2. "Kurulum Tamamlandı mı?" Claim'ini bul.
				var ilkGirisClaim = user.FindFirst("IlkGirisYapildiMi")?.Value;

				// 3. Eğer False ise (Henüz kurulum yapmamışsa)
				if (ilkGirisClaim == "False")
				{
					// Şu an hangi sayfaya gitmeye çalışıyor?
					var controller = context.RouteData.Values["controller"]?.ToString();
					var action = context.RouteData.Values["action"]?.ToString();

					// Eğer zaten Kurulum sayfasındaysa veya Çıkış yapıyorsa dokunma.
					// Aksi takdirde her yerden Kurulum sayfasına yönlendir.
					if (!(controller == "Account" && (action == "Kurulum" || action == "Logout")))
					{
						context.Result = new RedirectToActionResult("Kurulum", "Account", null);
					}
				}
			}

			base.OnActionExecuting(context);
		}
	}
}
