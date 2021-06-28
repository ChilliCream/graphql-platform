#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a field or argument of input- or output-types.
    /// </summary>
    public interface IField
        : IHasName
        , IHasDescription
        , IHasDirectives
        , IHasSyntaxNode
        , IHasRuntimeType
        , IHasReadOnlyContextData
    {
        /// <summary>
        /// Gets the type of which declares this field.
        /// </summary>
        ITypeSystemObject DeclaringType { get; }

        /// <summary>
        /// Field coordinate help with pointing to a field or argument in the schema.
        /// </summary>
        FieldCoordinate Coordinate { get; }
    }
}
