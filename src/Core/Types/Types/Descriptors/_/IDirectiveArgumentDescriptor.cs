using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IDirectiveArgumentDescriptor
    {
        IDirectiveArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition);

        IDirectiveArgumentDescriptor Name(NameString value);

        IDirectiveArgumentDescriptor Description(string value);

        IDirectiveArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IDirectiveArgumentDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        IDirectiveArgumentDescriptor Type(ITypeNode typeNode);

        IDirectiveArgumentDescriptor DefaultValue(IValueNode value);

        IDirectiveArgumentDescriptor DefaultValue(object value);

        IDirectiveArgumentDescriptor Ignore();
    }
}
