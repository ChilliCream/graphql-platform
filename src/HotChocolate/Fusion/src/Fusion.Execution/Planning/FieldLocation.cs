using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Identifies a leaf field by the path to its parent object plus its
/// response name. Used as a dictionary key when partitioning fields by
/// effective <see cref="DeferUsageSetKey"/>. Equality is sequence-based on
/// <see cref="Path"/>, which is why this type overrides the record-struct
/// default (the default would compare the underlying ImmutableArray by
/// reference, not by contents).
/// </summary>
internal readonly record struct FieldLocation(
    ImmutableArray<FieldPathSegment> Path,
    string ResponseName)
{
    public bool Equals(FieldLocation other)
    {
        if (!string.Equals(ResponseName, other.ResponseName, StringComparison.Ordinal))
        {
            return false;
        }

        if (Path.Length != other.Path.Length)
        {
            return false;
        }

        for (var i = 0; i < Path.Length; i++)
        {
            if (!Path[i].Equals(other.Path[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = StringComparer.Ordinal.GetHashCode(ResponseName);

        for (var i = 0; i < Path.Length; i++)
        {
            hash = HashCode.Combine(hash, Path[i]);
        }

        return hash;
    }
}
