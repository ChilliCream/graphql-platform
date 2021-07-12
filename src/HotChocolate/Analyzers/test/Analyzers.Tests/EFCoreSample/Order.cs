using System;
using System.Collections.Generic;

namespace HotChocolate.Analyzers.EFCoreSample
{
    public class Order
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;
        public List<Product> Products { get; set; } = default!;
        public DateTime OrderDate { get; set; }
        public double TotalValue { get; set; }
    }
}
