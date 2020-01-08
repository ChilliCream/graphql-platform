#nullable disable

namespace HotChocolate.Utilities.Introspection
{
    internal class EnumValue
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDeprecated { get; set; }
        public string DeprecationReason { get; set; }
    }
}
