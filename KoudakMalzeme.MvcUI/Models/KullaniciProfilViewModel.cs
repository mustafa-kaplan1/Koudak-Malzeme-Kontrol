using KoudakMalzeme.Shared.Entities;

namespace KoudakMalzeme.MvcUI.Models
{
	public class KullaniciProfilViewModel
	{
		public Kullanici Kullanici { get; set; } = new();
		public List<Emanet> Emanetler { get; set; } = new();
		public bool KendiProfiliMi { get; set; }
		public List<AktifZimmetOzetViewModel> AktifZimmetListesi { get; set; } = new();
	}

	public class AktifZimmetOzetViewModel
	{
		public string MalzemeAdi { get; set; }
		public int Adet { get; set; }
		public DateTime VerilisTarihi { get; set; }
		public int EmanetId { get; set; }
	}
}