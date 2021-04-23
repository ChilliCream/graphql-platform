using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL object type
    /// </summary>
    public interface IObjectType : IComplexOutputType
    {
        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        new ObjectTypeDefinitionNode? SyntaxNode { get; }

        /// <summary>
        /// Gets the field that the type exposes.
        /// </summary>
        new IFieldCollection<IObjectField> Fields { get; }
    }
}
