using System.Collections.Generic;

namespace KoudakMalzeme.Shared.Dtos
{
	public class EmanetVermeIstegiDto
	{
		public int UyeId { get; set; }
		public int VerenPersonelId { get; set; }
		public string? Not { get; set; }
		public List<EmanetKalemDto> Kalemler { get; set; } = new();
	}

	public class EmanetIadeIstegiDto
	{
		public int EmanetId { get; set; }
		public int AlanPersonelId { get; set; }
		public List<EmanetKalemDto> IadeEdilenler { get; set; } = new();
	}

	public class EmanetKalemDto
	{
		public int MalzemeId { get; set; }
		public int Adet { get; set; }
	}

	public class EmanetTalepOlusturDto
	{
		public List<EmanetSepetItemDto> Malzemeler { get; set; }
	}

	public class EmanetSepetItemDto
	{
		public int MalzemeId { get; set; }
		public int Adet { get; set; }
	}

	public class EmanetOnayDto
	{
		public int EmanetId { get; set; }
		public int PersonelId { get; set; } // İşlemi yapan admin/malzemeci
		public string? MalzemeciNotu { get; set; }
		public DateTime? PlanlananIadeTarihi { get; set; }
	}

	public class EmanetRedDto
	{
		public int EmanetId { get; set; }
		public int PersonelId { get; set; }
		public string? RetNedeni { get; set; }
	}
}
