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

			// Geçici şifre: admin tarafından verildiği gibi kullanılacak veya oluşturulacak
			string geciciSifre;
			if (dto.GenerateRandom)
			{
				geciciSifre = GenerateStrongPassword();
			}
			else if (!string.IsNullOrEmpty(dto.Password))
			{
				geciciSifre = dto.Password!;
			}
			else
			{
				// Eski davranış: sabit geçici şifre
				geciciSifre = "Koudak123!";
			}
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
				GeçiciŞifre = geciciSifre, // Geçici şifreyi kaydet
				OlusturulmaTarihi = DateTime.Now
			};

			_context.Kullanicilar.Add(user);
			await _context.SaveChangesAsync();

			return ServiceResult<Kullanici>.Basarili(user, $"Üye eklendi. Geçici Şifre: {geciciSifre}");
		}

		public async Task<ServiceResult<bool>> AdminGuncelleSifreAsync(KoudakMalzeme.Shared.Dtos.AdminGuncelleSifreDto dto)
		{
			var user = await _context.Kullanicilar.FindAsync(dto.KullaniciId);
			if (user == null) return ServiceResult<bool>.Basarisiz("Kullanıcı bulunamadı.");

			if (user.IlkGirisYapildiMi)
			{
				return ServiceResult<bool>.Basarisiz("Bu kullanıcının şifresi kullanıcı tarafından zaten belirlenmiş.");
			}

			string yeniSifre;
			if (dto.GenerateRandom)
			{
				yeniSifre = GenerateStrongPassword();
			}
			else if (!string.IsNullOrEmpty(dto.YeniSifre))
			{
				yeniSifre = dto.YeniSifre!;
			}
			else
			{
				return ServiceResult<bool>.Basarisiz("Yeni şifre belirtilmedi.");
			}

			CreatePasswordHash(yeniSifre, out byte[] passwordHash, out byte[] passwordSalt);

			user.PasswordHash = passwordHash;
			user.PasswordSalt = passwordSalt;
			user.GeçiciŞifre = yeniSifre; // Geçici şifreyi kaydet
			user.IlkGirisYapildiMi = false; // Admin sets temporary password; user must complete first login

			await _context.SaveChangesAsync();

			return ServiceResult<bool>.Basarili(true, $"Geçici şifre güncellendi. Yeni Şifre: {yeniSifre}");
		}

		private string GenerateStrongPassword(int length = 16)
		{
			const string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
			const string lower = "abcdefghijkmnopqrstuvwxyz";
			const string digits = "0123456789";
			const string special = "!@#$%^&*()-_=+[]{};:,.<>?";
			var all = upper + lower + digits + special;
			var rng = RandomNumberGenerator.Create();
			var bytes = new byte[length];
			rng.GetBytes(bytes);
			var chars = new char[length];
			for (int i = 0; i < length; i++)
			{
				chars[i] = all[bytes[i] % all.Length];
			}
			// Ensure at least one from each category
			chars[0] = upper[bytes[0] % upper.Length];
			chars[1] = lower[bytes[1] % lower.Length];
			chars[2] = digits[bytes[2] % digits.Length];
			chars[3] = special[bytes[3] % special.Length];
			return new string(chars);
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
			user.GeçiciŞifre = null; // Geçici şifre temizle (artık kalıcı şifre belirlenmiş)
			user.IlkGirisYapildiMi = true; // Artık kurulum tamamlandı

			await _context.SaveChangesAsync();

			return ServiceResult<bool>.Basarili(true);
		}

		public async Task<ServiceResult<List<Kullanici>>> TumKullanicilariGetirAsync()
		{
			var kullanicilar = await _context.Kullanicilar
				.Include(u => u.AldigiEmanetler)
					.ThenInclude(e => e.EmanetDetaylari)
						.ThenInclude(ed => ed.Malzeme) // Malzeme isimlerini görebilmek için
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

		public async Task<ServiceResult<bool>> UpdateUserAsync(Kullanici kullanici)
		{
			try
			{
				var existingUser = await _context.Kullanicilar.FindAsync(kullanici.Id);
				if (existingUser == null)
					return ServiceResult<bool>.Basarisiz("Kullanıcı bulunamadı.");

				// Güncellenebilir alanları güncelle
				existingUser.Ad = kullanici.Ad;
				existingUser.Soyad = kullanici.Soyad;
				existingUser.Email = kullanici.Email;
				existingUser.Telefon = kullanici.Telefon;
				existingUser.OkulNo = kullanici.OkulNo;
				existingUser.Rol = kullanici.Rol;
				existingUser.IlkGirisYapildiMi = kullanici.IlkGirisYapildiMi;

				// Eğer ilk giriş yapıldıysa, geçici şifreyi temizle
				if (kullanici.IlkGirisYapildiMi)
				{
					existingUser.GeçiciŞifre = null;
				}

				_context.Kullanicilar.Update(existingUser);
				await _context.SaveChangesAsync();

				return ServiceResult<bool>.Basarili(true, "Kullanıcı başarıyla güncellendi.");
			}
			catch (Exception ex)
			{
				return ServiceResult<bool>.Basarisiz($"Güncelleme sırasında hata: {ex.Message}");
			}
		}

		public async Task<ServiceResult<bool>> DeleteUserAsync(int id)
		{
			try
			{
				var kullanici = await _context.Kullanicilar.FindAsync(id);
				if (kullanici == null)
					return ServiceResult<bool>.Basarisiz("Kullanıcı bulunamadı.");

				_context.Kullanicilar.Remove(kullanici);
				await _context.SaveChangesAsync();

				return ServiceResult<bool>.Basarili(true, "Kullanıcı başarıyla silindi.");
			}
			catch (Exception ex)
			{
				return ServiceResult<bool>.Basarisiz($"Silme sırasında hata: {ex.Message}");
			}
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
