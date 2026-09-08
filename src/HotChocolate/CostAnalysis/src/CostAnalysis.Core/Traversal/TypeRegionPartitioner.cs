namespace HotChocolate.CostAnalysis;

/// <summary>
/// Partitions a boundary's scope into type regions: the coarsest partition
/// such that every type condition appearing in the boundary is constant
/// (fully inside or fully outside) within each region, so one representative
/// type per region decides which selections apply for every member.
/// </summary>
internal static class TypeRegionPartitioner
{
    /// <summary>
    /// Partitions <paramref name="scope"/> against <paramref name="conditions"/>.
    /// The result has at most <c>min(|scope|, 2^conditions.Count)</c> regions.
    /// </summary>
    /// <param name="snapshot">
    /// The schema snapshot the regions are built against.
    /// </param>
    /// <param name="scope">
    /// The boundary's possible types.
    /// </param>
    /// <param name="conditions">
    /// Every distinct type condition appearing in the boundary.
    /// </param>
    public static IReadOnlyList<PossibleTypeSet> Partition(
        CostSchemaSnapshot snapshot,
        PossibleTypeSet scope,
        IReadOnlyList<PossibleTypeSet> conditions)
    {
        var members = new int[scope.Count];
        var memberCount = 0;

        foreach (var index in scope)
        {
            members[memberCount++] = index;
        }

        var ranges = new List<(int Start, int Length)> { (0, memberCount) };

        foreach (var condition in conditions)
        {
            var refined = new List<(int Start, int Length)>(ranges.Count);

            foreach (var (start, length) in ranges)
            {
                var insideCount = PartitionInPlace(members, start, length, condition);

                if (insideCount > 0)
                {
                    refined.Add((start, insideCount));
                }

                if (insideCount < length)
                {
                    refined.Add((start + insideCount, length - insideCount));
                }
            }

            ranges = refined;
        }

        var regions = new PossibleTypeSet[ranges.Count];

        for (var i = 0; i < ranges.Count; i++)
        {
            var (start, length) = ranges[i];
            regions[i] = PossibleTypeSet.Create(snapshot.ObjectTypeCount, members.AsSpan(start, length));
        }

        return regions;
    }

    /// <summary>
    /// Partitions <c>members[start..start+length)</c> in place into a
    /// prefix whose types <paramref name="condition"/> contains and a
    /// suffix whose types it does not, reusing the input array so neither
    /// half allocates.
    /// </summary>
    private static int PartitionInPlace(int[] members, int start, int length, PossibleTypeSet condition)
    {
        var i = start;
        var j = start + length - 1;

        while (i <= j)
        {
            if (condition.Contains(members[i]))
            {
                i++;
            }
            else
            {
                (members[i], members[j]) = (members[j], members[i]);
                j--;
            }
        }

        return i - start;
    }
}
