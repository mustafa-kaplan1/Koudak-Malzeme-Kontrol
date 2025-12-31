namespace KoudakMalzeme.Shared.Enums
{
	public enum EmanetDurumu
	{
		TalepEdildi = 1,
		TeslimEdildi = 2,    // Zimmetlendi / Aktif
		Tamamlandi = 3,      // İade Alındı / Kapandı
		IptalEdildi = 4,     // İşlem İptal (Talep aşamasında)
		Reddedildi = 5,      // Talep Reddedildi
		IadeTalepEdildi = 6, // Kullanıcı iade etmek istiyor
		KismenIadeEdildi = 7, // Bir kısmı iade edildi, kalanı duruyor
	}
}