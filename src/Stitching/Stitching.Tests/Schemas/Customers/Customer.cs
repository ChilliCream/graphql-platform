namespace HotChocolate.Stitching.Schemas.Customers
{
    public class Customer
        : ICustomerOrConsultant
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ConsultantId { get; set; }
    }
}
