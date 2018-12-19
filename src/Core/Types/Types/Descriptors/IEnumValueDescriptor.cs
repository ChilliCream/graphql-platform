using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IEnumValueDescriptor
        : IFluent
    {
        IEnumValueDescriptor SyntaxNode(EnumValueDefinitionNode syntaxNode);

        IEnumValueDescriptor Name(NameString name);

        IEnumValueDescriptor Description(string description);

        IEnumValueDescriptor DeprecationReason(string deprecationReason);
    }
}
