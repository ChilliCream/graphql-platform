using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IArgumentDescriptor
    {
        IArgumentDescriptor SyntaxNode(InputValueDefinitionNode syntaxNode);

        IArgumentDescriptor Description(string description);

        IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IArgumentDescriptor Type(ITypeNode type);

        IArgumentDescriptor DefaultValue(IValueNode defaultValue);

        IArgumentDescriptor DefaultValue(object defaultValue);
    }

    public interface IDirectiveArgumentDescriptor
        : IArgumentDescriptor
    {
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
