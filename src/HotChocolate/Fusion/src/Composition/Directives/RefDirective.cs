using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

public sealed class RefDirective
{
    public RefDirective(SchemaCoordinate coordinate)
    {
        Coordinate = coordinate;
    }

    public RefDirective(FieldNode field)
    {
        Field = field;
    }

    [MemberNotNullWhen(true, nameof(Coordinate))]
    [MemberNotNullWhen(false, nameof(Field))]
    public bool IsCoordinate => Field is null;

    public SchemaCoordinate? Coordinate { get; }

    public FieldNode? Field { get; }
}
