using KoudakMalzeme.Business.Abstract;
using KoudakMalzeme.Shared.Dtos;
using KoudakMalzeme.DataAccess;
using KoudakMalzeme.Shared.Entities;
using KoudakMalzeme.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
			var user = await _context.Kullanicilar.FirstOrDefaultAsync(x => x.Email == loginDto.Email);
			if (user == null)
				return ServiceResult<AuthResponseDto>.Basarisiz("Kullanıcı bulunamadı.");

			if (!VerifyPasswordHash(loginDto.Password, user.PasswordHash, user.PasswordSalt))
				return ServiceResult<AuthResponseDto>.Basarisiz("Şifre hatalı.");

			// HATA BURADAYDI: Doğrudan CreateToken çağırıyoruz.
			return CreateToken(user);
		}

		public async Task<ServiceResult<string>> AdminUyeEkleAsync(AdminUyeEkleDto dto)
		{
			if (await UserExists(dto.Email))
				return ServiceResult<string>.Basarisiz("Bu e-posta zaten kayıtlı.");

			// Rastgele Şifre (6 haneli)
			string geciciSifre = new Random().Next(100000, 999999).ToString();

			CreatePasswordHash(geciciSifre, out byte[] passwordHash, out byte[] passwordSalt);

			var user = new Kullanici
			{
				OkulNo = dto.OkulNo,
				Email = dto.Email,
				Ad = dto.Ad ?? "",
				Soyad = dto.Soyad ?? "",
				Telefon = "",
				PasswordHash = passwordHash,
				PasswordSalt = passwordSalt,
				Rol = KullaniciRolu.Uye,
				IlkGirisYapildiMi = false // Zorunlu güncelleme bayrağı
			};

			_context.Kullanicilar.Add(user);
			await _context.SaveChangesAsync();

			return ServiceResult<string>.Basarili(geciciSifre, "Kullanıcı oluşturuldu. Geçici Şifre: " + geciciSifre);
		}

		public async Task<ServiceResult<bool>> IlkGirisTamamlaAsync(IlkGirisGuncellemeDto dto)
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

			user.IlkGirisYapildiMi = true; // KİLİT NOKTA

			_context.Kullanicilar.Update(user);
			await _context.SaveChangesAsync();

			return ServiceResult<bool>.Basarili(true, "Hesap kurulumu tamamlandı.");
		}

		private async Task<bool> UserExists(string email)
		{
			return await _context.Kullanicilar.AnyAsync(x => x.Email == email);
		}

		private ServiceResult<AuthResponseDto> CreateToken(Kullanici user)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Name, $"{user.Ad} {user.Soyad}"),
				new Claim(ClaimTypes.Role, user.Rol.ToString())
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
				_configuration.GetSection("AppSettings:Token").Value ?? "bu_varsayilan_cok_uzun_bir_gizli_anahtardir_en_az_64_karakter_olmali_ki_guvenli_olsun_123456789"));

			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.Now.AddDays(1),
				SigningCredentials = creds
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var token = tokenHandler.CreateToken(tokenDescriptor);
			var tokenString = tokenHandler.WriteToken(token);

			return ServiceResult<AuthResponseDto>.Basarili(new AuthResponseDto
			{
				Token = tokenString,
				AdSoyad = $"{user.Ad} {user.Soyad}".Trim(),
				Rol = user.Rol.ToString(),
				IlkGirisYapildiMi = user.IlkGirisYapildiMi, // EKLENDİ
				Expiration = tokenDescriptor.Expires.Value
			}, "Giriş başarılı.");
		}

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
				for (int i = 0; i < computedHash.Length; i++)
				{
					if (computedHash[i] != storedHash[i]) return false;
				}
			}
			return true;
		}
	}
}
