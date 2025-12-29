using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. MVC Servisleri
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("ApiClient", client =>
{
	client.BaseAddress = new Uri("http://localhost:5016/");
});

// 3. Cookie Authentication (MVC tarafında oturum tutma)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Account/Login"; // Giriş yapmamışsa buraya at
		options.LogoutPath = "/Account/Logout";
		options.AccessDeniedPath = "/Account/ErisimEngellendi"; // Yetkisi yoksa
		options.Cookie.Name = "KoudakAuth";
		options.ExpireTimeSpan = TimeSpan.FromDays(1);
	});

// 4. Session (Geçici veri taşıma için)
builder.Services.AddSession();

var app = builder.Build();

// --- Pipeline ---

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot klasörünü açar

app.UseRouting();

app.UseAuthentication(); // Kimlik Doğrulama
app.UseAuthorization();  // Yetkilendirme

app.UseSession(); // Session'ı aktif et

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
