namespace KoudakMalzeme.Business.Types
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

	// Token bilgisini dönmek için
	public class AuthResponseDto
	{
		public string Token { get; set; } = string.Empty;
		public string AdSoyad { get; set; } = string.Empty;
		public string Rol { get; set; } = string.Empty;
		public DateTime Expiration { get; set; }
	}
}
