using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputFieldDescriptor
        : IFluent
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

        IInputFieldDescriptor Ignore();

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
