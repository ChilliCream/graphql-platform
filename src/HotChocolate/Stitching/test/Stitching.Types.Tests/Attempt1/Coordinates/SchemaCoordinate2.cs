using System;
using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Coordinates;

[DebuggerDisplay(@"SchemaCoordinate2: {SchemaCoordinatePrinter.Print(this)}")]
internal readonly struct SchemaCoordinate2 : ISchemaCoordinate2
{
    public ISchemaCoordinate2? Parent { get; }
    public SyntaxKind Kind { get; }
    public bool IsRoot => Kind == SyntaxKind.Document;
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
        NameNode? name = default)
    {
        if (parent is not null && name is null)
        {
            throw new ArgumentNullException(nameof(name), @"Name must be provided when Parent is not null");
        }

        Parent = parent;
        Kind = kind;
        Name = name;
    }

    public override int GetHashCode()
    {
        return IsRoot
            ? HashCode.Combine(IsRoot)
            : HashCode.Combine(Kind, Name, Parent);
    }

    public bool Equals(SchemaCoordinate2 other)
    {
        return IsRoot.IsEqualTo(other.IsRoot)
               || Kind.IsEqualTo(other.Kind)
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
