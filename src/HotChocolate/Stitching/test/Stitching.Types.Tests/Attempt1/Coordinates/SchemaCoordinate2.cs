using System;
using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Coordinates;

[DebuggerDisplay(@"SchemaCoordinate2: {SchemaCoordinatePrinter.Print(this)}")]
internal readonly struct SchemaCoordinate2 : ISchemaCoordinate2
{
    public ISchemaCoordinate2? Parent { get; }
    public SyntaxKind Kind { get; }
    public NameNode? Name { get; }

    internal SchemaCoordinate2(
        SyntaxKind kind,
        NameNode? nodeName = default)
        : this(default, kind, nodeName)
    {
    }

    internal SchemaCoordinate2(
        ISchemaCoordinate2? parent,
        SyntaxKind kind,
        NameNode? nodeName = default)
    {
        Parent = parent;
        Kind = kind;
        Name = nodeName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Kind, Name, Parent);
    }

    public bool Equals(SchemaCoordinate2 other)
    {
        return Kind.IsEqualTo(other.Kind)
               && Name.IsEqualTo(other.Name)
               && Parent.IsEqualTo(other.Parent);
    }

    public override bool Equals(object? obj)
    {
        return obj is SchemaCoordinate2 other
               && Equals(other);
    }

    public static bool operator ==(SchemaCoordinate2 left, SchemaCoordinate2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SchemaCoordinate2 left, SchemaCoordinate2 right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return Name?.Value ?? $"{GetHashCode()}";
    }
}
