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

    public interface IInterfaceTypeDescriptor<T>
        : IInterfaceTypeDescriptor
    {
        new IInterfaceTypeDescriptor<T> SyntaxNode(InterfaceTypeDefinitionNode syntaxNode);

        new IInterfaceTypeDescriptor<T> Name(NameString name);

        new IInterfaceTypeDescriptor<T> Description(string description);

        new IInterfaceTypeDescriptor<T> ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);

        new IInterfaceTypeDescriptor<T> Field(NameString name);

        new IInterfaceTypeDescriptor<T> Directive<T>(T directive)
            where T : class;

        new IInterfaceTypeDescriptor<T> Directive<T>()
            where T : class, new();

        new IInterfaceTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);

        new IInterfaceTypeDescriptor<T> Directive(
            string name,
            params ArgumentNode[] arguments);
    }
}
