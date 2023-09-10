using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a directive that defines semantic equivalence between two
/// fields or a field and an argument.
/// </summary>
internal sealed class IsDirective
{
    /// <summary>
    /// Creates a new instance of <see cref="IsDirective"/> that
    /// uses a schema coordinate to refer to a field or argument.
    /// </summary>
    /// <param name="coordinate">
    /// A schema coordinate that refers to another field or argument.
    /// </param>
    public IsDirective(SchemaCoordinate coordinate)
    {
        Coordinate = coordinate;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IsDirective"/> that a field syntax to refer to field.
    /// </summary>
    /// <param name="field">
    /// The field selection syntax that refers to another field.
    /// </param>
    public IsDirective(FieldNode field)
    {
        Field = field;
    }

    /// <summary>
    /// Returns <c>true</c> if this directive refers to a schema coordinate.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Coordinate))]
    [MemberNotNullWhen(false, nameof(Field))]
    public bool IsCoordinate => Field is null;

    /// <summary>
    /// A schema coordinate that refers to another field or argument.
    /// </summary>
    public SchemaCoordinate? Coordinate { get; }

    /// <summary>
    /// If used on an argument this field selection syntax refers to a
    /// field of the return type of the declaring field.
    /// </summary>
    public FieldNode? Field { get; }
}