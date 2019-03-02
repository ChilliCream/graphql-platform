using System.Collections.Generic;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class CustomerRepository
    {
        public List<Customer> Customers { get; } = new List<Customer>
        {
            new Customer
            {
                Id = "1",
                Name = "Freddy Freeman",
                Street = "Far far away 1",
                ConsultantId = "1"
            },
            new Customer
            {
                Id = "2",
                Name = "Carol Danvers",
                Street = "Far far away 2",
                ConsultantId = "1"
            },
            new Customer
            {
                Id = "3",
                Name = "Walter Lawson",
                Street = "Far far away 3",
                ConsultantId = "2"
            }
        };

        public List<Consultant> Consultants { get; } = new List<Consultant>
        {
            new Consultant
            {
                Id = "1",
                Name = "Jordan Belfort",
            },
            new Consultant
            {
                Id = "2",
                Name = "Gordon Gekko",
            }
        };
    }
}
