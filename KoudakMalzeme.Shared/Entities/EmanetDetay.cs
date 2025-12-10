namespace KoudakMalzeme.Shared.Entities
{
	public class EmanetDetay : BaseEntity
	{
		public int EmanetId { get; set; }
		public Emanet Emanet { get; set; }

		public int MalzemeId { get; set; }
		public Malzeme Malzeme { get; set; }

		// Kaç tane aldı? (Örn: 5 Karabina)
		public int AlinanAdet { get; set; }

		// Kaç tanesini geri getirdi? (Örn: Bugün 3 tanesini getirdi, 2'si kaldı)
		public int IadeEdilenAdet { get; set; } = 0;

		// Hesaplanan özellik: Geriye kaç tane kaldı?
		public int KalanAdet => AlinanAdet - IadeEdilenAdet;

		// Bu kalemin durumu (Hepsi döndü mü?)
		public bool TamamlandiMi => IadeEdilenAdet >= AlinanAdet;
	}
}