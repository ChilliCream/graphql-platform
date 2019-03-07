using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInterfaceFieldDescriptor
        : IFluent
    {
        IInterfaceFieldDescriptor SyntaxNode(
            FieldDefinitionNode fieldDefinitionNode);

        IInterfaceFieldDescriptor Name(NameString value);

        IInterfaceFieldDescriptor Description(string value);

        IInterfaceFieldDescriptor DeprecationReason(string reason);

        IInterfaceFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;

        IInterfaceFieldDescriptor Type<TOutputType>(TOutputType type)
            where TOutputType : class, IOutputType;

        IInterfaceFieldDescriptor Type(ITypeNode type);

        IInterfaceFieldDescriptor Ignore();

        IInterfaceFieldDescriptor Argument(
            NameString name,
            Action<IArgumentDescriptor> argument);

        IInterfaceFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IInterfaceFieldDescriptor Directive<T>()
            where T : class, new();

        IInterfaceFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
