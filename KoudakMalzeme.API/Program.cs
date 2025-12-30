using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Business.Concrete;
using KoudakMalzeme.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Servisler (Dependency Injection)
builder.Services.AddScoped<IMalzemeService, MalzemeManager>();
builder.Services.AddScoped<IEmanetService, EmanetManager>();
builder.Services.AddScoped<IAuthService, AuthManager>(); // YENİ: AuthManager eklendi

// 3. Authentication (Kimlik Doğrulama) Ayarları --- YENİ ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
				.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
			ValidateIssuer = false, // Şimdilik false (localhost)
			ValidateAudience = false // Şimdilik false
		};
	});

// 4. Controller
builder.Services.AddControllers().AddJsonOptions(options =>
{
	// İç içe veri çekerken (Malzeme->Emanet->Uye) döngü hatasını engeller
	options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// 5. Swagger Ayarları (Swagger'da 'Authorize' butonu çıksın diye) --- YENİ ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
	{
		Description = "Standart Authorization başlığı. Örnek: \"bearer {token}\"",
		In = ParameterLocation.Header,
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey
	});

	options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll",
		builder =>
		{
			builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
		});
});

var app = builder.Build();

// --- Pipeline ---

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication(); // kimlik doğrula
app.UseAuthorization();  // yetkiye bak

app.MapControllers();

app.Run();
