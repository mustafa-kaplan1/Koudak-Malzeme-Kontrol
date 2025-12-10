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

		// JWT ve Güvenlik
		public byte[] PasswordHash { get; set; }
		public byte[] PasswordSalt { get; set; }
		public KullaniciRolu Rol { get; set; }

		// Navigation Properties (İlişkiler)
		// Kullanıcının aldığı emanetler
		public ICollection<Emanet> AldigiEmanetler { get; set; }
	}
}