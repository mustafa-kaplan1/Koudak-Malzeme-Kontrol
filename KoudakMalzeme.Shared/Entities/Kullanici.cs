using KoudakMalzeme.Shared.Enums;
using System.Collections.Generic;

namespace KoudakMalzeme.Shared.Entities
{
	public class Kullanici : BaseEntity
	{
		public string OkulNo { get; set; }
		public string Ad { get; set; }
		public string Soyad { get; set; }
		public string Email { get; set; }
		public string Telefon { get; set; }
		public string? ProfilResmiYolu { get; set; }

		public byte[] PasswordHash { get; set; }
		public byte[] PasswordSalt { get; set; }
		public KullaniciRolu Rol { get; set; }
		public ICollection<Emanet> AldigiEmanetler { get; set; }
		public bool IlkGirisYapildiMi { get; set; } = false;
	}
}