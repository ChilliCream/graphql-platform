namespace HotChocolate.Validation
{
    public class ComplexInput2
    {
        public string Name { get; set; }

        public string Owner { get; set; }

        public ComplexInput2 Child { get; set; }
    }
}
