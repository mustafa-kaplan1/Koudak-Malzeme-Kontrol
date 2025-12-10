namespace KoudakMalzeme.Shared.Enums
{
	public enum EmanetDurumu
	{
		TalepEdildi = 0,      // Kullanıcı istedi, onay bekliyor
		TeslimEdildi = 1,     // Malzeme kullanıcıya verildi
		KismenIadeEdildi = 2, // Bir kısmı getirildi, bir kısmı hala kullanıcıda
		Tamamlandi = 3,       // Her şey geri alındı, işlem kapandı
		IptalEdildi = 4,      // Talep reddedildi veya iptal oldu
		Gecikmede = 5         // Süresi doldu getirilmedi
	}
}