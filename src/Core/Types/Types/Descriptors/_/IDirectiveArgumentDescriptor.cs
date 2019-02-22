using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IDirectiveArgumentDescriptor
        : IArgumentDescriptor
    {
        new IDirectiveArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition);

        IDirectiveArgumentDescriptor Name(
            NameString value);

        new IDirectiveArgumentDescriptor Description(
            string value);

        new IDirectiveArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;

        new IDirectiveArgumentDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType;

        new IDirectiveArgumentDescriptor Type(
            ITypeNode typeNode);

        new IDirectiveArgumentDescriptor DefaultValue(
            IValueNode value);

        new IDirectiveArgumentDescriptor DefaultValue(
            object value);

        IDirectiveArgumentDescriptor Ignore();
    }
}
