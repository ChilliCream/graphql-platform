using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IEnumValueDescriptor
        : IFluent
    {
        IEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition);

        IEnumValueDescriptor Name(NameString value);

        IEnumValueDescriptor Description(string value);

        IEnumValueDescriptor DeprecationReason(string value);
    }
}
