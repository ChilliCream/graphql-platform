using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ArgumentDescription
    {
        public InputValueDefinitionNode SyntaxNode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TypeReference TypeReference { get; set; }
        public IValueNode DefaultValue { get; set; }
        public object NativeDefaultValue { get; set; }
    }
}
