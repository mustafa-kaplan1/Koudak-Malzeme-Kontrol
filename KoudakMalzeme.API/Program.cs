using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Business.Concrete;
using KoudakMalzeme.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Bağlantısı (DbContext)
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Servislerin Kaydedilmesi (Dependency Injection)
// "Biri senden IMalzemeService isterse, ona MalzemeManager ver" diyoruz.
builder.Services.AddScoped<IMalzemeService, MalzemeManager>();
builder.Services.AddScoped<IEmanetService, EmanetManager>();

// 3. Controller'ları ekle
builder.Services.AddControllers();

// 4. Swagger (API Dokümantasyonu)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. CORS Ayarları (Web sitesinin API'ye erişebilmesi için)
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll",
		builder =>
		{
			builder.AllowAnyOrigin()
				   .AllowAnyMethod()
				   .AllowAnyHeader();
		});
});

var app = builder.Build();

// --- HTTP Request Pipeline ---

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll"); // CORS'u aktif et

app.UseAuthorization();

app.MapControllers();

app.Run();
