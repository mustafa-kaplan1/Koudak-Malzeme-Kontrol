using System.Collections.Generic;

namespace KoudakMalzeme.Shared.Entities
{
	public class Malzeme : BaseEntity
	{
		public string Ad { get; set; }
		public string? Aciklama { get; set; }
		public string? GorselYolu { get; set; }

		// Kulübün toplam sahip olduğu miktar (Örn: 20 tane var)
		public int ToplamStok { get; set; }

		// Şu an odada rafta duran miktar (Örn: 5 tane emanetteyse burası 15 olur)
		public int GuncelStok { get; set; }

		public ICollection<EmanetDetay>? EmanetGecmisi { get; set; }
	}
}