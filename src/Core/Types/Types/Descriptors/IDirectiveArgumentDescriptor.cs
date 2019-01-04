using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IDirectiveArgumentDescriptor
        : IArgumentDescriptor
    {
        IDirectiveArgumentDescriptor Name(NameString name);

        new IDirectiveArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode syntaxNode);

        new IDirectiveArgumentDescriptor Description(string description);

        new IDirectiveArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;

        new IDirectiveArgumentDescriptor Type(ITypeNode type);

        new IDirectiveArgumentDescriptor DefaultValue(IValueNode defaultValue);

        new IDirectiveArgumentDescriptor DefaultValue(object defaultValue);

        IDirectiveArgumentDescriptor Ignore();
    }
}
