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
}
