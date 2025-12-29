using System.Collections.Generic;

namespace KoudakMalzeme.Business.Types
{
	// Malzeme talep ederken veya verirken kullanılacak model
	public class EmanetVermeIstegi
	{
		public int UyeId { get; set; } // Kime veriyoruz?
		public int VerenPersonelId { get; set; } // Kim veriyor?
		public string? Not { get; set; }
		public List<EmanetKalem> Kalemler { get; set; } = new();
	}

	// İade alırken kullanılacak model
	public class EmanetIadeIstegi
	{
		public int EmanetId { get; set; }
		public int AlanPersonelId { get; set; } // Kim teslim alıyor?
		public List<EmanetKalem> IadeEdilenler { get; set; } = new();
	}

	public class EmanetKalem
	{
		public int MalzemeId { get; set; }
		public int Adet { get; set; }
	}
}