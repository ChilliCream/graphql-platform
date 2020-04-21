namespace HotChocolate.Stitching.Schemas.Customers
{
    public class ComplexInput
    {
        public string Value { get; set; }

        public ComplexInput Deeper { get; set; }

        public string[] ValueArray { get; set; }

        public ComplexInput[] DeeperArray { get; set; }
    }
}
