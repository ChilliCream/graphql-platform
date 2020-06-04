using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface ICustomFilterFieldDescriptor
    {
        ICustomFilterFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode);

        ICustomFilterFieldDescriptor Name(NameString value);

        ICustomFilterFieldDescriptor Description(string value);

        ICustomFilterFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;

        ICustomFilterFieldDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        ICustomFilterFieldDescriptor Type(ITypeNode typeNode);

        ICustomFilterFieldDescriptor Type(Type type);

        ICustomFilterFieldDescriptor Ignore(bool ignore = true);

        ICustomFilterFieldDescriptor DefaultValue(IValueNode value);

        ICustomFilterFieldDescriptor DefaultValue(object value);

        ICustomFilterFieldDescriptor OperationKind(int kind);

        ICustomFilterFieldDescriptor Kind(int kind);

        ICustomFilterFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        ICustomFilterFieldDescriptor Directive<T>()
            where T : class, new();

        ICustomFilterFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
