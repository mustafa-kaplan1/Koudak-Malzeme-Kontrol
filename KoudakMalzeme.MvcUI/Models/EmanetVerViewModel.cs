using KoudakMalzeme.Shared.Entities;

namespace KoudakMalzeme.MvcUI.Models
{
	public class EmanetVerViewModel
	{
		public List<Kullanici> Uyeler { get; set; } = new();
		public List<Malzeme> Malzemeler { get; set; } = new();
	}
}