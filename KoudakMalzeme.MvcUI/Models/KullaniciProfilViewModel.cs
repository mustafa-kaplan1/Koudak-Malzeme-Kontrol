using KoudakMalzeme.Shared.Entities;

namespace KoudakMalzeme.MvcUI.Models
{
	public class KullaniciProfilViewModel
	{
		public Kullanici Kullanici { get; set; } = new();
		public List<Emanet> Emanetler { get; set; } = new();
		public bool KendiProfiliMi { get; set; }
	}
}