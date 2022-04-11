using System;
using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

[DebuggerDisplay(@"SchemaCoordinate2: {SchemaCoordinatePrinter.Print(this)}")]
public readonly struct SchemaCoordinate2 : ISchemaCoordinate2
{
    public ISchemaCoordinate2? Parent { get; }
    public NameNode? Name { get; }

    internal SchemaCoordinate2(SchemaCoordinate2 parent, NameNode? nodeName = default)
    {
        Parent = parent;
        Name = nodeName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Parent, Name);
    }

    public bool Equals(SchemaCoordinate2 other)
    {
        return Equals(Parent, other.Parent)
               && Equals(Name, other.Name);
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
}