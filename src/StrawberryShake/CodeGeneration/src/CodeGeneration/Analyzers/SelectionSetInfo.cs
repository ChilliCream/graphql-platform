using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

public readonly struct SelectionSetInfo(ITypeDefinition type, SelectionSetNode selectionSet)
    : IEquatable<SelectionSetInfo>
{
    public ITypeDefinition Type { get; } = type;

    public SelectionSetNode SelectionSet { get; } = selectionSet;

    public bool Equals(SelectionSetInfo other) =>
        Type.Equals(other.Type)
        && SelectionSet.Equals(other.SelectionSet);

    public override bool Equals(object? obj) =>
        obj is SelectionSetInfo other
        && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return Type.GetHashCode() * 397
                ^ SelectionSet.GetHashCode() * 397;
        }
    }
}
