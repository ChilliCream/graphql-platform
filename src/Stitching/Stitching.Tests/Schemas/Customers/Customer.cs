using System;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class Customer
        : ICustomerOrConsultant
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Street { get; set; }
        public string ConsultantId { get; set; }
        public int SomeInt { get; set; }
        public Guid SomeGuid { get; set; }
        public CustomerKind Kind { get; set; }
    }
}
