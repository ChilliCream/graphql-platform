using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Resolves the effective <see cref="DeferUsageSetKey"/> for every leaf
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
    /// effective <see cref="DeferUsageSetKey"/> per location.
    /// </summary>
    public static Dictionary<FieldLocation, DeferUsageSetKey> Resolve(List<FieldOccurrence> occurrences)
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

            acc.Add(occurrence.EnclosingDefer);
        }

        var result = new Dictionary<FieldLocation, DeferUsageSetKey>(leavesByLocation.Count);

        foreach (var (location, acc) in leavesByLocation)
        {
            result[location] = acc.ToEffectiveSet();
        }

        return result;
    }

    private sealed class LeavesAccumulator
    {
        private bool _hasNonDeferred;
        private readonly List<DeferUsage> _leaves = [];

        public void Add(DeferUsage? leaf)
        {
            if (leaf is null)
            {
                _hasNonDeferred = true;
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

        public DeferUsageSetKey ToEffectiveSet()
        {
            if (_hasNonDeferred || _leaves.Count == 0)
            {
                return DeferUsageSetKey.Empty;
            }

            // Parent-child pruning: drop any leaf whose ancestor is also
            // in the set.
            var pruned = new List<DeferUsage>(_leaves.Count);
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
                return DeferUsageSetKey.Empty;
            }

            pruned.Sort(static (a, b) => a.Id.CompareTo(b.Id));
            return new DeferUsageSetKey(pruned.ToImmutableArray());
        }
    }
}
