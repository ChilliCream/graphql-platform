using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Resolves the effective <see cref="DeliveryGroupSetKey"/> for every leaf
/// field location collected by <see cref="DeferOccurrenceCollector"/>.
/// Fields whose set is empty are non-deferred; fields sharing a non-empty
/// set form a single subplan, matching the GraphQL incremental-delivery
/// spec rule that two sibling <c>... @defer</c> fragments which contribute
/// the same field collapse into one delivery group.
/// </summary>
internal static class DeferEffectiveSetResolver
{
    /// <summary>
    /// Groups occurrences by <see cref="FieldLocation"/> and computes the
    /// effective <see cref="DeliveryGroupSetKey"/> per location.
    /// </summary>
    public static Dictionary<FieldLocation, DeliveryGroupSetKey> Resolve(List<FieldOccurrence> occurrences)
    {
        var leavesByLocation = new Dictionary<FieldLocation, LeavesAccumulator>();

        foreach (var occurrence in occurrences)
        {
            var location = new FieldLocation(occurrence.ParentPath, occurrence.ResponseName);

            if (!leavesByLocation.TryGetValue(location, out var acc))
            {
                acc = new LeavesAccumulator();
                leavesByLocation[location] = acc;
            }

            acc.Add(occurrence.EnclosingSubPlan);
        }

        var result = new Dictionary<FieldLocation, DeliveryGroupSetKey>(leavesByLocation.Count);

        foreach (var (location, acc) in leavesByLocation)
        {
            result[location] = acc.ToEffectiveSet();
        }

        return result;
    }

    private sealed class LeavesAccumulator
    {
        private bool _hasImmediate;
        private readonly List<DeliveryGroup> _leaves = [];

        public void Add(DeliveryGroup? leaf)
        {
            if (leaf is null)
            {
                _hasImmediate = true;
                return;
            }

            foreach (var existing in _leaves)
            {
                if (ReferenceEquals(existing, leaf))
                {
                    return;
                }
            }

            _leaves.Add(leaf);
        }

        public DeliveryGroupSetKey ToEffectiveSet()
        {
            if (_hasImmediate || _leaves.Count == 0)
            {
                return DeliveryGroupSetKey.Empty;
            }

            // Parent-child pruning: drop any leaf whose ancestor is also
            // in the set.
            var pruned = new List<DeliveryGroup>(_leaves.Count);
            for (var i = 0; i < _leaves.Count; i++)
            {
                var ancestor = _leaves[i].Parent;
                var dropped = false;

                while (ancestor is not null)
                {
                    for (var j = 0; j < _leaves.Count; j++)
                    {
                        if (j != i && ReferenceEquals(_leaves[j], ancestor))
                        {
                            dropped = true;
                            break;
                        }
                    }

                    if (dropped)
                    {
                        break;
                    }

                    ancestor = ancestor.Parent;
                }

                if (!dropped)
                {
                    pruned.Add(_leaves[i]);
                }
            }

            if (pruned.Count == 0)
            {
                return DeliveryGroupSetKey.Empty;
            }

            pruned.Sort(static (a, b) => a.Id.CompareTo(b.Id));
            return new DeliveryGroupSetKey(pruned.ToImmutableArray());
        }
    }
}
