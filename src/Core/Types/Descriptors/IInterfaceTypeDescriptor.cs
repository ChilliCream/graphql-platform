using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor
        : IFluent
    {
        IInterfaceTypeDescriptor SyntaxNode(InterfaceTypeDefinitionNode syntaxNode);

        IInterfaceTypeDescriptor Name(string name);

        IInterfaceTypeDescriptor Description(string description);

        IInterfaceTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);

        IInterfaceFieldDescriptor Field(string name);
    }
}
