using System;

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Represents a type definition.
    /// </summary>
    public interface ITypeDefinition
        : IDefinition
        , IHasSyntaxNode
        , IHasRuntimeType
        , IHasDirectiveDefinition
        , IHasExtendsType
    {
        /// <summary>
        /// Gets or sets the runtime type.
        /// The runtime type defines of which value the type is when it
        /// manifests in the execution engine.
        /// </summary>
        new Type RuntimeType { get; set; }
    }
}
