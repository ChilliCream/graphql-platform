using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInputFieldDescriptor
        : IFluent
    {
        IInputFieldDescriptor SyntaxNode(InputValueDefinitionNode syntaxNode);

        IInputFieldDescriptor Name(string name);

        IInputFieldDescriptor Description(string description);

        IInputFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IInputFieldDescriptor Type(ITypeNode type);

        IInputFieldDescriptor DefaultValue(IValueNode defaultValue);

        IInputFieldDescriptor DefaultValue(object defaultValue);
    }
}
