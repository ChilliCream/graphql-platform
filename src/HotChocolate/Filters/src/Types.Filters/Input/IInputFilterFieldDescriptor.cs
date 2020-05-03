using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public interface IInputFilterFieldDescriptor
    {
        IInputFilterFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode);

        IInputFilterFieldDescriptor Name(NameString value);

        IInputFilterFieldDescriptor Description(string value);

        IInputFilterFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IInputFilterFieldDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        IInputFilterFieldDescriptor Type(ITypeNode typeNode);

        IInputFilterFieldDescriptor Type(Type type);

        IInputFilterFieldDescriptor Ignore(bool ignore = true);

        IInputFilterFieldDescriptor DefaultValue(IValueNode value);

        IInputFilterFieldDescriptor DefaultValue(object value);

        IInputFilterFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IInputFilterFieldDescriptor Directive<T>()
            where T : class, new();

        IInputFilterFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}