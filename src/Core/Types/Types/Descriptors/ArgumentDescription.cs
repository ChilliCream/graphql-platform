using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ArgumentDescription
        : FieldDescriptionBase
    {
        public InputValueDefinitionNode SyntaxNode { get; set; }

        public IValueNode DefaultValue { get; set; }

        public object NativeDefaultValue { get; set; }
    }
}
