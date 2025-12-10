using System;

namespace KoudakMalzeme.Shared.Entities
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}