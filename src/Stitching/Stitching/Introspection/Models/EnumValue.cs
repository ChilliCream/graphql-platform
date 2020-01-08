namespace HotChocolate.Stitching.Introspection.Models
{
    internal class EnumValue
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDeprecated { get; set; }
        public string DeprecationReason { get; set; }
    }
}
