#nullable disable

namespace HotChocolate.Utilities.Introspection
{
    internal class InputField
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TypeRef Type { get; set; }
        public string DefaultValue { get; set; }
    }
}
