using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IEnumValueDescriptor
        : IDescriptor<EnumValueDefinition>
        , IFluent
    {
        IEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition);

        IEnumValueDescriptor Name(NameString value);

        IEnumValueDescriptor Description(string value);

        IEnumValueDescriptor DeprecationReason(string reason);
    }
}
