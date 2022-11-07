namespace HotChocolate.Stitching.Schemas.Customers
{
    public class Consultant
        : ICustomerOrConsultant
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
