using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class EnumValueDescription
        : TypeDescriptionBase
    {
        public EnumValueDefinitionNode SyntaxNode { get; set; }

        public string DeprecationReason { get; set; }

        public object Value { get; set; }
    }
}
