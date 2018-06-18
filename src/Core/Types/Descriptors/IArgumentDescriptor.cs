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
}
