using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IArgumentDescriptor
    {
        IArgumentDescriptor SyntaxNode(InputValueDefinitionNode syntaxNode);

        IArgumentDescriptor Description(string description);

        IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IArgumentDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        IArgumentDescriptor Type(ITypeNode type);

        IArgumentDescriptor DefaultValue(IValueNode defaultValue);

        IArgumentDescriptor DefaultValue(object defaultValue);

        IArgumentDescriptor Directive<T>(T directive)
            where T : class;

        IArgumentDescriptor Directive<T>()
            where T : class, new();

        IArgumentDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
