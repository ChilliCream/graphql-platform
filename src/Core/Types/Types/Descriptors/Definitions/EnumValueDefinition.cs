using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class EnumValueDefinition
        : TypeDefinitionBase<EnumValueDefinitionNode>
        , ICanBeDeprecated
    {
        public string DeprecationReason { get; set; }

        public bool IsDeprecated => !string.IsNullOrEmpty(DeprecationReason);

        public object Value { get; set; }
    }
}
