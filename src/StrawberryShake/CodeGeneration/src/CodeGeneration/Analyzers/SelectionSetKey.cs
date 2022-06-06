using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

public readonly struct SelectionSetKey : IEquatable<SelectionSetKey>
{
    public SelectionSetKey(INamedType type, SelectionSetNode selectionSet)
    {
        Type = type;
        SelectionSet = selectionSet;
    }

    public INamedType Type { get; }

    public SelectionSetNode SelectionSet { get; }

    public bool Equals(SelectionSetKey other)
        => Type.Equals(other.Type) && SelectionSet.Equals(other.SelectionSet);

    public override bool Equals(object? obj)
        => obj is SelectionSetKey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Type, SelectionSet);

    public static bool operator ==(SelectionSetKey left, SelectionSetKey right)
        => left.Equals(right);

    public static bool operator !=(SelectionSetKey left, SelectionSetKey right)
        => !(left == right);
}