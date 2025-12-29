using System;

namespace KoudakMalzeme.Shared.Dtos
{
	public class UserLoginDto
	{
		public string Email { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class UserRegisterDto
	{
		public string OkulNo { get; set; } = string.Empty;
		public string Ad { get; set; } = string.Empty;
		public string Soyad { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Telefon { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class AuthResponseDto
	{
		public string Token { get; set; } = string.Empty;
		public string AdSoyad { get; set; } = string.Empty;
		public string Rol { get; set; } = string.Empty;
		public bool IlkGirisYapildiMi { get; set; }
		public DateTime Expiration { get; set; }
	}

	public class AdminUyeEkleDto
	{
		public string OkulNo { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? Ad { get; set; }
		public string? Soyad { get; set; }
	}

	public class IlkGirisGuncellemeDto
	{
		public int KullaniciId { get; set; }
		public string Ad { get; set; } = string.Empty;
		public string Soyad { get; set; } = string.Empty;
		public string Telefon { get; set; } = string.Empty;
		public string YeniSifre { get; set; } = string.Empty;
		public string YeniSifreTekrar { get; set; } = string.Empty;
	}
}
