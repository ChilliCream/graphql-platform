using System;

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Represents a type definition.
    /// </summary>
    public interface ITypeDefinition
        : IHasSyntaxNode
        , IHasRuntimeType
        , IHasDirectiveDefinition
        , IHasExtendsType
    {
        /// <summary>
        /// Gets or sets the name the type shall have.
        /// </summary>
        NameString Name { get; set; }

        /// <summary>
        /// Gets or sets the runtime type.
        /// The runtime type defines of which value the type is when it
        /// manifests in the execution engine.
        /// </summary>
        new Type RuntimeType { get; set; }
    }
}
