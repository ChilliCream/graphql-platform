namespace HotChocolate.CostAnalysis;

/// <summary>
/// Groups possible object types that satisfy the same type conditions within a selection set.
/// </summary>
internal static class TypeRegionPartitioner
{
    /// <summary>
    /// Partitions <paramref name="scope"/> against <paramref name="conditions"/>.
    /// The result has at most <c>min(|scope|, 2^conditions.Count)</c> regions.
    /// </summary>
    /// <param name="schemaIndex">
    /// The schema index the regions are built against.
    /// </param>
    /// <param name="scope">
    /// The boundary's possible types.
    /// </param>
    /// <param name="conditions">
    /// Every distinct type condition appearing in the boundary.
    /// </param>
    public static IReadOnlyList<PossibleTypeSet> Partition(
        CostSchemaIndex schemaIndex,
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
            regions[i] = PossibleTypeSet.Create(schemaIndex.ObjectTypeCount, members.AsSpan(start, length));
        }

        return regions;
    }

    /// <summary>
    /// Partitions the specified range in place, placing types that match <paramref name="condition"/>
    /// before types that do not. Returns the number of matching types.
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
