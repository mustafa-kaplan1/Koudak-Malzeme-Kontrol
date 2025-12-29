namespace KoudakMalzeme.Business.Types
{
	public class ServiceResult<T>
	{
		public bool BasariliMi { get; set; }
		public string? Mesaj { get; set; }
		public T? Veri { get; set; }

		// Başarılı sonucu kolayca oluşturmak için
		public static ServiceResult<T> Basarili(T veri, string mesaj = "İşlem başarılı")
		{
			return new ServiceResult<T>
			{
				BasariliMi = true,
				Veri = veri,
				Mesaj = mesaj
			};
		}

		// Başarısız sonucu kolayca oluşturmak için
		public static ServiceResult<T> Basarisiz(string hataMesaji)
		{
			return new ServiceResult<T>
			{
				BasariliMi = false,
				Veri = default,
				Mesaj = hataMesaji
			};
		}
	}
}