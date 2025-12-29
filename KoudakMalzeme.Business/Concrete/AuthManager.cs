using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.DataAccess;
using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.Shared.Entities;
using KoudakMalzeme.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace KoudakMalzeme.Business.Concrete
{
	public class AuthManager : IAuthService
	{
		private readonly AppDbContext _context;
		private readonly IConfiguration _configuration;

		public AuthManager(AppDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}

		public async Task<ServiceResult<AuthResponseDto>> LoginAsync(UserLoginDto loginDto)
		{
			var user = await _context.Kullanicilar.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
			if (user == null)
				return ServiceResult<AuthResponseDto>.Basarisiz("Kullanıcı bulunamadı.");

			if (!VerifyPasswordHash(loginDto.Password, user.PasswordHash, user.PasswordSalt))
				return ServiceResult<AuthResponseDto>.Basarisiz("Şifre hatalı.");

			string token = CreateToken(user);

			var response = new AuthResponseDto
			{
				Token = token,
				AdSoyad = $"{user.Ad} {user.Soyad}",
				Rol = user.Rol.ToString(),
				IlkGirisYapildiMi = user.IlkGirisYapildiMi,
				Expiration = DateTime.Now.AddDays(1)
			};

			return ServiceResult<AuthResponseDto>.Basarili(response);
		}

		public async Task<ServiceResult<Kullanici>> RegisterAsync(UserRegisterDto registerDto)
		{
			if (await _context.Kullanicilar.AnyAsync(u => u.Email == registerDto.Email))
				return ServiceResult<Kullanici>.Basarisiz("Bu e-posta zaten kayıtlı.");

			CreatePasswordHash(registerDto.Password, out byte[] passwordHash, out byte[] passwordSalt);

			var user = new Kullanici
			{
				OkulNo = registerDto.OkulNo,
				Ad = registerDto.Ad,
				Soyad = registerDto.Soyad,
				Email = registerDto.Email,
				Telefon = registerDto.Telefon,
				Rol = KullaniciRolu.Uye,
				PasswordHash = passwordHash,
				PasswordSalt = passwordSalt,
				IlkGirisYapildiMi = false // İlk girişte şifre değiştirmeli
			};

			_context.Kullanicilar.Add(user);
			await _context.SaveChangesAsync();

			return ServiceResult<Kullanici>.Basarili(user, "Kullanıcı başarıyla oluşturuldu.");
		}

		public async Task<ServiceResult<Kullanici>> AdminUyeEkleAsync(AdminUyeEkleDto dto)
		{
			if (await _context.Kullanicilar.AnyAsync(u => u.Email == dto.Email))
				return ServiceResult<Kullanici>.Basarisiz("Bu e-posta kullanımda.");

			if (await _context.Kullanicilar.AnyAsync(u => u.OkulNo == dto.OkulNo))
				return ServiceResult<Kullanici>.Basarisiz("Bu okul numarası kullanımda.");

			// Geçici şifre: Okul numarasının son 4 hanesi veya sabit bir değer
			string geciciSifre = "Koudak123!";
			CreatePasswordHash(geciciSifre, out byte[] passwordHash, out byte[] passwordSalt);

			var user = new Kullanici
			{
				OkulNo = dto.OkulNo,
				Email = dto.Email,
				Ad = dto.Ad ?? "",
				Soyad = dto.Soyad ?? "",
				Telefon = "", // Boş bırakılabilir veya dummy data
				Rol = KullaniciRolu.Uye,
				PasswordHash = passwordHash,
				PasswordSalt = passwordSalt,
				IlkGirisYapildiMi = false,
				OlusturulmaTarihi = DateTime.Now
			};

			_context.Kullanicilar.Add(user);
			await _context.SaveChangesAsync();

			return ServiceResult<Kullanici>.Basarili(user, $"Üye eklendi. Geçici Şifre: {geciciSifre}");
		}

		public async Task<ServiceResult<bool>> IlkGirisGuncellemeAsync(IlkGirisGuncellemeDto dto)
		{
			var user = await _context.Kullanicilar.FindAsync(dto.KullaniciId);
			if (user == null) return ServiceResult<bool>.Basarisiz("Kullanıcı bulunamadı.");

			if (dto.YeniSifre != dto.YeniSifreTekrar)
				return ServiceResult<bool>.Basarisiz("Şifreler uyuşmuyor.");

			CreatePasswordHash(dto.YeniSifre, out byte[] passwordHash, out byte[] passwordSalt);

			user.Ad = dto.Ad;
			user.Soyad = dto.Soyad;
			user.Telefon = dto.Telefon;
			user.PasswordHash = passwordHash;
			user.PasswordSalt = passwordSalt;
			user.IlkGirisYapildiMi = true; // Artık kurulum tamamlandı

			await _context.SaveChangesAsync();

			return ServiceResult<bool>.Basarili(true);
		}

		// --- YENİ EKLENEN METOTLAR (DOĞRU YERDE) ---

		public async Task<ServiceResult<List<Kullanici>>> TumKullanicilariGetirAsync()
		{
			var kullanicilar = await _context.Kullanicilar
				.OrderBy(u => u.Ad)
				.ToListAsync();

			return ServiceResult<List<Kullanici>>.Basarili(kullanicilar);
		}

		public async Task<ServiceResult<Kullanici>> GetirByIdAsync(int id)
		{
			var kullanici = await _context.Kullanicilar.FindAsync(id);

			if (kullanici == null)
				return ServiceResult<Kullanici>.Basarisiz("Kullanıcı bulunamadı.");

			return ServiceResult<Kullanici>.Basarili(kullanici);
		}

		// --- Yardımcı Metotlar ---

		private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
		{
			using (var hmac = new HMACSHA512())
			{
				passwordSalt = hmac.Key;
				passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
			}
		}

		private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
		{
			using (var hmac = new HMACSHA512(storedSalt))
			{
				var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
				return computedHash.SequenceEqual(storedHash);
			}
		}

		private string CreateToken(Kullanici user)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, $"{user.Ad} {user.Soyad}"),
				new Claim(ClaimTypes.Role, user.Rol.ToString())
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value!));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

			var token = new JwtSecurityToken(
				claims: claims,
				expires: DateTime.Now.AddDays(1),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
