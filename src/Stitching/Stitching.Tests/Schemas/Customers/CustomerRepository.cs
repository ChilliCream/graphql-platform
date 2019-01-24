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
                ConsultantId = "1"
            },
            new Customer
            {
                Id = "2",
                Name = "Carol Danvers",
                ConsultantId = "1"
            },
            new Customer
            {
                Id = "3",
                Name = "Walter Lawson",
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
