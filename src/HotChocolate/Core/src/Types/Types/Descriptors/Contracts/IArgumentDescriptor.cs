using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    /// <summary>
    /// The argument descriptor configures output field arguments.
    /// </summary>
    public interface IArgumentDescriptor
        : IInputMemberDescriptor<IArgumentDescriptor, ArgumentDefinition>
        , IDirectiveDescriptor<IArgumentDescriptor>
        , IDefaultValueDescriptor<IArgumentDescriptor>
    {
        /// <summary>
        /// Associates the argument with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="inputValueDefinition">
        /// The the type definition node.
        /// </param>
        IArgumentDescriptor SyntaxNode(InputValueDefinitionNode inputValueDefinition);
    }
}
