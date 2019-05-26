using System;
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
                ConsultantId = "1",
                SomeInt = 1,
                SomeGuid = new Guid("01e2f5dc-0f19-4305-99d3-3c5c234a6524"),
                Kind = CustomerKind.Premium
            },
            new Customer
            {
                Id = "2",
                Name = "Carol Danvers",
                Street = "Far far away 2",
                ConsultantId = "1",
                SomeInt = 2,
                SomeGuid = new Guid("7f84a645-3439-4a6c-91b1-d313f699648d"),
                Kind = CustomerKind.Standard
            },
            new Customer
            {
                Id = "3",
                Name = "Walter Lawson",
                Street = "Far far away 3",
                ConsultantId = "2",
                SomeInt = 3,
                SomeGuid = new Guid("c1c4ec83-a0db-4020-ad0c-9ec6e09ad949")
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
