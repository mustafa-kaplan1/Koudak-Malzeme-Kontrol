using KoudakMalzeme.Shared.Enums;
using System;
using System.Collections.Generic;

namespace KoudakMalzeme.Shared.Entities
{
	public class Emanet : BaseEntity
	{
		// Malzemeyi alan üye
		public int UyeId { get; set; }
		public Kullanici Uye { get; set; }

		// Malzemeyi depodan çıkarıp teslim eden yetkili (Malzemeci/Admin)
		public int? VerenPersonelId { get; set; }
		public Kullanici? VerenPersonel { get; set; }

		// Malzemeyi geri teslim alan yetkili (Bu farklı biri olabilir)
		public int? AlanPersonelId { get; set; }
		public Kullanici? AlanPersonel { get; set; }

		public DateTime TeslimAlmaTarihi { get; set; } = DateTime.Now;
		public DateTime? IadeTarihi { get; set; }       // İşlemin tamamen kapandığı tarih
		public DateTime? PlanlananIadeTarihi { get; set; } // Ne zaman getirecek?

		public string? MalzemeciNotu { get; set; }
		public EmanetDurumu Durum { get; set; } = EmanetDurumu.TalepEdildi;

		// Bu emanet işleminde hangi malzemelerden kaçar tane var?
		public ICollection<EmanetDetay> EmanetDetaylari { get; set; }
	}
}