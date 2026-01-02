using System.Collections.Generic;

namespace KoudakMalzeme.Shared.Entities
{
	public class Malzeme : BaseEntity
	{
		public string Ad { get; set; }
		public string? Aciklama { get; set; }
		public string? GorselYolu { get; set; }
		public string? Etiketler { get; set; }

		public int ToplamStok { get; set; }
		public int GuncelStok { get; set; }

		public ICollection<EmanetDetay>? EmanetGecmisi { get; set; }
	}
}