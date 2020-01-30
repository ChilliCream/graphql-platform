using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInputFieldDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        IInputFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode);

        IInputFieldDescriptor Name(NameString value);

        IInputFieldDescriptor Description(string value);

        IInputFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IInputFieldDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        IInputFieldDescriptor Type(ITypeNode typeNode);

        IInputFieldDescriptor Type(Type type);

        IInputFieldDescriptor Ignore(bool ignore = true);

        IInputFieldDescriptor DefaultValue(IValueNode value);

        IInputFieldDescriptor DefaultValue(object value);

        IInputFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IInputFieldDescriptor Directive<T>()
            where T : class, new();

        IInputFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
