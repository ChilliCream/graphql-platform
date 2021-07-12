using System.Collections.Generic;

namespace HotChocolate.Analyzers.Configuration.EFCoreSample
{
    public class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public User? User { get; set; }
        public List<Address> ShippingAddresses { get; set; } = default!;
        public List<Order> Orders { get; set; } = default!;
    }
}
