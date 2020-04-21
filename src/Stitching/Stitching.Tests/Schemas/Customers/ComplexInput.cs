namespace HotChocolate.Stitching.Schemas.Customers
{
    public class ComplexInput
    {
        public string Value { get; set; }
        public ComplexInput Deeper { get; set; }
    }
}
