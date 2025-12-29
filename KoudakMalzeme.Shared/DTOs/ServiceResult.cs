namespace KoudakMalzeme.Shared.Dtos
{
	public class ServiceResult<T>
	{
		public bool BasariliMi { get; set; }
		public string? Mesaj { get; set; }
		public T? Veri { get; set; }

		public static ServiceResult<T> Basarili(T veri, string mesaj = "İşlem başarılı")
		{
			return new ServiceResult<T>
			{
				BasariliMi = true,
				Veri = veri,
				Mesaj = mesaj
			};
		}

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