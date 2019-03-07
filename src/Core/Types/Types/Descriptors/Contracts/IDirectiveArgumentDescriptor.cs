using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IDirectiveArgumentDescriptor
        : IFluent
    {
        /// <summary>
        /// Associates the argument with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="inputValueDefinition">
        /// The the type definition node.
        /// </param>
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
