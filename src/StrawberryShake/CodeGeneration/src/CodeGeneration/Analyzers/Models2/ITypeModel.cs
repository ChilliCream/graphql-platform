using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models2
{
    /// <summary>
    /// Represents a model that is bound to a specific GraphQL schema type.
    /// </summary>
    public interface ITypeModel
    {
        /// <summary>
        /// Gets the GraphQL schema type.
        /// </summary>
        INamedType Type { get; }
    }
}
