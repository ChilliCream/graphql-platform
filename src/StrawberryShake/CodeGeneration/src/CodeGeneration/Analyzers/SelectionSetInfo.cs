using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

public readonly struct SelectionSetInfo : IEquatable<SelectionSetInfo>
{
    public SelectionSetInfo(INamedType type, SelectionSetNode selectionSet)
    {
        Type = type;
        SelectionSet = selectionSet;
    }

    public INamedType Type { get; }

    public SelectionSetNode SelectionSet { get; }

    public bool Equals(SelectionSetInfo other) =>
        Type.Equals(other.Type) &&
        SelectionSet.Equals(other.SelectionSet);

    public override bool Equals(object? obj) =>
        obj is SelectionSetInfo other &&
        Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return Type.GetHashCode() * 397 ^
                   SelectionSet.GetHashCode() * 397;
        }
    }
}
