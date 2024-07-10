#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a field or argument of input- or output-types.
/// </summary>
public interface IField
    : IHasName
    , IHasDescription
    , IHasSchemaCoordinate
    , IHasDirectives
    , IHasRuntimeType
    , IHasReadOnlyContextData
    , ITypeSystemMember
{
    /// <summary>
    /// Gets the type of which declares this field.
    /// </summary>
    ITypeSystemObject DeclaringType { get; }

    /// <summary>
    /// The index of this field in the declaring type system member`s field collection.
    /// </summary>
    int Index { get; }
}
