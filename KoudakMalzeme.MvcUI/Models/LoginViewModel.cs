using System.ComponentModel.DataAnnotations;

namespace KoudakMalzeme.MvcUI.Models
{
	public class LoginViewModel
	{
		[Required(ErrorMessage = "E-posta adresi gereklidir.")]
		[EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Şifre gereklidir.")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = string.Empty;
	}
}