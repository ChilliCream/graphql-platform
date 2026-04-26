using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A canonical key for a <see cref="DeliveryGroup"/> set. The wrapped array is
/// sorted ascending by <see cref="DeliveryGroup.Id"/> so sequence equality
/// doubles as set equality.
/// </summary>
internal readonly record struct DeliveryGroupSetKey(ImmutableArray<DeliveryGroup> Items)
    : IEquatable<DeliveryGroupSetKey>
    , IComparable<DeliveryGroupSetKey>
{
    public static DeliveryGroupSetKey Empty { get; } = new([]);

    public bool IsEmpty => Items.IsDefaultOrEmpty;

    public bool Equals(DeliveryGroupSetKey other)
    {
        if (Items.IsDefaultOrEmpty)
        {
            return other.Items.IsDefaultOrEmpty;
        }

        if (other.Items.IsDefaultOrEmpty)
        {
            return false;
        }

        if (Items.Length != other.Items.Length)
        {
            return false;
        }

        for (var i = 0; i < Items.Length; i++)
        {
            if (!ReferenceEquals(Items[i], other.Items[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        if (Items.IsDefaultOrEmpty)
        {
            return 0;
        }

        var hash = new HashCode();
        for (var i = 0; i < Items.Length; i++)
        {
            hash.Add(Items[i].Id);
        }

        return hash.ToHashCode();
    }

    public int CompareTo(DeliveryGroupSetKey other)
    {
        if (Items.IsDefaultOrEmpty)
        {
            return other.Items.IsDefaultOrEmpty ? 0 : -1;
        }

        if (other.Items.IsDefaultOrEmpty)
        {
            return 1;
        }

        var commonLength = Math.Min(Items.Length, other.Items.Length);
        for (var i = 0; i < commonLength; i++)
        {
            var cmp = Items[i].Id.CompareTo(other.Items[i].Id);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return Items.Length.CompareTo(other.Items.Length);
    }
}
