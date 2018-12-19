using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor
        : IFluent
    {
        IInterfaceTypeDescriptor SyntaxNode(InterfaceTypeDefinitionNode syntaxNode);

        IInterfaceTypeDescriptor Name(NameString name);

        IInterfaceTypeDescriptor Description(string description);

        IInterfaceTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);

        IInterfaceFieldDescriptor Field(NameString name);

        IInterfaceTypeDescriptor Directive<T>(T directive)
            where T : class;

        IInterfaceTypeDescriptor Directive<T>()
            where T : class, new();

        IInterfaceTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);

        IInterfaceTypeDescriptor Directive(
            string name,
            params ArgumentNode[] arguments);
    }
}
