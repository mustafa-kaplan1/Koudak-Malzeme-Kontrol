using System.ComponentModel.DataAnnotations;

namespace KoudakMalzeme.MvcUI.Models
{
	public class IlkGirisViewModel
	{
		// Gizli olarak ID'yi tutacağız
		public int KullaniciId { get; set; }

		[Required(ErrorMessage = "Ad alanı zorunludur.")]
		public string Ad { get; set; } = string.Empty;

		[Required(ErrorMessage = "Soyad alanı zorunludur.")]
		public string Soyad { get; set; } = string.Empty;

		[Required(ErrorMessage = "Telefon zorunludur.")]
		[Phone(ErrorMessage = "Geçerli bir telefon giriniz.")]
		public string Telefon { get; set; } = string.Empty;

		[Required(ErrorMessage = "Yeni şifre zorunludur.")]
		[MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
		[DataType(DataType.Password)]
		public string YeniSifre { get; set; } = string.Empty;

		[Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
		[Compare("YeniSifre", ErrorMessage = "Şifreler uyuşmuyor.")]
		[DataType(DataType.Password)]
		public string YeniSifreTekrar { get; set; } = string.Empty;
	}
}
