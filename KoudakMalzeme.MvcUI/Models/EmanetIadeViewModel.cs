using KoudakMalzeme.Shared.Entities;

namespace KoudakMalzeme.MvcUI.Models
{
	public class EmanetIadeViewModel
	{
		public int EmanetId { get; set; }
		public Emanet Emanet { get; set; } = new();
		
		// Key: MalzemeId, Value: Adet
		public Dictionary<int, int> IadeAdetleri { get; set; } = new();
	}
}
