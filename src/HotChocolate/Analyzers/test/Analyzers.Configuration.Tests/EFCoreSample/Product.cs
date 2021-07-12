using System.Collections.Generic;

namespace HotChocolate.Analyzers.Configuration.EFCoreSample
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? Sku { get; set; }
        public double Price { get; set; }
        public List<Order> Orders { get; set; } = default!;
        public bool IsFeatured { get; set; }
    }
}
