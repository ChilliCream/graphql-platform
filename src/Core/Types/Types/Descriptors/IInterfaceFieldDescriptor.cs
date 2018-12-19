using System;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInterfaceFieldDescriptor
        : IFluent
    {
        IInterfaceFieldDescriptor SyntaxNode(FieldDefinitionNode syntaxNode);

        IInterfaceFieldDescriptor Name(NameString name);

        IInterfaceFieldDescriptor Description(string description);

        IInterfaceFieldDescriptor DeprecationReason(string deprecationReason);

        IInterfaceFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;

        IInterfaceFieldDescriptor Type(ITypeNode type);

        IInterfaceFieldDescriptor Argument(
            NameString name,
            Action<IArgumentDescriptor> argument);

        IInterfaceFieldDescriptor Directive<T>(T directive)
            where T : class;

        IInterfaceFieldDescriptor Directive<T>()
            where T : class, new();

        IInterfaceFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);

        IInterfaceFieldDescriptor Directive(
            string name,
            params ArgumentNode[] arguments);
    }
}
